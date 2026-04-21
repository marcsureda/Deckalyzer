using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public class DeckAnalysisService : IDeckAnalysisService
{
    private readonly IScryfallService _scryfall;
    private readonly IDeckParser _parser;
    private readonly IPowerLevelAnalyzer _analyzer;
    private readonly ILogger<DeckAnalysisService> _logger;

    public DeckAnalysisService(
        IScryfallService scryfall,
        IDeckParser parser,
        IPowerLevelAnalyzer analyzer,
        ILogger<DeckAnalysisService> logger)
    {
        _scryfall = scryfall;
        _parser = parser;
        _analyzer = analyzer;
        _logger = logger;
    }

    public async Task<DeckAnalysisResult> AnalyzeDeck(string deckList, CancellationToken cancellationToken = default)
    {
        // Parse the decklist
        var (commanders, mainCards, sideboardCards, parseWarnings) = _parser.ParseDeckList(deckList);

        if (mainCards.Count == 0)
        {
            throw new ArgumentException("No cards found in the deck list. Please check the format.");
        }

        var warnings = new List<string>(parseWarnings);

        // Warn about sideboard cards being skipped
        if (sideboardCards.Count > 0)
        {
            warnings.Add($"Sideboard ({sideboardCards.Count} cards) was skipped — only the main deck is analyzed.");
        }

        // Card count validation (Commander decks should be exactly 100)
        int totalQuantity = mainCards.Sum(c => c.quantity);
        if (totalQuantity != 100)
        {
            warnings.Add($"Deck has {totalQuantity} cards — Commander decks should have exactly 100 cards (including the commander).");
        }

        _logger.LogInformation("Parsed {Count} main cards, {SBCount} sideboard cards, commanders: {Commanders}",
            mainCards.Count, sideboardCards.Count, string.Join(" + ", commanders));

        // Collect unique card names for Scryfall lookup
        var uniqueNames = mainCards.Select(c => c.name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Fetch game changer names and card data in parallel
        var gameChangerTask = _scryfall.GetGameChangerNamesAsync();
        var scryfallTask = _scryfall.GetCardsByNames(uniqueNames);

        await Task.WhenAll(gameChangerTask, scryfallTask);

        var gameChangerNames = gameChangerTask.Result;
        var scryfallCards = scryfallTask.Result;

        if (gameChangerNames.Count > 0)
        {
            _logger.LogInformation("Loaded {Count} game changer names from Scryfall", gameChangerNames.Count);
            _analyzer.SetDynamicGameChangers(gameChangerNames);
        }
        else
        {
            _logger.LogWarning("Could not fetch game changers from Scryfall, using fallback list");
        }

        _logger.LogInformation("Fetched {Count} cards from Scryfall", scryfallCards.Count);

        // Build a lookup by name
        var scryfallLookup = new Dictionary<string, ScryfallCard>(StringComparer.OrdinalIgnoreCase);
        foreach (var sc in scryfallCards)
        {
            scryfallLookup.TryAdd(sc.Name, sc);

            // Also add by first face name for DFCs
            if (sc.CardFaces?.Count > 0)
            {
                scryfallLookup.TryAdd(sc.CardFaces[0].Name, sc);
            }
        }

        // Retry not-found cards via fuzzy search (resolves Universes Beyond alternate names
        // like "Cecil Harvey" → "Tymna the Weaver", reprints with different names, etc.)
        var notFoundNames = uniqueNames
            .Where(name => !scryfallLookup.ContainsKey(name) && !scryfallLookup.ContainsKey(name.Split(" // ")[0].Trim()))
            .ToList();

        foreach (var missingName in notFoundNames)
        {
            var fuzzyResult = await _scryfall.GetCardByName(missingName);
            if (fuzzyResult != null)
            {
                scryfallLookup.TryAdd(missingName, fuzzyResult);
                scryfallLookup.TryAdd(fuzzyResult.Name, fuzzyResult);
                if (fuzzyResult.CardFaces?.Count > 0)
                {
                    scryfallLookup.TryAdd(fuzzyResult.CardFaces[0].Name, fuzzyResult);
                }
                _logger.LogInformation("Fuzzy resolved: \"{Input}\" → \"{Resolved}\"", missingName, fuzzyResult.Name);
            }
        }

        // Analyze each card
        var analyzedCards = new List<CardInfo>();
        foreach (var (quantity, name) in mainCards)
        {
            // Try exact match, then try first part of double-faced card name
            if (!scryfallLookup.TryGetValue(name, out var scryfallCard))
            {
                var baseName = name.Split(" // ")[0].Trim();
                scryfallLookup.TryGetValue(baseName, out scryfallCard);
            }

            if (scryfallCard != null)
            {
                var isCommander = commanders.Any(cmd =>
                    string.Equals(name, cmd, StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(cmd, StringComparison.OrdinalIgnoreCase));

                var cardInfo = _analyzer.AnalyzeCard(scryfallCard, isCommander);
                cardInfo.Quantity = quantity;
                analyzedCards.Add(cardInfo);
            }
            else
            {
                _logger.LogWarning("Could not find card data for: {CardName}", name);
                warnings.Add($"Card not found: \"{name}\" — check spelling or set code.");

                // Add a placeholder for unfound cards
                analyzedCards.Add(new CardInfo
                {
                    Name = name,
                    Quantity = quantity,
                    Playability = 30,
                    Impact = 2,
                    PowerScore = 2,
                });
            }
        }

        // Run full deck analysis
        var result = _analyzer.AnalyzeDeck(analyzedCards);
        result.Warnings = warnings;

        // Fetch cheapest English print prices from Cardmarket (via Scryfall)
        var cardNamesForPricing = analyzedCards
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var cheapestPrices = await _scryfall.GetCheapestEurPrices(cardNamesForPricing);

        // Update each card's EUR price, Cardmarket link, and image from the cheapest print
        foreach (var card in result.Cards)
        {
            CheapestPrintInfo? info = null;
            if (cheapestPrices.TryGetValue(card.Name, out var cpInfo))
                info = cpInfo;
            else
            {
                var frontFace = card.Name.Split(" // ")[0].Trim();
                cheapestPrices.TryGetValue(frontFace, out info);
            }

            if (info != null)
            {
                card.PriceEur = info.PriceEur;
                if (!string.IsNullOrEmpty(info.CardmarketUri))
                    card.CardmarketUri = info.CardmarketUri;
                if (!string.IsNullOrEmpty(info.ImageUri))
                    card.ImageUriCheapest = info.ImageUri;
            }
        }

        // Calculate deck total value
        result.DeckTotalValueEur = result.Cards.Sum(c => c.PriceEur * c.Quantity);

        // Try to fetch images for add recommendations
        await EnrichRecommendationsWithImages(result.AddRecommendations);

        // Enrich tokens with images from Scryfall's all_parts
        await EnrichTokensWithImages(result.Tokens, scryfallLookup);

        return result;
    }

    private async Task EnrichRecommendationsWithImages(List<CardRecommendation> recommendations)
    {
        var cardNames = recommendations
            .Where(r => string.IsNullOrEmpty(r.ImageUri))
            .Select(r => r.CardName)
            .ToList();

        if (cardNames.Count == 0) return;

        var cards = await _scryfall.GetCardsByNames(cardNames);
        var lookup = cards.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        foreach (var rec in recommendations)
        {
            if (lookup.TryGetValue(rec.CardName, out var card))
            {
                rec.ImageUri = card.ImageUris?.Normal
                    ?? card.CardFaces?.FirstOrDefault()?.ImageUris?.Normal
                    ?? "";
            }
        }
    }

    /// <summary>
    /// Fetches token images by looking up all_parts from the producing cards in Scryfall data.
    /// </summary>
    private async Task EnrichTokensWithImages(List<TokenInfo> tokens, Dictionary<string, ScryfallCard> scryfallLookup)
    {
        if (tokens.Count == 0) return;

        // Build a map of token description keywords to Scryfall all_parts token URIs
        var tokenImageCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            if (!string.IsNullOrEmpty(token.ImageUri)) continue;

            // Try to find a token image from one of the producing cards' all_parts
            foreach (var producerName in token.ProducedBy)
            {
                if (tokenImageCache.TryGetValue(token.Description, out var cachedImage))
                {
                    token.ImageUri = cachedImage;
                    break;
                }

                if (!scryfallLookup.TryGetValue(producerName, out var producerCard))
                    continue;

                if (producerCard.AllParts == null || producerCard.AllParts.Count == 0)
                    continue;

                // Find the token part that best matches this token description
                var tokenPart = producerCard.AllParts
                    .Where(p => p.Component.Equals("token", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(p => TokenDescriptionMatches(token.Description, p.Name, p.TypeLine));

                // If no specific match, just take the first token part
                tokenPart ??= producerCard.AllParts
                    .FirstOrDefault(p => p.Component.Equals("token", StringComparison.OrdinalIgnoreCase));

                if (tokenPart != null && !string.IsNullOrEmpty(tokenPart.Uri))
                {
                    try
                    {
                        var tokenCard = await _scryfall.GetCardByUri(tokenPart.Uri);
                        if (tokenCard != null)
                        {
                            var imageUri = tokenCard.ImageUris?.Normal
                                ?? tokenCard.CardFaces?.FirstOrDefault()?.ImageUris?.Normal
                                ?? "";

                            if (!string.IsNullOrEmpty(imageUri))
                            {
                                token.ImageUri = imageUri;
                                tokenImageCache[token.Description] = imageUri;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch token image for {Token}", token.Description);
                    }
                }
            }
        }
    }

    private static bool TokenDescriptionMatches(string tokenDescription, string partName, string partTypeLine)
    {
        var descLower = tokenDescription.ToLowerInvariant();
        var nameLower = partName.ToLowerInvariant();
        var typeLower = partTypeLine.ToLowerInvariant();

        // Check if the token name appears in the description
        if (descLower.Contains(nameLower)) return true;

        // Check for creature type matches (e.g., "Vampire" in "1/1 Black Vampire")
        var typeWords = typeLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var tw in typeWords)
        {
            if (tw.Length > 3 && descLower.Contains(tw) && tw != "token" && tw != "creature")
                return true;
        }

        return false;
    }
}

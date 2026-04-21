using System.Text.RegularExpressions;
using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public partial class PowerLevelAnalyzer : IPowerLevelAnalyzer
{
    // Volatile reference allows atomic replacement without a lock.
    // IsGameChanger() reads the reference once — safe because we never mutate the HashSet itself,
    // only swap the entire reference via SetDynamicGameChangers().
    private volatile HashSet<string> _dynamicGameChangers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Atomically replaces the dynamic game changer list fetched from Scryfall's is:gamechanger search.
    /// Thread-safe: the volatile reference ensures all threads immediately see the new set.
    /// </summary>
    public void SetDynamicGameChangers(HashSet<string> names)
    {
        // Assign to a local immutable snapshot; volatile write ensures visibility across threads.
        _dynamicGameChangers = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    public CardInfo AnalyzeCard(ScryfallCard scryfall, bool isCommander)
    {
        var card = new CardInfo
        {
            Name = scryfall.Name,
            Cmc = scryfall.Cmc,
            ManaCost = scryfall.ManaCost ?? scryfall.CardFaces?.FirstOrDefault()?.ManaCost ?? "",
            TypeLine = scryfall.TypeLine,
            OracleText = scryfall.OracleText ?? scryfall.CardFaces?.FirstOrDefault()?.OracleText ?? "",
            ColorIdentity = scryfall.ColorIdentity,
            Colors = scryfall.Colors ?? [],
            Keywords = scryfall.Keywords,
            Rarity = scryfall.Rarity,
            EdhrecRank = scryfall.EdhrecRank ?? 20000,
            IsCommander = isCommander,
            ScryfallUri = scryfall.ScryfallUri,
        };

        // Price
        if (scryfall.Prices?.Usd != null && double.TryParse(scryfall.Prices.Usd, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
            card.Price = price;
        if (scryfall.Prices?.Eur != null && double.TryParse(scryfall.Prices.Eur, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var priceEur))
            card.PriceEur = priceEur;

        // Image
        card.ImageUri = scryfall.ImageUris?.Normal
            ?? scryfall.CardFaces?.FirstOrDefault()?.ImageUris?.Normal
            ?? "";

        // Type classification
        var typeLine = card.TypeLine.ToLowerInvariant();
        card.IsLand = typeLine.Contains("land");
        card.IsCreature = typeLine.Contains("creature");
        card.IsArtifact = typeLine.Contains("artifact");
        card.IsEnchantment = typeLine.Contains("enchantment");
        card.IsInstant = typeLine.Contains("instant");
        card.IsSorcery = typeLine.Contains("sorcery");
        card.IsPlaneswalker = typeLine.Contains("planeswalker");

        var oracleText = card.OracleText.ToLowerInvariant();

        // Functional classification
        card.IsTutor = TutorCards.Contains(card.Name) || ClassifyAsTutor(oracleText);
        card.IsExtraTurn = ExtraTurnCards.Contains(card.Name) || oracleText.Contains("extra turn");
        card.IsMassLandDenial = MassLandDenialCards.Contains(card.Name) || ClassifyAsMLD(oracleText);
        // Use Scryfall's official game_changer field, then dynamic list, then fallback
        card.IsGameChanger = scryfall.GameChanger || IsGameChanger(card.Name);
        card.IsFastMana = FastManaCards.Contains(card.Name);
        card.IsInfiniteComboPiece = AllComboCardNames.Contains(card.Name);
        card.IsCounterspell = ClassifyAsCounterspell(oracleText, typeLine);
        card.IsBoardWipe = ClassifyAsBoardWipe(oracleText);
        card.IsRemoval = ClassifyAsRemoval(oracleText, typeLine);
        card.IsCardDraw = ClassifyAsCardDraw(oracleText);
        card.IsRamp = ClassifyAsRamp(oracleText, typeLine, card);

        // Calculate scores
        card.Playability = CalculatePlayability(card);
        card.Impact = CalculateImpact(card, scryfall.Reserved);
        card.PowerScore = CalculatePowerScore(card);

        return card;
    }

    public DeckAnalysisResult AnalyzeDeck(List<CardInfo> cards)
    {
        var result = new DeckAnalysisResult
        {
            TotalCards = cards.Count,
            Cards = cards.OrderByDescending(c => c.Impact).ToList()
        };

        // Commander info (supports partner/companion — multiple commanders)
        var commanderCards = cards.Where(c => c.IsCommander).ToList();
        if (commanderCards.Count > 0)
        {
            // Primary commander name + image for backwards compat
            result.CommanderName = string.Join(" + ", commanderCards.Select(c => c.Name));
            result.CommanderImageUri = commanderCards[0].ImageUri;

            // Full lists for partner display
            result.CommanderNames = commanderCards.Select(c => c.Name).ToList();
            result.CommanderImageUris = commanderCards.Select(c => c.ImageUri).ToList();

            // Merge color identities from all commanders
            result.ColorIdentity = commanderCards
                .SelectMany(c => c.ColorIdentity)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Composition
        result.Composition = new DeckComposition
        {
            Creatures = cards.Count(c => c.IsCreature && !c.IsLand),
            Instants = cards.Count(c => c.IsInstant),
            Sorceries = cards.Count(c => c.IsSorcery),
            Artifacts = cards.Count(c => c.IsArtifact && !c.IsCreature),
            Enchantments = cards.Count(c => c.IsEnchantment && !c.IsCreature),
            Planeswalkers = cards.Count(c => c.IsPlaneswalker),
            Lands = cards.Count(c => c.IsLand),
            Ramp = cards.Count(c => c.IsRamp),
            CardDraw = cards.Count(c => c.IsCardDraw),
            Removal = cards.Count(c => c.IsRemoval),
            BoardWipes = cards.Count(c => c.IsBoardWipe),
            Counterspells = cards.Count(c => c.IsCounterspell),
            Tutors = cards.Count(c => c.IsTutor),
        };
        result.Composition.Other = result.TotalCards -
            result.Composition.Creatures - result.Composition.Instants -
            result.Composition.Sorceries - result.Composition.Artifacts -
            result.Composition.Enchantments - result.Composition.Planeswalkers -
            result.Composition.Lands;
        if (result.Composition.Other < 0) result.Composition.Other = 0;

        // Mana Analysis
        AnalyzeMana(cards, result);

        // Bracket Analysis (initial — will be adjusted after power calculation)
        AnalyzeBracket(cards, result);

        // Power Metrics
        CalculatePowerMetrics(cards, result);

        // Adjust bracket based on power level (bracket 1 can't have power 7)
        AdjustBracketForPower(result);

        // Synergy with commander
        CalculateSynergies(cards);

        // Strategy / archetype detection
        AnalyzeStrategy(cards, result);

        // Token analysis
        AnalyzeTokens(cards, result);

        // Recommendations
        GenerateRecommendations(cards, result);

        // Strengths and Weaknesses
        AnalyzeStrengthsWeaknesses(result);

        return result;
    }
}

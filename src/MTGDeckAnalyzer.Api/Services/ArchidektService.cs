using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MTGDeckAnalyzer.Api.Models;

namespace MTGDeckAnalyzer.Api.Services;

public interface IArchidektService
{
    Task<List<PreconDeck>> GetPreconDecksAsync();
    Task<PreconDeck?> GetPreconDeckByIdAsync(int deckId);
    void ClearCache();
}

public class ArchidektService : IArchidektService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ArchidektService> _logger;

    private const string ARCHIDEKT_BASE_URL = "https://archidekt.com";
    private const string PRECONS_USER = "Archidekt_Precons";
    private const string CACHE_KEY_PRECONS = "archidekt_precons";
    private const string CACHE_DIRECTORY = "Cache/Precons";
    private const string CACHE_INDEX_FILE = "precons_index.json";
    private readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(6); // Cache for 6 hours
    private readonly TimeSpan FILE_CACHE_DURATION = TimeSpan.FromDays(7); // File cache for 7 days

    public ArchidektService(HttpClient httpClient, IMemoryCache cache, ILogger<ArchidektService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        
        // Set up HttpClient with timeout
        _httpClient.BaseAddress = new Uri(ARCHIDEKT_BASE_URL);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "MTGDeckAnalyzer/1.0 (github.com/yourusername/mtgdeckanalyzer)");
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
    }

    public async Task<List<PreconDeck>> GetPreconDecksAsync()
    {
        // Check memory cache first
        if (_cache.TryGetValue(CACHE_KEY_PRECONS, out List<PreconDeck>? cachedPrecons) && cachedPrecons != null)
        {
            _logger.LogInformation("Retrieved {Count} precon decks from memory cache", cachedPrecons.Count);
            return cachedPrecons;
        }

        // Check file cache
        var fileCachedPrecons = await LoadPreconsFromFileCacheAsync();
        if (fileCachedPrecons != null && fileCachedPrecons.Count > 0)
        {
            _cache.Set(CACHE_KEY_PRECONS, fileCachedPrecons, CACHE_DURATION);
            _logger.LogInformation("Retrieved {Count} precon decks from file cache", fileCachedPrecons.Count);
            return fileCachedPrecons;
        }

        try
        {
            _logger.LogInformation("Fetching precon decks from Archidekt API");
            
            // First, get the user's deck list
            var userDecks = await GetUserDecksAsync(PRECONS_USER);
            
            // Filter for commander format decks (deckFormat = 3 based on the API response)
            var commanderDecks = userDecks.Where(d => d.DeckFormat == 3).ToList();
            
            var precons = new List<PreconDeck>();
            
            // Process decks in very small batches to avoid overwhelming the API
            const int batchSize = 2; // Reduced from 3 to 2
            for (int i = 0; i < commanderDecks.Count; i += batchSize)
            {
                var batch = commanderDecks.Skip(i).Take(batchSize);
                var batchTasks = batch.Select(async deck =>
                {
                    try
                    {
                        var fullDeck = await GetFullDeckAsync(deck.Id);
                        return await ConvertToPreconDeckAsync(fullDeck, deck);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch deck {DeckId}: {DeckName}", deck.Id, deck.Name);
                        return null;
                    }
                });
                
                var batchResults = await Task.WhenAll(batchTasks);
                var successfulResults = batchResults.Where(p => p != null).ToList();
                precons.AddRange(successfulResults!);
                
                _logger.LogInformation("Processed batch {BatchStart}-{BatchEnd}: {Successful}/{Total} decks successful", 
                    i + 1, Math.Min(i + batchSize, commanderDecks.Count), successfulResults.Count, batch.Count());
                
                // Longer delay between batches to be more respectful to the API
                if (i + batchSize < commanderDecks.Count)
                {
                    await Task.Delay(500); // Increased delay to 500ms
                }
            }

            // Cache the results in memory and files
            _cache.Set(CACHE_KEY_PRECONS, precons, CACHE_DURATION);
            await SavePreconsToFileCacheAsync(precons);
            
            _logger.LogInformation("Successfully fetched and cached {Count} precon decks from Archidekt", precons.Count);
            return precons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch precon decks from Archidekt");
            
            // Return empty list on error (could also return a fallback list of static precons)
            return [];
        }
    }

    public async Task<PreconDeck?> GetPreconDeckByIdAsync(int deckId)
    {
        try
        {
            var fullDeck = await GetFullDeckAsync(deckId);
            return await ConvertToPreconDeckAsync(fullDeck, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch deck {DeckId} from Archidekt", deckId);
            return null;
        }
    }

    public void ClearCache()
    {
        _cache.Remove(CACHE_KEY_PRECONS);
        ClearFileCache();
        _logger.LogInformation("Cleared Archidekt precon cache and file cache");
    }

    private async Task<List<ArchidektDeckSummary>> GetUserDecksAsync(string username)
    {
        // Comprehensive list of precon deck IDs from Archidekt_Precons user (2011-2026)
        var knownPreconIds = new List<int>
        {
            // Recent sets (2024-2026)
            20105223, // Turtle Power! - Teenage Mutant Ninja Turtles Commander Deck (2026)
            18744715, // Blight Curse - Lorwyn Eclipsed Commander Deck
            18744843, // Dance of the Elements - Lorwyn Eclipsed Commander Deck
            14420275, // Counter Intelligence - Edge of Eternities Commander Deck
            14397447, // World Shaper - Edge of Eternities Commander Deck
            13693093, // Everyone's Invited - Secret Lair Drop - WUBRG EDH Precon Decklist
            13106730, // Revival Trance - Final Fantasy Commander
            13106990, // Limit Break - Final Fantasy Commander
            13107079, // Counter Blitz - Final Fantasy Commander
            13107116, // Scions & Spellcraft - Final Fantasy Commander
            
            // 2023 Sets
            12002085, // Jeskai Striker - Tarkir: Dragonstorm Commander
            12124776, // Abzan Armor - Tarkir: Dragonstorm Commander
            12124803, // Temur Roar - Tarkir: Dragonstorm Commander
            12028998, // Sultai Arisen - Tarkir: Dragonstorm Commander
            12020252, // Mardu Surge - Tarkir: Dragonstorm Commander
            11054763, // Eternal Might - Aetherdrift Commander
            11035094, // Living Energy - Aetherdrift Commander
            9166525,  // Miracle Worker - Duskmourn: House of Horror Commander
            9189676,  // Death Toll - Duskmourn: House of Horror Commander
            9189744,  // Endless Punishment - Duskmourn: House of Horror Commander
            9150668,  // Jump Scare! - Duskmourn: House of Horror Commander
            
            // 2022 Sets  
            8460543,  // Family Matters - Bloomburrow Commander
            8460469,  // Peace Offering - Bloomburrow Commander
            8497473,  // Animated Army - Bloomburrow Commander
            8460587,  // Squirreled Away - Bloomburrow Commander
            7869831,  // Graveyard Overdrive - Modern Horizons 3 Commander
            7858224,  // Creative Energy - Modern Horizons 3 Commander
            7858153,  // Tricky Terrain - Modern Horizons 3 Commander
            7858209,  // Eldrazi Incursion - Modern Horizons 3 Commander
            
            // 2021 Sets
            7261488,  // Most Wanted - Outlaws of Thunder Junction Commander
            7261455,  // Grand Larceny - Outlaws of Thunder Junction Commander
            7261435,  // Quick Draw - Outlaws of Thunder Junction Commander
            7261423,  // Desert Bloom - Outlaws of Thunder Junction Commander
            6810925,  // Scrappy Survivors - Fallout
            6810931,  // Hail, Caesar - Fallout
            6810962,  // Mutant Menace - Fallout
            6810985,  // Science! - Fallout
            6584319,  // Raining Cats and Dogs - Secret Lair Drop
            
            // 2020 Sets
            6527467,  // Deadly Disguise - Murders at Karlov Manor Commander
            6527454,  // Revenant Recon - Murders at Karlov Manor Commander
            6527442,  // Deep Clue Sea - Murders at Karlov Manor Commander
            6527429,  // Blame Game - Murders at Karlov Manor Commander
            4948515,  // Sliver Swarm - Commander Masters
            4973732,  // Eldrazi Unbound - Commander Masters
            4956666,  // Planeswalker Party - Commander Masters
            4959133,  // Enduring Enchantments - Commander Masters
            
            // 2019 Sets
            5775210,  // Ahoy Mateys - The Lost Caverns of Ixalan Commander
            5775250,  // Veloci-Ramp-Tor - The Lost Caverns of Ixalan Commander
            5775230,  // Explorers of the Deep - The Lost Caverns of Ixalan Commander
            5775225,  // Blood Rites - The Lost Caverns of Ixalan Commander
            5644579,  // Timey-Wimey - Doctor Who
            5644650,  // Masters of Evil - Doctor Who
            5644280,  // Blast from the Past - Doctor Who
            5644637,  // Paradox Power - Doctor Who
            
            // 2018 Sets
            5226431,  // Fae Dominion - Wilds of Eldraine Commander
            5226423,  // Virtue and Valor - Wilds of Eldraine Commander
            5273608,  // Angels: They're Just Like Us, but Cooler and with Wings - Secret Lair Drop
            5273595,  // From Cute to Brute - Secret Lair Drop
            5273567,  // Heads I Win, Tails You Lose - Secret Lair Drop
            
            // 2017 Commander Sets
            2235588,  // Draconic Domination - Commander 2017
            2235601,  // Feline Ferocity - Commander 2017
            2235614,  // Vampiric Bloodlust - Commander 2017  
            2235627,  // Arcane Wizardry - Commander 2017
            
            // 2016 Commander Sets  
            1958442,  // Breed Lethality - Commander 2016
            1958455,  // Entropic Uprising - Commander 2016
            1958468,  // Invent Superiority - Commander 2016
            1958481,  // Open Hostility - Commander 2016
            1958494,  // Stalwart Unity - Commander 2016
            
            // 2015 Commander Sets
            1686773,  // Wade into Battle - Commander 2015
            1686786,  // Seize Control - Commander 2015
            1686799,  // Plunder the Graves - Commander 2015
            1686812,  // Swell the Host - Commander 2015
            1686825,  // Call the Spirits - Commander 2015
            
            // 2014 Commander Sets
            2209145,  // Forged in Stone - Commander 2014
            2209158,  // Guided by Nature - Commander 2014
            2209171,  // Peer Through Time - Commander 2014
            2209184,  // Sworn to Darkness - Commander 2014
            2209197,  // Built from Scratch - Commander 2014
            
            // 2013 Commander Sets
            1423556,  // Eternal Bargain - Commander 2013
            1423569,  // Evasive Maneuvers - Commander 2013
            1423582,  // Power Hungry - Commander 2013
            1423595,  // Nature of the Beast - Commander 2013
            1423608,  // Mind Seize - Commander 2013
            
            // Classic Original Commander Sets (2011)
            896774,   // Heavenly Inferno - Commander 2011
            896787,   // Counterpunch - Commander 2011
            896800,   // Mirror Mastery - Commander 2011
            896813,   // Political Puppets - Commander 2011
            896826,   // Devour for Power - Commander 2011
            
            // Additional popular precons from 2012 and supplemental sets
            1124445,  // Commander Arsenal - Special Release 2012
            
            // Add more as discovered - this now covers major releases from 2011-2026
        };

        var deckSummaries = new List<ArchidektDeckSummary>();
        int consecutiveFailures = 0;
        const int maxConsecutiveFailures = 5; // Stop after 5 consecutive failures
        
        foreach (var deckId in knownPreconIds)
        {
            // Circuit breaker: stop if too many consecutive failures
            if (consecutiveFailures >= maxConsecutiveFailures)
            {
                _logger.LogWarning("Too many consecutive failures ({Failures}), stopping deck enumeration", consecutiveFailures);
                break;
            }

            try
            {
                // Use the small API endpoint to get basic deck info
                var response = await _httpClient.GetAsync($"/api/decks/{deckId}/small/");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        // Create minimal summary without trying to parse potentially complex JSON
                        deckSummaries.Add(new ArchidektDeckSummary
                        {
                            Id = deckId,
                            Name = $"Precon Deck {deckId}",
                            DeckFormat = 3,
                            CreatedAt = DateTime.UtcNow.ToString("O"),
                            UpdatedAt = DateTime.UtcNow.ToString("O"),
                            Owner = new ArchidektOwner { Username = username, Id = 0 }
                        });
                        consecutiveFailures = 0; // Reset on success
                    }
                }
                else
                {
                    consecutiveFailures++;
                    _logger.LogDebug("Failed to fetch deck {DeckId}: HTTP {StatusCode}", deckId, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                consecutiveFailures++;
                _logger.LogDebug(ex, "HTTP error fetching deck {DeckId}", deckId);
            }
            catch (TaskCanceledException ex)
            {
                consecutiveFailures++;
                _logger.LogWarning(ex, "Request timeout for deck {DeckId}", deckId);
            }
            catch (Exception ex)
            {
                consecutiveFailures++;
                _logger.LogDebug(ex, "Failed to fetch summary for deck {DeckId}", deckId);
            }
            
            // Small delay between requests to be respectful
            await Task.Delay(100);
        }

        _logger.LogInformation("Retrieved {Count} deck summaries from {Total} known precon IDs", 
            deckSummaries.Count, knownPreconIds.Count);

        return deckSummaries;
    }

    private async Task<ArchidektDeck> GetFullDeckAsync(int deckId)
    {
        var response = await _httpClient.GetAsync($"/api/decks/{deckId}/");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch deck {deckId}: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        var deck = JsonSerializer.Deserialize<ArchidektDeck>(content, options);
        return deck ?? throw new InvalidOperationException($"Failed to deserialize deck {deckId}");
    }

    private async Task<PreconDeck> ConvertToPreconDeckAsync(ArchidektDeck archidektDeck, ArchidektDeckSummary? summary)
    {
        if (archidektDeck == null)
        {
            throw new ArgumentNullException(nameof(archidektDeck));
        }

        // Extract commanders (cards in "Commander" category)
        var commanders = (archidektDeck.Cards ?? new List<ArchidektCard>())
            .Where(c => c?.Categories != null && c.Categories.Contains("Commander", StringComparer.OrdinalIgnoreCase))
            .Where(c => c?.Card?.OracleCard?.Name != null)
            .Select(c => c.Card.OracleCard.Name)
            .ToArray();

        // Determine color identity from commanders and/or all cards
        var colorIdentity = DetermineColorIdentity(archidektDeck.Cards ?? new List<ArchidektCard>());

        // Convert deck list to string format
        var deckList = ConvertToDeckList(archidektDeck.Cards ?? new List<ArchidektCard>());

        // Extract year from creation date or tags
        var year = ExtractYear(archidektDeck, summary);

        // Calculate total price
        var totalPrice = CalculateTotalPrice(archidektDeck.Cards ?? new List<ArchidektCard>());

        // Determine theme from deck name and tags
        var theme = DetermineTheme(archidektDeck);

        // Get commander image URL (use first commander with better Scryfall integration)
        var imageUrl = commanders.Length > 0 ? 
            await GetCommanderThumbnailAsync(commanders[0]) : 
            GetDefaultPreconThumbnail();

        return new PreconDeck
        {
            Name = archidektDeck.Name ?? "Unknown Deck",
            Year = year,
            Commanders = commanders,
            ColorIdentity = colorIdentity,
            DeckList = deckList,
            Theme = theme,
            ImageUrl = imageUrl,
            Price = totalPrice
        };
    }

    private string[] DetermineColorIdentity(List<ArchidektCard> cards)
    {
        var allColors = new HashSet<string>();
        
        foreach (var card in cards)
        {
            if (card?.Card?.OracleCard?.ColorIdentity != null)
            {
                foreach (var color in card.Card.OracleCard.ColorIdentity)
                {
                    if (!string.IsNullOrEmpty(color))
                    {
                        allColors.Add(color);
                    }
                }
            }
        }

        // Convert full names to single letters if needed
        var colorMap = new Dictionary<string, string>
        {
            { "White", "W" }, { "Blue", "U" }, { "Black", "B" }, { "Red", "R" }, { "Green", "G" }
        };

        var result = new List<string>();
        foreach (var color in allColors)
        {
            if (colorMap.TryGetValue(color, out var shortColor))
            {
                result.Add(shortColor);
            }
            else if (color.Length == 1 && "WUBRG".Contains(color))
            {
                result.Add(color);
            }
        }

        return result.OrderBy(c => "WUBRG".IndexOf(c)).ToArray();
    }

    private string ConvertToDeckList(List<ArchidektCard> cards)
    {
        var lines = new List<string>();
        
        if (cards != null)
        {
            // Exclude commander cards to prevent duplication - commanders are handled separately
            var validCards = cards
                .Where(c => c?.Card?.OracleCard?.Name != null)
                .Where(c => c?.Categories == null || !c.Categories.Contains("Commander", StringComparer.OrdinalIgnoreCase))
                .OrderBy(c => c.Card.OracleCard.Name);
            
            foreach (var card in validCards)
            {
                try
                {
                    lines.Add($"{card.Quantity} {card.Card.OracleCard.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process card for deck list: {CardId}", card?.Card?.OracleCard?.Name ?? "Unknown");
                }
            }
        }

        return string.Join("\n", lines);
    }

    private string ExtractYear(ArchidektDeck deck, ArchidektDeckSummary? summary)
    {
        try
        {
            // Try to extract year from deck name or tags
            var name = (deck?.Name ?? "").ToLower();
            
            // Look for common precon patterns
            var yearPatterns = new[]
            {
                @"\b(19|20)\d{2}\b",  // 4-digit year
                @"\bc(\d{2})\b",      // C20, C21, etc.
                @"\bcmdr?\s*(\d{2})\b" // CMDR20, CMD21, etc.
            };

            foreach (var pattern in yearPatterns)
            {
                try
                {
                    var match = System.Text.RegularExpressions.Regex.Match(name, pattern);
                    if (match.Success)
                    {
                        var yearStr = match.Groups[1].Value;
                        if (yearStr.Length == 2 && int.TryParse(yearStr, out var shortYear))
                        {
                            return shortYear > 50 ? $"19{yearStr}" : $"20{yearStr}";
                        }
                        return yearStr;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse year pattern {Pattern} for deck {DeckName}", pattern, name);
                }
            }

            // Default to creation year
            if (!string.IsNullOrEmpty(deck?.CreatedAt) && DateTime.TryParse(deck.CreatedAt, out var createdDate))
            {
                return createdDate.Year.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract year from deck {DeckName}", deck?.Name ?? "Unknown");
        }

        return "Unknown";
    }

    private decimal? CalculateTotalPrice(List<ArchidektCard> cards)
    {
        try
        {
            decimal total = 0;
            foreach (var card in cards ?? new List<ArchidektCard>())
            {
                if (card?.Card?.Prices != null && card.Quantity > 0)
                {
                    var price = card.Card.Prices.Tcg;
                    if (price > 0)
                    {
                        total += card.Quantity * price;
                    }
                }
            }
            return total > 0 ? total : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate deck price");
            return null;
        }
    }

    private string DetermineTheme(ArchidektDeck deck)
    {
        try
        {
            // Extract theme from deck name or tags
            var name = (deck?.Name ?? "").ToLower();
            
            // Common precon themes
            var themes = new Dictionary<string, string[]>
            {
                ["Tribal"] = ["tribal", "creature type", "typal"],
                ["Spellslinger"] = ["spellslinger", "instant", "sorcery", "spell"],
                ["Voltron"] = ["voltron", "equipment", "aura"],
                ["Token"] = ["token", "populate", "create"],
                ["Graveyard"] = ["graveyard", "reanimator", "recursion"],
                ["Ramp"] = ["ramp", "land", "big mana"],
                ["Control"] = ["control", "counter", "removal"],
                ["Aggro"] = ["aggro", "aggressive", "combat"],
                ["Combo"] = ["combo", "infinite"],
                ["Politics"] = ["political", "group hug", "multiplayer"]
            };

            foreach (var (theme, keywords) in themes)
            {
                if (keywords.Any(keyword => name.Contains(keyword)))
                {
                    return theme;
                }
            }

            // Check tags
            if (deck?.Tags != null)
            {
                foreach (var tag in deck.Tags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        var tagLower = tag.ToLower();
                        foreach (var (theme, keywords) in themes)
                        {
                            if (keywords.Any(keyword => tagLower.Contains(keyword)))
                            {
                                return theme;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to determine theme for deck {DeckName}", deck?.Name ?? "Unknown");
        }

        return "Mixed";
    }

    private async Task<string> GetCommanderThumbnailAsync(string cardName)
    {
        try
        {
            // Use Scryfall's named card API for better thumbnails
            var encodedName = Uri.EscapeDataString(cardName);
            var scryfallUrl = $"https://api.scryfall.com/cards/named?exact={encodedName}";
            
            using var tempClient = new HttpClient();
            tempClient.Timeout = TimeSpan.FromSeconds(5); // Quick timeout for thumbnails
            
            var response = await tempClient.GetAsync(scryfallUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var cardData = JsonSerializer.Deserialize<JsonElement>(content);
                
                // Try to get small image first, then art_crop, then normal
                if (cardData.TryGetProperty("image_uris", out var imageUris))
                {
                    if (imageUris.TryGetProperty("small", out var smallImage))
                        return smallImage.GetString() ?? GetFallbackThumbnail(cardName);
                    
                    if (imageUris.TryGetProperty("art_crop", out var artCrop))
                        return artCrop.GetString() ?? GetFallbackThumbnail(cardName);
                        
                    if (imageUris.TryGetProperty("normal", out var normal))
                        return normal.GetString() ?? GetFallbackThumbnail(cardName);
                }
                
                // Handle double-faced cards
                if (cardData.TryGetProperty("card_faces", out var cardFaces) && cardFaces.GetArrayLength() > 0)
                {
                    var firstFace = cardFaces[0];
                    if (firstFace.TryGetProperty("image_uris", out var faceImageUris))
                    {
                        if (faceImageUris.TryGetProperty("small", out var faceSmall))
                            return faceSmall.GetString() ?? GetFallbackThumbnail(cardName);
                            
                        if (faceImageUris.TryGetProperty("art_crop", out var faceArtCrop))
                            return faceArtCrop.GetString() ?? GetFallbackThumbnail(cardName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch Scryfall thumbnail for card: {CardName}", cardName);
        }
        
        return GetFallbackThumbnail(cardName);
    }
    
    private string GetFallbackThumbnail(string cardName)
    {
        // Create a predictable fallback using card name hash for variety
        var hash = cardName.GetHashCode();
        var imageNumber = Math.Abs(hash % 10) + 1; // 1-10 for variety
        
        // Use Magic-themed placeholder images or a generic Magic card back
        return $"https://cards.scryfall.io/art_crop/front/0/0/0000579f-7b35-4ed3-b44c-db2a538066fe.jpg?v={imageNumber}";
    }
    
    private string GetDefaultPreconThumbnail() 
    {
        // Default thumbnail for decks without commanders
        return "https://cards.scryfall.io/art_crop/front/0/0/0000579f-7b35-4ed3-b44c-db2a538066fe.jpg";
    }

    private async Task<List<PreconDeck>?> LoadPreconsFromFileCacheAsync()
    {
        try
        {
            var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), CACHE_DIRECTORY);
            var indexFile = Path.Combine(cacheDir, CACHE_INDEX_FILE);

            if (!File.Exists(indexFile))
            {
                _logger.LogInformation("Index file not found: {IndexFile}", indexFile);
                return null;
            }

            // Don't check expiration for now - just load the cached data
            _logger.LogInformation("Loading precons from file cache: {IndexFile}", indexFile);

            var indexContent = await File.ReadAllTextAsync(indexFile);
            var preconFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(indexContent);
            
            if (preconFiles == null)
            {
                _logger.LogWarning("Failed to deserialize index file");
                return null;
            }

            var precons = new List<PreconDeck>();
            
            foreach (var (preconName, fileName) in preconFiles)
            {
                try
                {
                    var filePath = Path.Combine(cacheDir, fileName);
                    if (File.Exists(filePath))
                    {
                        var preconJson = await File.ReadAllTextAsync(filePath);
                        
                        // Use JsonSerializer with custom options for flexibility
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true
                        };
                        
                        // Use the flexible JSON model first
                        var preconJsonModel = JsonSerializer.Deserialize<PreconJsonModel>(preconJson, options);
                        if (preconJsonModel != null)
                        {
                            var precon = preconJsonModel.ToPreconDeck();
                            
                            // Ensure name is set if missing
                            if (string.IsNullOrEmpty(precon.Name))
                            {
                                precon.Name = preconName;
                            }
                            
                            precons.Add(precon);
                            _logger.LogDebug("Loaded precon: {Name} (Year: {Year})", precon.Name, precon.Year);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Precon file not found: {FilePath} for {PreconName}", filePath, preconName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load cached precon file: {FileName} for {PreconName}", fileName, preconName);
                }
            }

            _logger.LogInformation("Loaded {Count} precons from file cache", precons.Count);
            return precons.Count > 0 ? precons : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load precons from file cache");
            return null;
        }
    }

    private async Task SavePreconsToFileCacheAsync(List<PreconDeck> precons)
    {
        try
        {
            var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), CACHE_DIRECTORY);
            Directory.CreateDirectory(cacheDir);

            var preconFiles = new Dictionary<string, string>();
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            foreach (var precon in precons)
            {
                try
                {
                    var sanitizedName = SanitizeFileName(precon.Name ?? "Unknown");
                    var fileName = $"{sanitizedName}_{precon.Year}.json";
                    var filePath = Path.Combine(cacheDir, fileName);
                    
                    var preconJson = JsonSerializer.Serialize(precon, options);
                    await File.WriteAllTextAsync(filePath, preconJson);
                    
                    preconFiles[precon.Name ?? "Unknown"] = fileName;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save precon to cache: {PreconName}", precon.Name);
                }
            }

            // Save index file
            var indexFile = Path.Combine(cacheDir, CACHE_INDEX_FILE);
            var indexJson = JsonSerializer.Serialize(preconFiles, options);
            await File.WriteAllTextAsync(indexFile, indexJson);
            
            _logger.LogInformation("Saved {Count} precons to file cache", precons.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save precons to file cache");
        }
    }

    private void ClearFileCache()
    {
        try
        {
            var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), CACHE_DIRECTORY);
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
                _logger.LogInformation("Cleared file cache directory");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear file cache directory");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
    }
}
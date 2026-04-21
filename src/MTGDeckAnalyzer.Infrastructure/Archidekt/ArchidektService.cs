using System.Text.Json;
using MTGDeckAnalyzer.Application.Models;
using MTGDeckAnalyzer.Application.Services;

namespace MTGDeckAnalyzer.Infrastructure.Archidekt;

public partial class ArchidektService : IArchidektService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ArchidektService> _logger;

    private const string ArchidektBaseUrl = "https://archidekt.com";
    private const string PreconsUser = "Archidekt_Precons";
    private const string CacheKeyPrecons = "archidekt_precons";
    private const string CacheDirectory = "Cache/Precons";
    private const string CacheIndexFile = "precons_index.json";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6); // Cache for 6 hours
    private static readonly TimeSpan FileCacheDuration = TimeSpan.FromDays(7); // File cache for 7 days

    public ArchidektService(HttpClient httpClient, IMemoryCache cache, ILogger<ArchidektService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        
        // Set up HttpClient with timeout
        _httpClient.BaseAddress = new Uri(ArchidektBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "MTGDeckAnalyzer/1.0 (github.com/yourusername/mtgdeckanalyzer)");
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
    }

    public async Task<List<PreconDeck>> GetPreconDecksAsync()
    {
        // Check memory cache first
        if (_cache.TryGetValue(CacheKeyPrecons, out List<PreconDeck>? cachedPrecons) && cachedPrecons != null)
        {
            _logger.LogInformation("Retrieved {Count} precon decks from memory cache", cachedPrecons.Count);
            return cachedPrecons;
        }

        // Check file cache
        var fileCachedPrecons = await LoadPreconsFromFileCacheAsync();
        if (fileCachedPrecons != null && fileCachedPrecons.Count > 0)
        {
            _cache.Set(CacheKeyPrecons, fileCachedPrecons, CacheDuration);
            _logger.LogInformation("Retrieved {Count} precon decks from file cache", fileCachedPrecons.Count);
            return fileCachedPrecons;
        }

        try
        {
            _logger.LogInformation("Fetching precon decks from Archidekt API");
            
            // First, get the user's deck list
            var userDecks = await GetUserDecksAsync(PreconsUser);
            
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
            _cache.Set(CacheKeyPrecons, precons, CacheDuration);
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
        _cache.Remove(CacheKeyPrecons);
        ClearFileCache();
        _logger.LogInformation("Cleared Archidekt precon cache and file cache");
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MTGDeckAnalyzer.Application.Models;
using MTGDeckAnalyzer.Application.Services;

namespace MTGDeckAnalyzer.Infrastructure.Scryfall;

public class ScryfallService : IScryfallService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ScryfallService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequest = DateTime.MinValue;
    private const int RateLimitMs = 75; // Scryfall asks for 50-100ms between requests
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public ScryfallService(HttpClient httpClient, ILogger<ScryfallService> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _httpClient.BaseAddress = new Uri("https://api.scryfall.com/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MTGDeckAnalyzer/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<List<ScryfallCard>> GetCardsByNames(List<string> cardNames, CancellationToken cancellationToken = default)
    {
        var results = new List<ScryfallCard>();
        var uncachedNames = new List<string>();

        // Check cache first
        foreach (var name in cardNames)
        {
            if (_cache.TryGetValue($"card:{name.ToLowerInvariant()}", out ScryfallCard? cached) && cached != null)
            {
                results.Add(cached);
            }
            else
            {
                uncachedNames.Add(name);
            }
        }

        if (uncachedNames.Count == 0) return results;

        // Use Scryfall collection endpoint (up to 75 cards at a time)
        var batches = uncachedNames
            .Select((name, idx) => new { name, idx })
            .GroupBy(x => x.idx / 75)
            .Select(g => g.Select(x => x.name).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            var collection = new ScryfallCollection
            {
                Identifiers = batch.Select(n => new ScryfallIdentifier { Name = n }).ToList()
            };

            await RespectRateLimit();

            try
            {
                var response = await _httpClient.PostAsJsonAsync("cards/collection", collection);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ScryfallCollectionResult>();
                    if (result?.Data != null)
                    {
                        foreach (var card in result.Data)
                        {
                            _cache.Set($"card:{card.Name.ToLowerInvariant()}", card, CacheDuration);
                            if (card.CardFaces?.Count > 0)
                                _cache.Set($"card:{card.CardFaces[0].Name.ToLowerInvariant()}", card, CacheDuration);
                        }
                        results.AddRange(result.Data);
                    }

                    if (result?.NotFound.Count > 0)
                    {
                        foreach (var nf in result.NotFound)
                        {
                            _logger.LogWarning("Card not found: {CardName}", nf.Name);
                        }
                    }
                }
                else
                {
                    _logger.LogError("Scryfall API error: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cards from Scryfall");
            }
        }

        return results;
    }

    public async Task<ScryfallCard?> GetCardByName(string cardName, CancellationToken cancellationToken = default)
    {
        await RespectRateLimit();

        try
        {
            var encodedName = Uri.EscapeDataString(cardName);
            var response = await _httpClient.GetAsync($"cards/named?fuzzy={encodedName}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ScryfallCard>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching card {CardName}", cardName);
        }

        return null;
    }

    /// <summary>
    /// Fetches the official WotC game changer card names from Scryfall.
    /// Returns a HashSet containing both full DFC names and front-face-only names.
    /// </summary>
    public async Task<HashSet<string>> GetGameChangerNamesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "gamechangers";
        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cached) && cached != null)
            return cached;

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await RespectRateLimit();
            var response = await _httpClient.GetAsync("cards/search?q=is%3Agamechanger&unique=cards&order=name");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ScryfallSearchResult>();
                if (result?.Data != null)
                {
                    foreach (var card in result.Data)
                    {
                        names.Add(card.Name);
                        // Also add first face name for DFCs so we match both formats
                        var parts = card.Name.Split(" // ");
                        if (parts.Length > 1)
                            names.Add(parts[0].Trim());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch game changer list from Scryfall, will use fallback list");
        }
        if (names.Count > 0)
            _cache.Set(cacheKey, names, TimeSpan.FromHours(24));
        return names;
    }

    public async Task<List<ScryfallCard>> SearchCards(string query, CancellationToken cancellationToken = default)
    {
        await RespectRateLimit();
        var results = new List<ScryfallCard>();

        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"cards/search?q={encodedQuery}&order=edhrec");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ScryfallSearchResult>();
                if (result?.Data != null)
                {
                    results.AddRange(result.Data);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cards");
        }

        return results;
    }

    /// <summary>
    /// Fetches a card by its Scryfall API URI (e.g., from all_parts related cards).
    /// </summary>
    public async Task<ScryfallCard?> GetCardByUri(string uri, CancellationToken cancellationToken = default)
    {
        await RespectRateLimit();

        try
        {
            var response = await _httpClient.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ScryfallCard>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching card from URI {Uri}", uri);
        }

        return null;
    }

    private static async Task RespectRateLimit()
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var elapsed = (DateTime.UtcNow - _lastRequest).TotalMilliseconds;
            if (elapsed < RateLimitMs)
            {
                await Task.Delay(RateLimitMs - (int)elapsed);
            }
            _lastRequest = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    /// <summary>
    /// For each card name, searches all printings and returns the cheapest EUR price (English, non-foil, paper)
    /// along with the Cardmarket link and image URI from that cheapest print.
    /// </summary>
    public async Task<Dictionary<string, CheapestPrintInfo>> GetCheapestEurPrices(List<string> cardNames, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, CheapestPrintInfo>(StringComparer.OrdinalIgnoreCase);

        // Check cache first
        var uncachedNames = new List<string>();
        foreach (var name in cardNames)
        {
            if (_cache.TryGetValue($"price:{name.ToLowerInvariant()}", out CheapestPrintInfo? cached) && cached != null)
                result[name] = cached;
            else
                uncachedNames.Add(name);
        }

        if (uncachedNames.Count == 0) return result;

        // Batch card names into groups of 8 for OR-based search queries
        var batches = uncachedNames
            .Select((name, idx) => new { name, idx })
            .GroupBy(x => x.idx / 8)
            .Select(g => g.Select(x => x.name).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            try
            {
                var orClauses = string.Join(" OR ", batch.Select(n => $"!\"{n}\""));
                var query = $"({orClauses}) lang:en -is:digital";
                var encodedQuery = Uri.EscapeDataString(query);

                await RespectRateLimit();
                var response = await _httpClient.GetAsync($"cards/search?q={encodedQuery}&unique=prints&order=eur&dir=asc");
                if (!response.IsSuccessStatusCode) continue;

                var searchResult = await response.Content.ReadFromJsonAsync<ScryfallSearchResult>();
                if (searchResult?.Data == null) continue;

                foreach (var card in searchResult.Data)
                {
                    var cardName = card.Name;
                    var frontFace = card.Name.Split(" // ")[0].Trim();

                    if (card.Prices?.Eur != null &&
                        double.TryParse(card.Prices.Eur, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var eurPrice) &&
                        eurPrice > 0)
                    {
                        var imageUri = card.ImageUris?.Normal
                            ?? card.CardFaces?.FirstOrDefault()?.ImageUris?.Normal
                            ?? string.Empty;
                        var cardmarketUri = card.PurchaseUris?.Cardmarket ?? string.Empty;

                        foreach (var batchName in batch)
                        {
                            if (string.Equals(batchName, cardName, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(batchName, frontFace, StringComparison.OrdinalIgnoreCase))
                            {
                                if (!result.TryGetValue(batchName, out var existing) || eurPrice < existing.PriceEur)
                                {
                                    var info = new CheapestPrintInfo
                                    {
                                        PriceEur = eurPrice,
                                        CardmarketUri = cardmarketUri,
                                        ImageUri = imageUri
                                    };
                                    result[batchName] = info;
                                    _cache.Set($"price:{batchName.ToLowerInvariant()}", info, CacheDuration);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching cheapest prints for batch");
            }
        }

        return result;
    }
}


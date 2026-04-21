using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public class PreconService : IPreconService
{
    private readonly IArchidektService _archidektService;
    private readonly ILogger<PreconService> _logger;

    public PreconService(IArchidektService archidektService, ILogger<PreconService> logger)
    {
        _archidektService = archidektService;
        _logger = logger;
    }

    public async Task<PreconSearchResult> SearchPreconsAsync(string? query = null, string? year = null, string[]? colors = null, int page = 1, int pageSize = 20)
    {
        try
        {
            // Get all precons from Archidekt (which prioritizes file cache)
            var allPrecons = await _archidektService.GetPreconDecksAsync();
            
            if (allPrecons.Count == 0)
            {
                _logger.LogWarning("No precons loaded from ArchidektService, falling back to static data");
                return GetFallbackPrecons(query, year, colors, page, pageSize);
            }
            
            var (precons, total) = ApplyFiltersAndPage(allPrecons, query, year, colors, page, pageSize);

            _logger.LogInformation("Returning {Count} of {Total} precons for query: {Query}, year: {Year}, colors: {Colors}", 
                precons.Count, total, query, year, colors != null ? string.Join(",", colors) : "none");

            return new PreconSearchResult { Precons = precons, TotalCount = total };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search precons");
            return GetFallbackPrecons(query, year, colors, page, pageSize);
        }
    }

    public async Task<PreconDeck?> GetPreconByNameAsync(string name)
    {
        try
        {
            var allPrecons = await _archidektService.GetPreconDecksAsync();
            var precon = allPrecons.FirstOrDefault(p => 
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            
            if (precon != null)
            {
                _logger.LogInformation("Found precon: {Name}", name);
            }
            else
            {
                _logger.LogWarning("Precon not found: {Name}", name);
            }
            
            return precon;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get precon by name: {Name}", name);
            
            // Try fallback static data
            return GetStaticPreconByName(name);
        }
    }

    private PreconSearchResult GetFallbackPrecons(string? query = null, string? year = null, string[]? colors = null, int page = 1, int pageSize = 20)
    {
        _logger.LogWarning("Using fallback static precon data");
        var (precons, total) = ApplyFiltersAndPage(GetStaticPrecons(), query, year, colors, page, pageSize);
        return new PreconSearchResult { Precons = precons, TotalCount = total };
    }

    /// <summary>Applies search filters, sort, and pagination to a precon list.</summary>
    private static (List<PreconDeck> precons, int total) ApplyFiltersAndPage(
        List<PreconDeck> source,
        string? query,
        string? year,
        string[]? colors,
        int page,
        int pageSize)
    {
        var filtered = source.AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            var q = query.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.ToLowerInvariant().Contains(q) ||
                p.Commanders.Any(c => c.ToLowerInvariant().Contains(q)) ||
                p.Theme.ToLowerInvariant().Contains(q));
        }

        if (!string.IsNullOrEmpty(year))
            filtered = filtered.Where(p => p.Year == year);

        if (colors is { Length: > 0 })
        {
            var colorSet = colors.Select(c => c.ToUpperInvariant()).ToHashSet();
            filtered = filtered.Where(p => p.ColorIdentity.Any(ci => colorSet.Contains(ci)));
        }

        var total = filtered.Count();
        var precons = filtered
            .AsEnumerable()
            .OrderByDescending(p => int.TryParse(p.Year, out int y) ? y : 0)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (precons, total);
    }

    private PreconDeck? GetStaticPreconByName(string name)
    {
        var staticPrecons = GetStaticPrecons();
        return staticPrecons.FirstOrDefault(p => 
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private List<PreconDeck> GetStaticPrecons()
    {
        return new List<PreconDeck>
        {
            // Commander 2011
            new PreconDeck
            {
                Name = "Heavenly Inferno",
                Commanders = ["Kaalia of the Vast"],
                Year = "2011",
                ColorIdentity = ["W", "B", "R"],
                Theme = "Angels, Demons, Dragons",
                ImageUrl = "https://cards.scryfall.io/art_crop/front/a/0/a0cc9eaf-c8d9-4da2-8fd8-8d423a02a3a8.jpg?1599708091",
                DeckList = "1 Kaalia of the Vast\n1 Command Tower\n1 Nomad Outpost\n1 Boros Garrison\n1 Orzhov Basilica\n1 Rakdos Carnarium\n1 Temple of the False God\n1 Bojuka Bog\n1 Reliquary Tower\n1 Barren Moor\n1 Forgotten Cave\n1 Secluded Steppe\n1 Evolving Wilds\n1 Terramorphic Expanse\n8 Plains\n8 Swamp\n8 Mountain\n1 Sol Ring\n1 Boros Signet\n1 Orzhov Signet\n1 Rakdos Signet\n1 Lightning Greaves\n1 Swiftfoot Boots"
            },
            
            // Commander 2020
            new PreconDeck
            {
                Name = "Arcane Maelstrom",
                Commanders = ["Kalamax, the Stormsire"],
                Year = "2020",
                ColorIdentity = ["U", "R", "G"],
                Theme = "Spells Matter",
                ImageUrl = "https://cards.scryfall.io/art_crop/front/f/9/f990cd78-2165-446f-a116-ae55d7a0f00d.jpg?1568003927",
                DeckList = "1 Kalamax, the Stormsire\n1 Command Tower\n1 Exotic Orchard\n1 Frontier Bivouac\n1 Opal Palace\n1 Temple of the False God"
            }
        };
    }
}
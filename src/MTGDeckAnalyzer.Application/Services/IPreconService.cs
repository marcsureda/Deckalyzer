using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

/// <summary>Provides search and lookup for official Magic preconstructed Commander decks.</summary>
public interface IPreconService
{
    Task<PreconSearchResult> SearchPreconsAsync(string? query = null, string? year = null, string[]? colors = null, int page = 1, int pageSize = 20);
    Task<PreconDeck?> GetPreconByNameAsync(string name);
}

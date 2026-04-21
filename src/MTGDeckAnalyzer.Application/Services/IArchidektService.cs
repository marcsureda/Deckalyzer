using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

/// <summary>Fetches preconstructed deck data from Archidekt.</summary>
public interface IArchidektService
{
    Task<List<PreconDeck>> GetPreconDecksAsync();
    Task<PreconDeck?> GetPreconDeckByIdAsync(int deckId);
    void ClearCache();
}

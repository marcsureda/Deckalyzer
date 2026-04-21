namespace MTGDeckAnalyzer.Application.Services;

/// <summary>Parses raw decklist text into structured card data.</summary>
public interface IDeckParser
{
    /// <summary>
    /// Parses a raw decklist string and returns identified commanders,
    /// main deck cards, sideboard cards, and any parse warnings.
    /// </summary>
    (List<string> commanders, List<(int quantity, string name)> mainCards, List<(int quantity, string name)> sideboardCards, List<string> warnings)
        ParseDeckList(string deckList);
}

using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

/// <summary>Analyzes individual cards and full decks to produce power-level metrics.</summary>
public interface IPowerLevelAnalyzer
{
    /// <summary>
    /// Updates the dynamic game changer list fetched from Scryfall.
    /// Thread-safe: replaces the reference atomically.
    /// </summary>
    void SetDynamicGameChangers(HashSet<string> names);

    /// <summary>Analyzes a single card and returns a populated <see cref="CardInfo"/>.</summary>
    CardInfo AnalyzeCard(ScryfallCard scryfall, bool isCommander);

    /// <summary>Runs full deck analysis over all analyzed cards and returns the final result.</summary>
    DeckAnalysisResult AnalyzeDeck(List<CardInfo> cards);
}

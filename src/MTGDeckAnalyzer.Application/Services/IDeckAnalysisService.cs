using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

/// <summary>Orchestrates the full deck analysis pipeline.</summary>
public interface IDeckAnalysisService
{
    /// <summary>
    /// Parses, fetches card data, analyzes power level, enriches prices and images,
    /// and returns a complete <see cref="DeckAnalysisResult"/>.
    /// </summary>
    Task<DeckAnalysisResult> AnalyzeDeck(string deckList, CancellationToken cancellationToken = default);
}

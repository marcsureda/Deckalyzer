namespace MTGDeckAnalyzer.Application.Models;

/// <summary>Request model for deck analysis. Immutable by design — parsed once and not modified.</summary>
public record DeckAnalysisRequest(string DeckList = "");

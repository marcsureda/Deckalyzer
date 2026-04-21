using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

/// <summary>Scryfall API abstraction for card data, prices, and search.</summary>
public interface IScryfallService
{
    /// <summary>Fetches cards by name using the collection endpoint (batch of up to 75).</summary>
    Task<List<ScryfallCard>> GetCardsByNames(List<string> cardNames, CancellationToken cancellationToken = default);

    /// <summary>Fetches a single card by fuzzy name match.</summary>
    Task<ScryfallCard?> GetCardByName(string cardName, CancellationToken cancellationToken = default);

    /// <summary>Fetches a card by its Scryfall API URI (e.g., from all_parts related cards).</summary>
    Task<ScryfallCard?> GetCardByUri(string uri, CancellationToken cancellationToken = default);

    /// <summary>Fetches the official WotC game changer card names from Scryfall's is:gamechanger search.</summary>
    Task<HashSet<string>> GetGameChangerNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>Searches cards by a Scryfall query string.</summary>
    Task<List<ScryfallCard>> SearchCards(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// For each card name, returns the cheapest English non-foil EUR price,
    /// Cardmarket link, and image URI from that cheapest print.
    /// </summary>
    Task<Dictionary<string, CheapestPrintInfo>> GetCheapestEurPrices(List<string> cardNames, CancellationToken cancellationToken = default);
}

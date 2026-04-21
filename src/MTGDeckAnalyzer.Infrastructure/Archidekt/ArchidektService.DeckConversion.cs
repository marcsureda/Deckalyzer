using System.Text.Json;
using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Infrastructure.Archidekt;

public partial class ArchidektService
{
    private async Task<ArchidektDeck> GetFullDeckAsync(int deckId)
    {
        var response = await _httpClient.GetAsync($"/api/decks/{deckId}/");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch deck {deckId}: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        var deck = JsonSerializer.Deserialize<ArchidektDeck>(content, options);
        return deck ?? throw new InvalidOperationException($"Failed to deserialize deck {deckId}");
    }

    private async Task<PreconDeck> ConvertToPreconDeckAsync(ArchidektDeck archidektDeck, ArchidektDeckSummary? summary)
    {
        if (archidektDeck == null)
        {
            throw new ArgumentNullException(nameof(archidektDeck));
        }

        // Extract commanders (cards in "Commander" category)
        var commanders = (archidektDeck.Cards ?? new List<ArchidektCard>())
            .Where(c => c?.Categories != null && c.Categories.Contains("Commander", StringComparer.OrdinalIgnoreCase))
            .Where(c => c?.Card?.OracleCard?.Name != null)
            .Select(c => c.Card.OracleCard.Name)
            .ToArray();

        // Determine color identity from commanders and/or all cards
        var colorIdentity = DetermineColorIdentity(archidektDeck.Cards ?? new List<ArchidektCard>());

        // Convert deck list to string format
        var deckList = ConvertToDeckList(archidektDeck.Cards ?? new List<ArchidektCard>());

        // Extract year from creation date or tags
        var year = ExtractYear(archidektDeck, summary);

        // Calculate total price
        var totalPrice = CalculateTotalPrice(archidektDeck.Cards ?? new List<ArchidektCard>());

        // Determine theme from deck name and tags
        var theme = DetermineTheme(archidektDeck);

        // Get commander image URL (use first commander with better Scryfall integration)
        var imageUrl = commanders.Length > 0 ? 
            await GetCommanderThumbnailAsync(commanders[0]) : 
            GetDefaultPreconThumbnail();

        return new PreconDeck
        {
            Name = archidektDeck.Name ?? "Unknown Deck",
            Year = year,
            Commanders = commanders,
            ColorIdentity = colorIdentity,
            DeckList = deckList,
            Theme = theme,
            ImageUrl = imageUrl,
            Price = totalPrice
        };
    }

    private string[] DetermineColorIdentity(List<ArchidektCard> cards)
    {
        var allColors = new HashSet<string>();
        
        foreach (var card in cards)
        {
            if (card?.Card?.OracleCard?.ColorIdentity != null)
            {
                foreach (var color in card.Card.OracleCard.ColorIdentity)
                {
                    if (!string.IsNullOrEmpty(color))
                    {
                        allColors.Add(color);
                    }
                }
            }
        }

        // Convert full names to single letters if needed
        var colorMap = new Dictionary<string, string>
        {
            { "White", "W" }, { "Blue", "U" }, { "Black", "B" }, { "Red", "R" }, { "Green", "G" }
        };

        var result = new List<string>();
        foreach (var color in allColors)
        {
            if (colorMap.TryGetValue(color, out var shortColor))
            {
                result.Add(shortColor);
            }
            else if (color.Length == 1 && "WUBRG".Contains(color))
            {
                result.Add(color);
            }
        }

        return result.OrderBy(c => "WUBRG".IndexOf(c)).ToArray();
    }

    private string ConvertToDeckList(List<ArchidektCard> cards)
    {
        var lines = new List<string>();
        
        if (cards != null)
        {
            // Exclude commander cards to prevent duplication - commanders are handled separately
            var validCards = cards
                .Where(c => c?.Card?.OracleCard?.Name != null)
                .Where(c => c?.Categories == null || !c.Categories.Contains("Commander", StringComparer.OrdinalIgnoreCase))
                .OrderBy(c => c.Card.OracleCard.Name);
            
            foreach (var card in validCards)
            {
                try
                {
                    lines.Add($"{card.Quantity} {card.Card.OracleCard.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process card for deck list: {CardId}", card?.Card?.OracleCard?.Name ?? "Unknown");
                }
            }
        }

        return string.Join("\n", lines);
    }

    private string ExtractYear(ArchidektDeck deck, ArchidektDeckSummary? summary)
    {
        try
        {
            // Try to extract year from deck name or tags
            var name = (deck?.Name ?? "").ToLower();
            
            // Look for common precon patterns
            var yearPatterns = new[]
            {
                @"\b(19|20)\d{2}\b",  // 4-digit year
                @"\bc(\d{2})\b",      // C20, C21, etc.
                @"\bcmdr?\s*(\d{2})\b" // CMDR20, CMD21, etc.
            };

            foreach (var pattern in yearPatterns)
            {
                try
                {
                    var match = System.Text.RegularExpressions.Regex.Match(name, pattern);
                    if (match.Success)
                    {
                        var yearStr = match.Groups[1].Value;
                        if (yearStr.Length == 2 && int.TryParse(yearStr, out var shortYear))
                        {
                            return shortYear > 50 ? $"19{yearStr}" : $"20{yearStr}";
                        }
                        return yearStr;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse year pattern {Pattern} for deck {DeckName}", pattern, name);
                }
            }

            // Default to creation year
            if (!string.IsNullOrEmpty(deck?.CreatedAt) && DateTime.TryParse(deck.CreatedAt, out var createdDate))
            {
                return createdDate.Year.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract year from deck {DeckName}", deck?.Name ?? "Unknown");
        }

        return "Unknown";
    }

    private decimal? CalculateTotalPrice(List<ArchidektCard> cards)
    {
        try
        {
            decimal total = 0;
            foreach (var card in cards ?? new List<ArchidektCard>())
            {
                if (card?.Card?.Prices != null && card.Quantity > 0)
                {
                    var price = card.Card.Prices.Tcg;
                    if (price > 0)
                    {
                        total += card.Quantity * price;
                    }
                }
            }
            return total > 0 ? total : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate deck price");
            return null;
        }
    }

    private string DetermineTheme(ArchidektDeck deck)
    {
        try
        {
            // Extract theme from deck name or tags
            var name = (deck?.Name ?? "").ToLower();
            
            // Common precon themes
            var themes = new Dictionary<string, string[]>
            {
                ["Tribal"] = ["tribal", "creature type", "typal"],
                ["Spellslinger"] = ["spellslinger", "instant", "sorcery", "spell"],
                ["Voltron"] = ["voltron", "equipment", "aura"],
                ["Token"] = ["token", "populate", "create"],
                ["Graveyard"] = ["graveyard", "reanimator", "recursion"],
                ["Ramp"] = ["ramp", "land", "big mana"],
                ["Control"] = ["control", "counter", "removal"],
                ["Aggro"] = ["aggro", "aggressive", "combat"],
                ["Combo"] = ["combo", "infinite"],
                ["Politics"] = ["political", "group hug", "multiplayer"]
            };

            foreach (var (theme, keywords) in themes)
            {
                if (keywords.Any(keyword => name.Contains(keyword)))
                {
                    return theme;
                }
            }

            // Check tags
            if (deck?.Tags != null)
            {
                foreach (var tag in deck.Tags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        var tagLower = tag.ToLower();
                        foreach (var (theme, keywords) in themes)
                        {
                            if (keywords.Any(keyword => tagLower.Contains(keyword)))
                            {
                                return theme;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to determine theme for deck {DeckName}", deck?.Name ?? "Unknown");
        }

        return "Mixed";
    }

    private async Task<string> GetCommanderThumbnailAsync(string cardName)
    {
        try
        {
            // Use Scryfall's named card API for better thumbnails
            var encodedName = Uri.EscapeDataString(cardName);
            var scryfallUrl = $"https://api.scryfall.com/cards/named?exact={encodedName}";
            
            using var tempClient = new HttpClient();
            tempClient.Timeout = TimeSpan.FromSeconds(5); // Quick timeout for thumbnails
            
            var response = await tempClient.GetAsync(scryfallUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var cardData = JsonSerializer.Deserialize<JsonElement>(content);
                
                // Try to get small image first, then art_crop, then normal
                if (cardData.TryGetProperty("image_uris", out var imageUris))
                {
                    if (imageUris.TryGetProperty("small", out var smallImage))
                        return smallImage.GetString() ?? GetFallbackThumbnail(cardName);
                    
                    if (imageUris.TryGetProperty("art_crop", out var artCrop))
                        return artCrop.GetString() ?? GetFallbackThumbnail(cardName);
                        
                    if (imageUris.TryGetProperty("normal", out var normal))
                        return normal.GetString() ?? GetFallbackThumbnail(cardName);
                }
                
                // Handle double-faced cards
                if (cardData.TryGetProperty("card_faces", out var cardFaces) && cardFaces.GetArrayLength() > 0)
                {
                    var firstFace = cardFaces[0];
                    if (firstFace.TryGetProperty("image_uris", out var faceImageUris))
                    {
                        if (faceImageUris.TryGetProperty("small", out var faceSmall))
                            return faceSmall.GetString() ?? GetFallbackThumbnail(cardName);
                            
                        if (faceImageUris.TryGetProperty("art_crop", out var faceArtCrop))
                            return faceArtCrop.GetString() ?? GetFallbackThumbnail(cardName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch Scryfall thumbnail for card: {CardName}", cardName);
        }
        
        return GetFallbackThumbnail(cardName);
    }
    
    private string GetFallbackThumbnail(string cardName)
    {
        // Create a predictable fallback using card name hash for variety
        var hash = cardName.GetHashCode();
        var imageNumber = Math.Abs(hash % 10) + 1; // 1-10 for variety
        
        // Use Magic-themed placeholder images or a generic Magic card back
        return $"https://cards.scryfall.io/art_crop/front/0/0/0000579f-7b35-4ed3-b44c-db2a538066fe.jpg?v={imageNumber}";
    }
    
    private string GetDefaultPreconThumbnail() 
    {
        // Default thumbnail for decks without commanders
        return "https://cards.scryfall.io/art_crop/front/0/0/0000579f-7b35-4ed3-b44c-db2a538066fe.jpg";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
    }
}

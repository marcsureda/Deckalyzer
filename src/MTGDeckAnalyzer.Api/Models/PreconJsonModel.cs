using System.Text.Json.Serialization;

namespace MTGDeckAnalyzer.Api.Models;

/// <summary>
/// Model for deserializing precon JSON files with flexible property handling
/// </summary>
public class PreconJsonModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("year")]
    public object Year { get; set; } = string.Empty;
    
    [JsonPropertyName("commanders")]
    public string[] Commanders { get; set; } = [];
    
    [JsonPropertyName("colorIdentity")]
    public string[] ColorIdentity { get; set; } = [];
    
    [JsonPropertyName("deckList")]
    public string DeckList { get; set; } = string.Empty;
    
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = string.Empty;
    
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("price")]
    public string? Price { get; set; }

    /// <summary>
    /// Convert to PreconDeck model
    /// </summary>
    public PreconDeck ToPreconDeck()
    {
        return new PreconDeck
        {
            Name = Name,
            Year = Year?.ToString() ?? string.Empty,
            Commanders = Commanders ?? [],
            ColorIdentity = ColorIdentity ?? [],
            DeckList = DeckList,
            Theme = Theme,
            ImageUrl = ImageUrl,
            Price = decimal.TryParse(Price?.Replace("$", "").Replace("€", ""), out var price) ? price : null
        };
    }
}
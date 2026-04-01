using System.Text.Json.Serialization;

namespace MTGDeckAnalyzer.Api.Models;

public class ScryfallCard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cmc")]
    public double Cmc { get; set; }

    [JsonPropertyName("mana_cost")]
    public string? ManaCost { get; set; }

    [JsonPropertyName("type_line")]
    public string TypeLine { get; set; } = string.Empty;

    [JsonPropertyName("oracle_text")]
    public string? OracleText { get; set; }

    [JsonPropertyName("color_identity")]
    public List<string> ColorIdentity { get; set; } = [];

    [JsonPropertyName("colors")]
    public List<string>? Colors { get; set; }

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = [];

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = string.Empty;

    [JsonPropertyName("edhrec_rank")]
    public int? EdhrecRank { get; set; }

    [JsonPropertyName("prices")]
    public ScryfallPrices? Prices { get; set; }

    [JsonPropertyName("image_uris")]
    public ScryfallImageUris? ImageUris { get; set; }

    [JsonPropertyName("card_faces")]
    public List<ScryfallCardFace>? CardFaces { get; set; }

    [JsonPropertyName("scryfall_uri")]
    public string ScryfallUri { get; set; } = string.Empty;

    [JsonPropertyName("prints_search_uri")]
    public string? PrintsSearchUri { get; set; }

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = string.Empty;

    [JsonPropertyName("legalities")]
    public Dictionary<string, string>? Legalities { get; set; }

    [JsonPropertyName("game_changer")]
    public bool GameChanger { get; set; }

    [JsonPropertyName("reserved")]
    public bool Reserved { get; set; }

    [JsonPropertyName("all_parts")]
    public List<ScryfallRelatedCard>? AllParts { get; set; }

    [JsonPropertyName("purchase_uris")]
    public ScryfallPurchaseUris? PurchaseUris { get; set; }

    [JsonPropertyName("power")]
    public string? Power { get; set; }

    [JsonPropertyName("toughness")]
    public string? Toughness { get; set; }
}

public class ScryfallPurchaseUris
{
    [JsonPropertyName("tcgplayer")]
    public string? TcgPlayer { get; set; }

    [JsonPropertyName("cardmarket")]
    public string? Cardmarket { get; set; }

    [JsonPropertyName("cardhoarder")]
    public string? Cardhoarder { get; set; }
}

public class ScryfallRelatedCard
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("component")]
    public string Component { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type_line")]
    public string TypeLine { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

public class ScryfallCardFace
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mana_cost")]
    public string? ManaCost { get; set; }

    [JsonPropertyName("type_line")]
    public string? TypeLine { get; set; }

    [JsonPropertyName("oracle_text")]
    public string? OracleText { get; set; }

    [JsonPropertyName("image_uris")]
    public ScryfallImageUris? ImageUris { get; set; }
}

public class ScryfallPrices
{
    [JsonPropertyName("usd")]
    public string? Usd { get; set; }

    [JsonPropertyName("usd_foil")]
    public string? UsdFoil { get; set; }

    [JsonPropertyName("eur")]
    public string? Eur { get; set; }

    [JsonPropertyName("eur_foil")]
    public string? EurFoil { get; set; }
}

public class ScryfallImageUris
{
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    [JsonPropertyName("normal")]
    public string? Normal { get; set; }

    [JsonPropertyName("large")]
    public string? Large { get; set; }

    [JsonPropertyName("png")]
    public string? Png { get; set; }
}

public class ScryfallSearchResult
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("total_cards")]
    public int TotalCards { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("data")]
    public List<ScryfallCard> Data { get; set; } = [];
}

public class ScryfallCollection
{
    [JsonPropertyName("identifiers")]
    public List<ScryfallIdentifier> Identifiers { get; set; } = [];
}

public class ScryfallIdentifier
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class ScryfallCollectionResult
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("not_found")]
    public List<ScryfallIdentifier> NotFound { get; set; } = [];

    [JsonPropertyName("data")]
    public List<ScryfallCard> Data { get; set; } = [];
}

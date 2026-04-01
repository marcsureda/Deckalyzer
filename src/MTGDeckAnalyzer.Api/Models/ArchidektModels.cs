namespace MTGDeckAnalyzer.Api.Models;

// Models for Archidekt API responses
public class ArchidektDeck
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public int DeckFormat { get; set; }
    public List<ArchidektCard> Cards { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<ArchidektCategory> Categories { get; set; } = [];
    public object? CustomCards { get; set; }
}

public class ArchidektCard
{
    public long Id { get; set; }  // Changed from int to long
    public List<string> Categories { get; set; } = [];
    public bool Companion { get; set; }
    public int Quantity { get; set; }
    public ArchidektCardDetail Card { get; set; } = new();
}

public class ArchidektCardDetail
{
    public int Id { get; set; }
    public string Artist { get; set; } = string.Empty;
    public string CollectorNumber { get; set; } = string.Empty;
    public string ReleasedAt { get; set; } = string.Empty;
    public ArchidektEdition Edition { get; set; } = new();
    public string Flavor { get; set; } = string.Empty;
    public ArchidektOracleCard OracleCard { get; set; } = new();
    public ArchidektPrices Prices { get; set; } = new();
    public string Rarity { get; set; } = string.Empty;
}

public class ArchidektEdition
{
    public string EditionCode { get; set; } = string.Empty;
    public string EditionName { get; set; } = string.Empty;
    public string EditionDate { get; set; } = string.Empty;
    public string EditionType { get; set; } = string.Empty;
}

public class ArchidektOracleCard
{
    public int Id { get; set; }
    public int Cmc { get; set; }
    public List<string> ColorIdentity { get; set; } = [];
    public List<string> Colors { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public string Power { get; set; } = string.Empty;
    public string Toughness { get; set; } = string.Empty;
    public List<string> Types { get; set; } = [];
    public List<string> SubTypes { get; set; } = [];
    public List<string> SuperTypes { get; set; } = [];
    public string Text { get; set; } = string.Empty;
    public string ManaCost { get; set; } = string.Empty;
    public string? Loyalty { get; set; }
}

public class ArchidektPrices
{
    public decimal Tcg { get; set; }
    public decimal TcgFoil { get; set; }
    public decimal Ck { get; set; }
    public decimal CkFoil { get; set; }
}

public class ArchidektCategory
{
    public string Name { get; set; } = string.Empty;
}

// Search response for user decks
public class ArchidektSearchResponse
{
    public List<ArchidektDeckSummary> Results { get; set; } = [];
    public int Count { get; set; }
}

public class ArchidektDeckSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public int DeckFormat { get; set; }
    public List<string> Tags { get; set; } = [];
    public ArchidektOwner Owner { get; set; } = new();
}

public class ArchidektOwner
{
    public string Username { get; set; } = string.Empty;
    public int Id { get; set; }
}
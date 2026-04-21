namespace MTGDeckAnalyzer.Application.Models;

public class DeckAnalysisResult
{
    public string CommanderName { get; set; } = string.Empty;
    public string CommanderImageUri { get; set; } = string.Empty;
    public List<string> CommanderNames { get; set; } = [];
    public List<string> CommanderImageUris { get; set; } = [];
    public List<string> ColorIdentity { get; set; } = [];
    public int TotalCards { get; set; }

    // Power Metrics
    public double PowerLevel { get; set; }
    public int Bracket { get; set; }
    public string BracketName { get; set; } = string.Empty;
    public double Efficiency { get; set; }
    public double TotalImpact { get; set; }
    public double Score { get; set; }
    public double AveragePlayability { get; set; }
    public double AverageCmc { get; set; }
    public double TippingPoint { get; set; }

    // Bracket Details
    public BracketDetails BracketDetails { get; set; } = new();

    // Deck Composition
    public DeckComposition Composition { get; set; } = new();

    // Mana Analysis
    public ManaAnalysis ManaAnalysis { get; set; } = new();

    // Cards
    public List<CardInfo> Cards { get; set; } = [];

    // Recommendations
    public List<CardRecommendation> CutRecommendations { get; set; } = [];
    public List<CardRecommendation> AddRecommendations { get; set; } = [];

    // Strengths and Weaknesses
    public List<string> Strengths { get; set; } = [];
    public List<string> Weaknesses { get; set; } = [];

    // Warnings (undetected cards, card count issues)
    public List<string> Warnings { get; set; } = [];

    // Detected Combos with descriptions
    public List<ComboInfo> Combos { get; set; } = [];

    // Strategy / archetype analysis
    public DeckStrategy Strategy { get; set; } = new();

    // Tokens produced by cards in the deck
    public List<TokenInfo> Tokens { get; set; } = [];

    // Deck total value (cheapest English prints, EUR)
    public double DeckTotalValueEur { get; set; }
}

public class BracketDetails
{
    public bool HasExtraTurns { get; set; }
    public bool HasChainingExtraTurns { get; set; }
    public bool HasMassLandDenial { get; set; }
    public bool HasTwoCardCombos { get; set; }
    public bool HasOnlyLateGameCombos { get; set; }
    public int GameChangerCount { get; set; }
    public List<string> ExtraTurnCards { get; set; } = [];
    public List<string> MassLandDenialCards { get; set; } = [];
    public List<string> ComboCards { get; set; } = [];
    public List<string> GameChangerCards { get; set; } = [];
    public List<BracketRequirement> Requirements { get; set; } = [];
}

public class BracketRequirement
{
    public int Bracket { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<BracketRule> Rules { get; set; } = [];
    public bool Passes { get; set; }
}

public class BracketRule
{
    public string Description { get; set; } = string.Empty;
    public bool Passes { get; set; }
}

public class DeckComposition
{
    public int Creatures { get; set; }
    public int Instants { get; set; }
    public int Sorceries { get; set; }
    public int Artifacts { get; set; }
    public int Enchantments { get; set; }
    public int Planeswalkers { get; set; }
    public int Lands { get; set; }
    public int Other { get; set; }
    public int Ramp { get; set; }
    public int CardDraw { get; set; }
    public int Removal { get; set; }
    public int BoardWipes { get; set; }
    public int Counterspells { get; set; }
    public int Tutors { get; set; }
}

public class ManaAnalysis
{
    public Dictionary<string, int> ColorSymbols { get; set; } = new();
    public Dictionary<string, int> ColorProducers { get; set; } = new();
    public Dictionary<int, int> ManaCurve { get; set; } = new();
    public double ManaScrew { get; set; }
    public double ManaFlood { get; set; }
    public double SweetSpot { get; set; }
}

public class CardRecommendation
{
    public string CardName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double EstimatedImpact { get; set; }
    public string ImageUri { get; set; } = string.Empty;
}

public class ComboInfo
{
    public List<string> Cards { get; set; } = [];
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsInfinite { get; set; }
    public string InfiniteEffect { get; set; } = string.Empty;
}

public class DeckStrategy
{
    public string PrimaryArchetype { get; set; } = string.Empty;
    public List<StrategyTag> Tags { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
}

public class StrategyTag
{
    public string Name { get; set; } = string.Empty;
    public int CardCount { get; set; }
    public double Percentage { get; set; }
    public List<string> ExampleCards { get; set; } = [];
}

public class TokenInfo
{
    public string Description { get; set; } = string.Empty;
    public string ImageUri { get; set; } = string.Empty;
    public List<string> ProducedBy { get; set; } = [];
}

public class PreconDeck
{
    public string Name { get; set; } = string.Empty;
    
    private object? _year;
    public string Year 
    { 
        get => _year?.ToString() ?? string.Empty;
        set => _year = value;
    }
    
    // Support both array and list deserialization
    public string[] Commanders { get; set; } = [];
    public string[] ColorIdentity { get; set; } = [];
    public string DeckList { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    
    // Property to handle numeric year from JSON
    public int YearNumber
    {
        get => int.TryParse(Year, out int year) ? year : 0;
        set => _year = value;
    }
}

public class PreconSearchResult
{
    public List<PreconDeck> Precons { get; set; } = [];
    public int TotalCount { get; set; }
}

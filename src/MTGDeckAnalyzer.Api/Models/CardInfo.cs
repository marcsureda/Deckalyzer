namespace MTGDeckAnalyzer.Api.Models;

public class CardInfo
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public double Cmc { get; set; }
    public string ManaCost { get; set; } = string.Empty;
    public string TypeLine { get; set; } = string.Empty;
    public string OracleText { get; set; } = string.Empty;
    public List<string> ColorIdentity { get; set; } = [];
    public List<string> Colors { get; set; } = [];
    public List<string> Keywords { get; set; } = [];
    public string Rarity { get; set; } = string.Empty;
    public double EdhrecRank { get; set; }
    public double Price { get; set; }
    public double PriceEur { get; set; }
    public string ImageUri { get; set; } = string.Empty;
    public string ScryfallUri { get; set; } = string.Empty;
    public bool IsCommander { get; set; }
    public bool IsLand { get; set; }
    public bool IsCreature { get; set; }
    public bool IsArtifact { get; set; }
    public bool IsEnchantment { get; set; }
    public bool IsInstant { get; set; }
    public bool IsSorcery { get; set; }
    public bool IsPlaneswalker { get; set; }

    // Analysis fields
    public double PowerScore { get; set; }
    public double Playability { get; set; }
    public double Impact { get; set; }
    public bool IsGameChanger { get; set; }
    public bool IsTutor { get; set; }
    public bool IsExtraTurn { get; set; }
    public bool IsMassLandDenial { get; set; }
    public bool IsInfiniteComboPiece { get; set; }
    public bool IsFastMana { get; set; }
    public bool IsCounterspell { get; set; }
    public bool IsBoardWipe { get; set; }
    public bool IsRemoval { get; set; }
    public bool IsCardDraw { get; set; }
    public bool IsRamp { get; set; }

    /// <summary>Synergy with the commander (0-100). Higher = more synergistic.</summary>
    public double Synergy { get; set; }

    /// <summary>Cardmarket URL for the cheapest English print.</summary>
    public string CardmarketUri { get; set; } = string.Empty;

    /// <summary>Image URI from the cheapest print (for consistency).</summary>
    public string ImageUriCheapest { get; set; } = string.Empty;
}

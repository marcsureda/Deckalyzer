using MTGDeckAnalyzer.Application.Models;
using MTGDeckAnalyzer.Application.Services;
using Xunit;

namespace MTGDeckAnalyzer.Application.Tests;

public class PowerLevelAnalyzerTests
{
    private readonly PowerLevelAnalyzer _sut = new();

    private static ScryfallCard MakeCard(string name, string typeLine, string oracleText = "", int edhrecRank = 5000)
    {
        return new ScryfallCard
        {
            Name = name,
            TypeLine = typeLine,
            OracleText = oracleText,
            ColorIdentity = [],
            Keywords = [],
            Rarity = "common",
            EdhrecRank = edhrecRank,
        };
    }

    [Fact]
    public void AnalyzeCard_BasicLand_IsLandTrue()
    {
        var card = MakeCard("Forest", "Basic Land — Forest");

        var result = _sut.AnalyzeCard(card, false);

        Assert.True(result.IsLand);
    }

    [Fact]
    public void AnalyzeCard_Island_IsLandTrue()
    {
        var card = MakeCard("Island", "Basic Land — Island");

        var result = _sut.AnalyzeCard(card, false);

        Assert.True(result.IsLand);
    }

    [Fact]
    public void AnalyzeCard_BasicLand_IsRampFalse()
    {
        var card = MakeCard("Forest", "Basic Land — Forest");

        var result = _sut.AnalyzeCard(card, false);

        Assert.False(result.IsRamp);
    }

    [Fact]
    public void AnalyzeCard_SolRing_IsFastManaTrue()
    {
        var card = MakeCard("Sol Ring", "Artifact", "{T}: Add {C}{C}.");

        var result = _sut.AnalyzeCard(card, false);

        Assert.True(result.IsFastMana);
    }

    [Fact]
    public void AnalyzeCard_BirdsOfParadise_IsRampTrue()
    {
        var card = MakeCard("Birds of Paradise", "Creature — Bird", "{T}: Add one mana of any color.");

        var result = _sut.AnalyzeCard(card, false);

        Assert.True(result.IsRamp);
    }

    [Fact]
    public void AnalyzeCard_HighEdhrecRank_LowPlayability()
    {
        var card = MakeCard("Obscure Card", "Creature — Human", "", edhrecRank: 50000);

        var result = _sut.AnalyzeCard(card, false);

        Assert.True(result.Playability <= 10);
    }

    [Fact]
    public void AnalyzeCard_LowEdhrecRank_HighPlayability()
    {
        var card = MakeCard("Sol Ring", "Artifact", "{T}: Add {C}{C}.", edhrecRank: 100);

        var result = _sut.AnalyzeCard(card, false);

        Assert.True(result.Playability >= 70);
    }

    [Fact]
    public void AnalyzeDeck_EmptyDeck_ReturnsDefaults()
    {
        var result = _sut.AnalyzeDeck([]);

        Assert.True(result.PowerLevel >= 0);
        Assert.True(result.Bracket >= 1);
    }

    [Fact]
    public void AnalyzeDeck_DeckWithExtraTurnCard_BracketAtLeast2()
    {
        var cards = new List<CardInfo>
        {
            new() { Name = "Time Warp", IsExtraTurn = true, TypeLine = "Sorcery", EdhrecRank = 1000 },
            new() { Name = "Island", IsLand = true, TypeLine = "Basic Land — Island" },
            new() { Name = "Island", IsLand = true, TypeLine = "Basic Land — Island" },
        };

        var result = _sut.AnalyzeDeck(cards);

        Assert.True(result.Bracket >= 2);
    }

    [Fact]
    public void AnalyzeDeck_DeckWithNoRestrictions_BracketIs1()
    {
        var cards = Enumerable.Range(1, 36)
            .Select(i => new CardInfo
            {
                Name = "Forest",
                IsLand = true,
                TypeLine = "Basic Land — Forest",
                EdhrecRank = 100
            })
            .ToList();

        var result = _sut.AnalyzeDeck(cards);

        Assert.Equal(1, result.Bracket);
    }
}

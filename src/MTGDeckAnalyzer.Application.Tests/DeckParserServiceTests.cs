using MTGDeckAnalyzer.Application.Services;
using Xunit;

namespace MTGDeckAnalyzer.Application.Tests;

public class DeckParserServiceTests
{
    private readonly DeckParserService _sut = new();

    [Fact]
    public void ParseDeckList_EmptyInput_ReturnsEmptyList()
    {
        var (commanders, mainCards, sideboardCards, warnings) = _sut.ParseDeckList("");

        Assert.Empty(mainCards);
    }

    [Fact]
    public void ParseDeckList_SingleCard_ReturnsSingleCard()
    {
        var input = "1 Sol Ring";

        var (commanders, mainCards, sideboardCards, warnings) = _sut.ParseDeckList(input);

        Assert.Single(mainCards);
        Assert.Equal("Sol Ring", mainCards[0].name);
        Assert.Equal(1, mainCards[0].quantity);
    }

    [Fact]
    public void ParseDeckList_WithCommanderHeader_DetectsCommander()
    {
        var input = """
            Commander
            1 Edgar Markov

            Deck
            1 Sol Ring
            """;

        var (commanders, mainCards, sideboardCards, warnings) = _sut.ParseDeckList(input);

        Assert.Contains("Edgar Markov", commanders);
    }

    [Fact]
    public void ParseDeckList_WithMoxfieldFormat_ParsesCorrectly()
    {
        // Moxfield format: deck block, then blank line, then commander at end
        var input = """
            1 Sol Ring
            1 Command Tower

            1 Edgar Markov
            """;

        var (commanders, mainCards, sideboardCards, warnings) = _sut.ParseDeckList(input);

        Assert.Contains("Edgar Markov", commanders);
        Assert.Contains(mainCards, c => c.name == "Sol Ring");
    }

    [Fact]
    public void ParseDeckList_WithSideboardSection_ExcludesSideboardCards()
    {
        var input = "1 Sol Ring\nSideboard\n1 Counterspell";

        var (commanders, mainCards, sideboardCards, warnings) = _sut.ParseDeckList(input);

        Assert.Contains(mainCards, c => c.name == "Sol Ring");
        Assert.Contains(sideboardCards, c => c.name == "Counterspell");
        Assert.DoesNotContain(mainCards, c => c.name == "Counterspell");
    }

    [Fact]
    public void ParseDeckList_WithDualFaceCardName_ParsesFullName()
    {
        var input = "1 Delver of Secrets // Insectile Aberration";

        var (commanders, mainCards, sideboardCards, warnings) = _sut.ParseDeckList(input);

        Assert.Single(mainCards);
        Assert.Equal("Delver of Secrets // Insectile Aberration", mainCards[0].name);
    }

    [Fact]
    public void ParseDeckList_WithQuantityOne_DefaultsToOne()
    {
        var input = "Sol Ring";

        var (commanders, mainCards, sideboardCards, warnings) = _sut.ParseDeckList(input);

        Assert.Single(mainCards);
        Assert.Equal(1, mainCards[0].quantity);
    }

    [Fact]
    public void ParseDeckList_WithInvalidLine_SkipsLine()
    {
        var input = """
            // This is a comment
            1 Sol Ring
            """;

        var (commanders, mainCards, sideboardCards, warnings) = _sut.ParseDeckList(input);

        Assert.Single(mainCards);
        Assert.Equal("Sol Ring", mainCards[0].name);
    }
}

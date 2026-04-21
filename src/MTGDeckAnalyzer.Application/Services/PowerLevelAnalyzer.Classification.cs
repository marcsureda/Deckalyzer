using System.Text.RegularExpressions;
using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public partial class PowerLevelAnalyzer
{
    private static bool ClassifyAsTutor(string oracleText)
    {
        return oracleText.Contains("search your library") &&
               !oracleText.Contains("basic land");
    }

    private static bool ClassifyAsMLD(string oracleText)
    {
        return (oracleText.Contains("destroy all land") ||
                oracleText.Contains("destroy all nonland") == false && oracleText.Contains("destroy all permanent")) &&
               !oracleText.Contains("you control");
    }

    private static bool ClassifyAsCounterspell(string oracleText, string typeLine)
    {
        return oracleText.Contains("counter target spell") ||
               oracleText.Contains("counter target activated") ||
               oracleText.Contains("counter target triggered");
    }

    private static bool ClassifyAsBoardWipe(string oracleText)
    {
        return (oracleText.Contains("destroy all creature") ||
                oracleText.Contains("destroy all nonland permanent") ||
                oracleText.Contains("all creatures get -") ||
                oracleText.Contains("exile all creature") ||
                oracleText.Contains("each creature") && oracleText.Contains("damage")) &&
               !oracleText.Contains("you control");
    }

    private static bool ClassifyAsRemoval(string oracleText, string typeLine)
    {
        if (typeLine.Contains("land")) return false;
        return oracleText.Contains("destroy target") ||
               oracleText.Contains("exile target") ||
               oracleText.Contains("deals") && oracleText.Contains("damage to target") ||
               oracleText.Contains("return target") && oracleText.Contains("to its owner");
    }

    private static bool ClassifyAsCardDraw(string oracleText)
    {
        return oracleText.Contains("draw a card") ||
               oracleText.Contains("draw two") ||
               oracleText.Contains("draw three") ||
               oracleText.Contains("draw cards") ||
               DrawXCardsRegex().IsMatch(oracleText);
    }

    private static bool ClassifyAsRamp(string oracleText, string typeLine, CardInfo card)
    {
        if (card.IsLand) return false;
        return oracleText.Contains("add {") ||
               oracleText.Contains("add one mana") ||
               oracleText.Contains("add two mana") ||
               oracleText.Contains("search your library for a basic land") ||
               (oracleText.Contains("land") && oracleText.Contains("onto the battlefield"));
    }

    /// <summary>
    /// Checks if a card is a game changer using: dynamic Scryfall list, then static fallback.
    /// Handles DFC names by checking both full name and front-face name.
    /// </summary>
    private bool IsGameChanger(string cardName)
    {
        // Check dynamic list (from Scryfall is:gamechanger search)
        if (_dynamicGameChangers.Count > 0)
        {
            if (_dynamicGameChangers.Contains(cardName)) return true;
            // Check front face only for DFCs
            var firstFace = cardName.Split(" // ")[0].Trim();
            if (firstFace != cardName && _dynamicGameChangers.Contains(firstFace)) return true;
        }

        // Check static fallback list
        if (GameChangerCardsFallback.Contains(cardName)) return true;
        // Check front face only for DFCs against fallback
        var frontFace = cardName.Split(" // ")[0].Trim();
        if (frontFace != cardName && GameChangerCardsFallback.Contains(frontFace)) return true;

        return false;
    }

    [GeneratedRegex(@"draw \w+ cards")]
    private static partial Regex DrawXCardsRegex();
}

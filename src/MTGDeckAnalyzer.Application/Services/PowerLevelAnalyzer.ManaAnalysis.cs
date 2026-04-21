using System.Text.RegularExpressions;
using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public partial class PowerLevelAnalyzer
{
    private static void AnalyzeMana(List<CardInfo> cards, DeckAnalysisResult result)
    {
        var symbols = new Dictionary<string, int>
        {
            ["W"] = 0, ["U"] = 0, ["B"] = 0, ["R"] = 0, ["G"] = 0, ["C"] = 0
        };
        var producers = new Dictionary<string, int>
        {
            ["W"] = 0, ["U"] = 0, ["B"] = 0, ["R"] = 0, ["G"] = 0, ["C"] = 0
        };
        var manaCurve = new Dictionary<int, int>();

        foreach (var card in cards)
        {
            // Count mana symbols in cost
            foreach (var color in new[] { "W", "U", "B", "R", "G" })
            {
                var regex = new Regex($"\\{{{color}\\}}");
                symbols[color] += regex.Matches(card.ManaCost).Count;
            }

            // Count mana producers
            if (card.IsLand || card.IsRamp)
            {
                foreach (var color in card.ColorIdentity)
                {
                    if (producers.ContainsKey(color))
                        producers[color]++;
                }
                if (card.ColorIdentity.Count == 0)
                    producers["C"]++;
            }

            // Mana curve (non-lands)
            if (!card.IsLand)
            {
                var cmcBucket = Math.Min((int)card.Cmc, 8);
                manaCurve[cmcBucket] = manaCurve.GetValueOrDefault(cmcBucket) + 1;
            }
        }

        var lands = cards.Count(c => c.IsLand);
        var nonLands = cards.Count - lands;

        // Simplified hypergeometric probability estimates
        var manaScrew = CalculateManaScrew(lands, cards.Count);
        var manaFlood = CalculateManaFlood(lands, cards.Count);

        result.ManaAnalysis = new ManaAnalysis
        {
            ColorSymbols = symbols,
            ColorProducers = producers,
            ManaCurve = manaCurve,
            ManaScrew = Math.Round(manaScrew, 1),
            ManaFlood = Math.Round(manaFlood, 1),
            SweetSpot = Math.Round(100 - manaScrew - manaFlood, 1),
        };
    }

    private static double CalculateManaScrew(int lands, int total)
    {
        // Probability of drawing < 3 lands in first 10 cards
        if (total == 0) return 50;
        double landRatio = (double)lands / total;
        double prob = 0;
        for (int k = 0; k < 3; k++)
        {
            prob += BinomialProbability(10, k, landRatio);
        }
        return prob * 100;
    }

    private static double CalculateManaFlood(int lands, int total)
    {
        if (total == 0) return 50;
        double landRatio = (double)lands / total;
        double prob = 0;
        for (int k = 7; k <= 10; k++)
        {
            prob += BinomialProbability(10, k, landRatio);
        }
        return prob * 100;
    }

    private static double BinomialProbability(int n, int k, double p)
    {
        double coeff = 1;
        for (int i = 0; i < k; i++)
        {
            coeff *= (double)(n - i) / (i + 1);
        }
        return coeff * Math.Pow(p, k) * Math.Pow(1 - p, n - k);
    }
}

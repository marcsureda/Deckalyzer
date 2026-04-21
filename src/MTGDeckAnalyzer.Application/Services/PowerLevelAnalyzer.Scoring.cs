using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public partial class PowerLevelAnalyzer
{
    /// <summary>
    /// Playability based on EDHREC rank: lower rank = more played = higher playability.
    /// Calibrated against edhpowerlevel.com: Edgar Markov deck avg = 52.2%.
    /// Rank 1 → 99%, Rank 100 → 72%, Rank 1000 → 58.5%, Rank 3000 → 52%,
    /// Rank 5000 → 49%, Rank 10000 → 45%, Rank 20000 → 41%
    /// </summary>
    private static double CalculatePlayability(CardInfo card)
    {
        if (card.EdhrecRank <= 0) return 45.0;
        if (card.EdhrecRank >= 30000) return 5.0;

        // Logarithmic decay: 95 - 12 * log10(rank)
        // Fitted so avg rank ~3000 → 53.3%, matching edhpowerlevel.com's 52.2% for Edgar Markov
        double playability = 95 - 12 * Math.Log10(Math.Max(1, card.EdhrecRank));
        return Math.Round(Math.Clamp(playability, 5.0, 99.0), 1);
    }

    /// <summary>
    /// Impact is primarily driven by EDHREC rank (community demand data).
    /// Calibrated against edhpowerlevel.com: Edgar Markov deck total = 842.
    /// Rank 1 → 21, Rank 100 → 12.6, Rank 1000 → 8.4, Rank 5000 → 5.5, Rank 10000 → 4.2
    /// </summary>
    private static double CalculateImpact(CardInfo card, bool isReservedList)
    {
        var typeLine = card.TypeLine.ToLowerInvariant();

        // Basic lands: fixed low impact (per edhpowerlevel: "basic lands now have a low base level of impact")
        if (card.IsLand && typeLine.Contains("basic"))
            return 1.0;

        // EDHREC rank → impact using log10 curve
        // 19.4 - 4.25 * log10(rank), capped at 18
        // rank 1→18(cap), rank 10→15.15, rank 50→12.18, rank 100→10.9,
        // rank 500→7.93, rank 1000→6.65, rank 3000→4.62, rank 10000→2.4
        double baseImpact;
        if (card.EdhrecRank <= 0 || card.EdhrecRank >= 30000)
            baseImpact = 0.5;
        else
            baseImpact = Math.Min(18.0, Math.Max(0.5, 19.4 - 4.25 * Math.Log10(card.EdhrecRank)));

        // Reserved list nerf: reduce by 30% (per edhpowerlevel changelog Sep 2024)
        if (isReservedList)
            baseImpact *= 0.7;

        // Non-basic lands: reduction (they contribute less individually than spells)
        if (card.IsLand)
            baseImpact *= 0.75;

        return Math.Round(baseImpact, 2);
    }

    private static double CalculatePowerScore(CardInfo card)
    {
        return Math.Round((card.Playability * 0.3 + card.Impact * 5) / 10.0, 2);
    }

    /// <summary>
    /// Calculates all deck-level power metrics calibrated against edhpowerlevel.com:
    /// - Total Impact: sum of all card impacts (EDHREC rank-driven)
    /// - AverageCmc: average CMC of non-land cards
    /// - Efficiency: 0-10 based on impact-weighted CMC (exponential decay)
    /// - Tipping Point: CMC where most impact concentrates
    /// - Average Playability: average playability across all cards
    /// - Power Level: sigmoid of average impact with small eff/play adjustments
    ///   Calibrated: precon(avgI~3.5)→3.2, tuned(avgI~6)→6.8, optimized(avgI~7.5)→8.3, cEDH(avgI~10+)→9.5
    /// - Score: 0-1000 composite
    /// </summary>
    private static void CalculatePowerMetrics(List<CardInfo> cards, DeckAnalysisResult result)
    {
        var nonLands = cards.Where(c => !c.IsLand).ToList();
        var lands = cards.Where(c => c.IsLand).ToList();

        // === Basic stats ===
        result.TotalImpact = Math.Round(cards.Sum(c => c.Impact), 2);

        result.AverageCmc = nonLands.Count > 0
            ? Math.Round(nonLands.Average(c => c.Cmc), 2)
            : 0;

        result.AveragePlayability = cards.Count > 0
            ? Math.Round(cards.Average(c => c.Playability), 1)
            : 0;

        // === Tipping Point: CMC where most impact concentrates ===
        if (nonLands.Count > 0)
        {
            var cmcGroups = nonLands.GroupBy(c => (int)c.Cmc)
                .Select(g => new { Cmc = g.Key, TotalImpact = g.Sum(c => c.Impact) })
                .OrderByDescending(g => g.TotalImpact)
                .ToList();
            result.TippingPoint = cmcGroups.FirstOrDefault()?.Cmc ?? 3;
        }

        // === Efficiency (0-10): impact-weighted CMC with exponential decay ===
        // Calibrated: decay=0.18 so Edgar Markov (wCMC ~3.0) → 6.98
        // CMC 1 → 10, CMC 2 → 8.35, CMC 3 → 6.98, CMC 4 → 5.83, CMC 5 → 4.87
        if (nonLands.Count > 0 && nonLands.Sum(c => c.Impact) > 0)
        {
            double totalNonLandImpact = nonLands.Sum(c => c.Impact);
            double impactWeightedCmc = nonLands.Sum(c => c.Impact * c.Cmc) / totalNonLandImpact;
            result.Efficiency = Math.Round(
                Math.Clamp(10.0 * Math.Exp(-0.18 * Math.Max(0, impactWeightedCmc - 1)), 0, 10), 2);
        }
        else
        {
            result.Efficiency = Math.Round(
                Math.Clamp(10.0 * Math.Exp(-0.18 * Math.Max(0, result.AverageCmc - 1)), 0, 10), 2);
        }

        // === Power Level (1-10): sigmoid of effective impact ===
        // avgImpact alone can't differentiate decks with similar card quality but different
        // structural power (fast mana, tutors, combos, game changers). We add a structural
        // bonus to the effective impact before feeding into the sigmoid.

        double avgImpactPerCard = cards.Count > 0 ? result.TotalImpact / cards.Count : 0;

        // Structural quality bonus: fast mana, tutors, game changers, combos
        // These differentiate a "power 8.4" deck from a "7.7" deck with similar avg card quality
        int fastManaCount = cards.Count(c => c.IsFastMana);
        int tutorCount = cards.Count(c => c.IsTutor);
        int gameChangerCount = cards.Count(c => c.IsGameChanger);
        int comboCount = result.Combos?.Count ?? 0;
        int counterspellCount = cards.Count(c => c.IsCounterspell);

        double structureBonus =
            Math.Min(fastManaCount, 6) * 0.12 +       // up to +0.72: explosive starts
            Math.Min(tutorCount, 6) * 0.10 +           // up to +0.60: consistency
            Math.Min(gameChangerCount, 8) * 0.06 +     // up to +0.48: format-warping cards
            Math.Min(comboCount, 3) * 0.15 +            // up to +0.45: win conditions
            Math.Min(counterspellCount, 4) * 0.05;      // up to +0.20: interaction density

        double effectiveImpact = avgImpactPerCard + structureBonus;

        // Sigmoid (midpoint=5.2, steepness=2.3)
        double basePower = 10.0 / (1.0 + Math.Exp(-(effectiveImpact - 5.2) / 2.3));

        // Minor adjustments for efficiency and playability
        double effAdj = (result.Efficiency - 5.0) * 0.06;
        double playAdj = (result.AveragePlayability / 100.0 - 0.5) * 0.15;

        // Apply 3.6% reduction to align with edhpowerlevel.com calibration
        result.PowerLevel = Math.Round(Math.Clamp((basePower + effAdj + playAdj) * 0.964, 1.0, 10.0), 2);

        // === Diagnostic logging ===
        Console.WriteLine($"[DIAG] Cards: {cards.Count}, TotalImpact: {result.TotalImpact}, AvgImpact: {avgImpactPerCard:F2}");
        Console.WriteLine($"[DIAG] StructureBonus: {structureBonus:F2} (FM:{fastManaCount} Tu:{tutorCount} GC:{gameChangerCount} Co:{comboCount} CS:{counterspellCount})");
        Console.WriteLine($"[DIAG] EffectiveImpact: {effectiveImpact:F2}, BasePower(sigmoid): {basePower:F2}, EffAdj: {effAdj:F2}, PlayAdj: {playAdj:F2}");
        Console.WriteLine($"[DIAG] Power: {result.PowerLevel}, Efficiency: {result.Efficiency}, Playability: {result.AveragePlayability}%");
        var topCards = cards.OrderByDescending(c => c.Impact).Take(10);
        Console.WriteLine($"[DIAG] Top 10 impact: {string.Join(", ", topCards.Select(c => $"{c.Name}={c.Impact}(r{c.EdhrecRank})"))}");
        var bottomCards = cards.Where(c => !c.IsLand || !c.TypeLine.ToLowerInvariant().Contains("basic")).OrderBy(c => c.Impact).Take(5);
        Console.WriteLine($"[DIAG] Bottom 5 impact: {string.Join(", ", bottomCards.Select(c => $"{c.Name}={c.Impact}(r{c.EdhrecRank})"))}");

        // === Score (0-1000): normalized composite ===
        double efficiencyFactor = result.Efficiency / 10.0;
        double playabilityFactor = result.AveragePlayability / 100.0;
        double qualityFactor = (efficiencyFactor + playabilityFactor) / 2.0;
        double rawScore = result.PowerLevel * 80.0 * (0.8 + 0.4 * qualityFactor);
        result.Score = Math.Round(Math.Clamp(rawScore, 0, 1000), 0);
    }

    /// <summary>Logistic sigmoid: 0.5 at midPoint, steepness controls slope.</summary>
    private static double Sigmoid(double x, double midPoint, double steepness)
    {
        return 1.0 / (1.0 + Math.Exp(-(x - midPoint) / steepness));
    }

    /// <summary>
    /// Adjusts the bracket AFTER power level is calculated.
    /// A bracket 1 deck cannot have power ~7 — the bracket floor is raised based on power.
    /// Conversely, a low-power deck with game changers already gets pushed up by rules alone.
    /// </summary>
    private static void AdjustBracketForPower(DeckAnalysisResult result)
    {
        // Power-based minimum bracket floors:
        // Power < 4   → bracket can be 1
        // Power 4-5.5 → bracket at least 2
        // Power 5.5-7 → bracket at least 3 
        // Power 7+    → bracket at least 4
        // Power 9+    → bracket 5 (cEDH)
        int minBracket;
        if (result.PowerLevel >= 9.0) minBracket = 5;
        else if (result.PowerLevel >= 7.0) minBracket = 4;
        else if (result.PowerLevel >= 5.5) minBracket = 3;
        else if (result.PowerLevel >= 4.0) minBracket = 2;
        else minBracket = 1;

        if (result.Bracket < minBracket)
        {
            result.Bracket = minBracket;
            result.BracketName = result.BracketDetails.Requirements
                .FirstOrDefault(r => r.Bracket == minBracket)?.Name ?? result.BracketName;
        }
    }
}

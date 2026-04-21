using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public partial class PowerLevelAnalyzer
{
    private void AnalyzeBracket(List<CardInfo> cards, DeckAnalysisResult result)
    {
        var details = new BracketDetails();

        // Detect extra turns
        var extraTurnCards = cards.Where(c => c.IsExtraTurn).ToList();
        details.HasExtraTurns = extraTurnCards.Count > 0;
        details.HasChainingExtraTurns = extraTurnCards.Count >= 2;
        details.ExtraTurnCards = extraTurnCards.Select(c => c.Name).ToList();

        // Detect mass land denial
        var mldCards = cards.Where(c => c.IsMassLandDenial).ToList();
        details.HasMassLandDenial = mldCards.Count > 0;
        details.MassLandDenialCards = mldCards.Select(c => c.Name).ToList();

        // Detect 2-card combos and build ComboInfo list
        var cardNames = cards.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var comboCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var detectedCombos = new List<ComboInfo>();
        var seenComboPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in cards)
        {
            if (ComboPairs.TryGetValue(card.Name, out var partners))
            {
                foreach (var partner in partners)
                {
                    if (cardNames.Contains(partner))
                    {
                        comboCards.Add(card.Name);
                        comboCards.Add(partner);

                        // Create unique combo key (sorted) to avoid duplicates
                        var comboPair = string.Compare(card.Name, partner, StringComparison.OrdinalIgnoreCase) < 0
                            ? $"{card.Name}+{partner}" : $"{partner}+{card.Name}";
                        if (seenComboPairs.Add(comboPair))
                        {
                            ComboDescriptions.TryGetValue(comboPair, out var desc);
                            ComboEffects.TryGetValue(comboPair, out var effect);
                            // Build Commander Spellbook search URL
                            var searchQuery = Uri.EscapeDataString($"{card.Name} {partner}");
                            detectedCombos.Add(new ComboInfo
                            {
                                Cards = [card.Name, partner],
                                Description = desc ?? $"Infinite combo: {card.Name} + {partner}",
                                Url = $"https://commanderspellbook.com/search/?q={searchQuery}",
                                IsInfinite = true,
                                InfiniteEffect = effect ?? "Infinite combo",
                            });
                        }
                    }
                }
            }
        }
        details.HasTwoCardCombos = comboCards.Count > 0;
        details.ComboCards = [.. comboCards];
        result.Combos = detectedCombos;

        // Detect 3-card combos
        foreach (var combo in ThreeCardCombos)
        {
            // For entries with "Any opponent" as a slot, treat as 2-real-card combo
            var realCards = combo.Cards.Where(c => !c.StartsWith("Any", StringComparison.OrdinalIgnoreCase)).ToList();
            if (realCards.All(c => cardNames.Contains(c)))
            {
                // Build sorted key to avoid duplicates
                var comboKey = string.Join("+", realCards.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
                if (seenComboPairs.Add(comboKey))
                {
                    foreach (var c in realCards)
                        comboCards.Add(c);

                    var searchQuery = Uri.EscapeDataString(string.Join(" ", realCards));
                    detectedCombos.Add(new ComboInfo
                    {
                        Cards = [.. realCards],
                        Description = combo.Description,
                        Url = $"https://commanderspellbook.com/search/?q={searchQuery}",
                        IsInfinite = true,
                        InfiniteEffect = combo.Effect,
                    });
                }
            }
        }

        // Update combo tracking after 3-card detection
        if (!details.HasTwoCardCombos && comboCards.Count > 0)
            details.HasTwoCardCombos = true; // 3-card combos still count for bracket calculations
        details.ComboCards = [.. comboCards];
        result.Combos = detectedCombos;

        // Check if combos are "late game" (both pieces CMC >= 5)
        if (details.HasTwoCardCombos)
        {
            var comboPieceCards = cards.Where(c => comboCards.Contains(c.Name)).ToList();
            details.HasOnlyLateGameCombos = comboPieceCards.All(c => c.Cmc >= 5);
        }

        // Count game changers
        var gameChangers = cards.Where(c => c.IsGameChanger).ToList();
        details.GameChangerCount = gameChangers.Count;
        details.GameChangerCards = gameChangers.Select(c => c.Name).ToList();

        // Determine bracket per WotC rules
        var brackets = new List<BracketRequirement>
        {
            new()
            {
                Bracket = 1,
                Name = "Exhibition",
                Rules =
                [
                    new() { Description = "No Extra Turns", Passes = !details.HasExtraTurns },
                    new() { Description = "No Mass Land Denial", Passes = !details.HasMassLandDenial },
                    new() { Description = "No 2-Card Combos", Passes = !details.HasTwoCardCombos },
                    new() { Description = "No Game Changers", Passes = details.GameChangerCount == 0 },
                ],
            },
            new()
            {
                Bracket = 2,
                Name = "Core",
                Rules =
                [
                    new() { Description = "No Chaining Extra Turns", Passes = !details.HasChainingExtraTurns },
                    new() { Description = "No Mass Land Denial", Passes = !details.HasMassLandDenial },
                    new() { Description = "No 2-Card Combos", Passes = !details.HasTwoCardCombos },
                    new() { Description = "No Game Changers", Passes = details.GameChangerCount == 0 },
                ],
            },
            new()
            {
                Bracket = 3,
                Name = "Upgraded",
                Rules =
                [
                    new() { Description = "No Chaining Extra Turns", Passes = !details.HasChainingExtraTurns },
                    new() { Description = "No Mass Land Denial", Passes = !details.HasMassLandDenial },
                    new() { Description = "Only Late Game 2-Card Combos", Passes = !details.HasTwoCardCombos || details.HasOnlyLateGameCombos },
                    new() { Description = "Max 3 Game Changers", Passes = details.GameChangerCount <= 3 },
                ],
            },
            new()
            {
                Bracket = 4,
                Name = "Optimized",
                Rules =
                [
                    new() { Description = "No Restrictions", Passes = true },
                ],
            },
            new()
            {
                Bracket = 5,
                Name = "cEDH",
                Rules =
                [
                    new() { Description = "No Restrictions", Passes = true },
                ],
            },
        };

        foreach (var b in brackets)
        {
            b.Passes = b.Rules.All(r => r.Passes);
        }

        details.Requirements = brackets;

        // Bracket is the lowest passing bracket
        var lowestPassing = brackets.FirstOrDefault(b => b.Passes);
        result.Bracket = lowestPassing?.Bracket ?? 4;
        result.BracketName = lowestPassing?.Name ?? "Optimized";
        result.BracketDetails = details;
    }
}

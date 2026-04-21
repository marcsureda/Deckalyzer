using System.Text.RegularExpressions;
using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public partial class PowerLevelAnalyzer
{
    private static void CalculateSynergies(List<CardInfo> cards)
    {
        var commanderCards = cards.Where(c => c.IsCommander).ToList();
        if (commanderCards.Count == 0)
        {
            // No commander detected — set all synergy to 50 (neutral)
            foreach (var card in cards)
                card.Synergy = 50.0;
            return;
        }

        // Merge all commanders' oracle text, types, keywords, colors, and themes
        var cmdOracle = string.Join(" ", commanderCards.Select(c => c.OracleText)).ToLowerInvariant();
        var cmdType = string.Join(" ", commanderCards.Select(c => c.TypeLine)).ToLowerInvariant();
        var cmdKeywords = new HashSet<string>(commanderCards.SelectMany(c => c.Keywords), StringComparer.OrdinalIgnoreCase);
        var cmdColors = new HashSet<string>(commanderCards.SelectMany(c => c.ColorIdentity));

        // Extract tribal types from commander
        var tribalTypes = ExtractCreatureTypes(cmdType, cmdOracle);

        // Extract commander themes from oracle text
        var cmdThemes = ExtractThemes(cmdOracle);

        foreach (var card in cards)
        {
            if (card.IsCommander)
            {
                card.Synergy = 100.0; // Commander is 100% synergistic with itself
                continue;
            }

            double synergy = 0;
            var oracle = card.OracleText.ToLowerInvariant();
            var type = card.TypeLine.ToLowerInvariant();

            // 1. Keyword overlap (e.g., commander has "flying" and card has "flying")
            if (cmdKeywords.Count > 0 && card.Keywords.Count > 0)
            {
                var sharedKeywords = card.Keywords.Count(k => cmdKeywords.Contains(k));
                synergy += Math.Min(20, sharedKeywords * 10);
            }

            // 2. Tribal synergy
            if (tribalTypes.Count > 0)
            {
                foreach (var tribe in tribalTypes)
                {
                    if (type.Contains(tribe) || oracle.Contains(tribe))
                    {
                        synergy += 25;
                        break;
                    }
                }
            }

            // 3. Theme overlap (tokens, counters, life, sacrifice, graveyard, etc.)
            if (cmdThemes.Count > 0)
            {
                var cardThemes = ExtractThemes(oracle);
                var shared = cmdThemes.Intersect(cardThemes).Count();
                synergy += Math.Min(25, shared * 8);
            }

            // 4. Color affinity — perfect color match is good
            if (!card.IsLand && card.Colors.Count > 0)
            {
                var matchingColors = card.Colors.Count(c => cmdColors.Contains(c));
                synergy += matchingColors * 3;
            }

            // 5. Functional role relevance: lands, ramp, draw are generically useful (baseline synergy)
            if (card.IsLand) synergy += 15;
            else if (card.IsRamp) synergy += 10;
            else if (card.IsCardDraw) synergy += 10;
            else if (card.IsRemoval) synergy += 8;
            else if (card.IsBoardWipe) synergy += 5;

            // 6. Direct mention: card mentions something the commander cares about
            if (cmdOracle.Contains("token") && (oracle.Contains("create") && oracle.Contains("token"))) synergy += 15;
            if (cmdOracle.Contains("counter") && oracle.Contains("+1/+1 counter")) synergy += 15;
            if (cmdOracle.Contains("sacrifice") && (oracle.Contains("sacrifice") || oracle.Contains("when") && oracle.Contains("dies"))) synergy += 12;
            if (cmdOracle.Contains("graveyard") && (oracle.Contains("graveyard") || oracle.Contains("mill"))) synergy += 12;
            if (cmdOracle.Contains("life") && (oracle.Contains("gain") && oracle.Contains("life") || oracle.Contains("lose life"))) synergy += 12;
            if (cmdOracle.Contains("attack") && oracle.Contains("attack")) synergy += 8;
            if (cmdOracle.Contains("enters") && oracle.Contains("enters")) synergy += 8;
            if (cmdOracle.Contains("cast") && oracle.Contains("whenever") && oracle.Contains("cast")) synergy += 10;

            card.Synergy = Math.Round(Math.Clamp(synergy, 0, 100), 1);
        }
    }

    /// <summary>
    /// Analyzes the deck's strategy/archetype by examining card text patterns, types, and keywords.
    /// </summary>
    private static void AnalyzeStrategy(List<CardInfo> cards, DeckAnalysisResult result)
    {
        var nonLands = cards.Where(c => !c.IsLand).ToList();
        int total = nonLands.Count;
        if (total == 0)
        {
            result.Strategy = new DeckStrategy { PrimaryArchetype = "Unknown", Summary = "Not enough cards to determine strategy." };
            return;
        }

        var tags = new List<StrategyTag>();

        // Token strategy
        var tokenCards = nonLands.Where(c => HasPattern(c, "create", "token") || HasPattern(c, "put", "token") || c.Keywords.Any(k => k.Equals("Fabricate", StringComparison.OrdinalIgnoreCase) || k.Equals("Amass", StringComparison.OrdinalIgnoreCase))).ToList();
        if (tokenCards.Count >= 3)
            tags.Add(MakeTag("Tokens", tokenCards, total));

        // Tribal / creature-type synergy
        var tribalCards = nonLands.Where(c => HasPattern(c, "all", "you control get") || HasPattern(c, "other", "you control get") || HasPattern(c, "lord") || (c.OracleText.Contains("of that type", StringComparison.OrdinalIgnoreCase))).ToList();
        var creatureTypeGroups = cards.Where(c => c.IsCreature).SelectMany(c => ExtractCreatureTypes(c.TypeLine)).GroupBy(t => t, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).ToList();
        var dominantType = creatureTypeGroups.FirstOrDefault();
        if (dominantType != null && dominantType.Count() >= 8)
        {
            var tribalExamples = cards.Where(c => c.TypeLine.Contains(dominantType.Key, StringComparison.OrdinalIgnoreCase)).Take(5).Select(c => c.Name).ToList();
            tags.Add(new StrategyTag { Name = $"Tribal ({dominantType.Key})", CardCount = dominantType.Count(), Percentage = Math.Round(100.0 * dominantType.Count() / total, 1), ExampleCards = tribalExamples });
        }
        else if (tribalCards.Count >= 3)
            tags.Add(MakeTag("Tribal Synergy", tribalCards, total));

        // Voltron (commander-focused combat)
        var voltronCards = nonLands.Where(c => c.TypeLine.Contains("Equipment", StringComparison.OrdinalIgnoreCase) || c.TypeLine.Contains("Aura", StringComparison.OrdinalIgnoreCase) || HasPattern(c, "equipped creature") || HasPattern(c, "enchanted creature")).ToList();
        if (voltronCards.Count >= 6)
            tags.Add(MakeTag("Voltron", voltronCards, total));

        // Aristocrats / sacrifice
        var sacrificeCards = nonLands.Where(c => HasPattern(c, "sacrifice") && (HasPattern(c, "whenever") || HasPattern(c, "you may sacrifice"))).ToList();
        if (sacrificeCards.Count >= 5)
            tags.Add(MakeTag("Aristocrats / Sacrifice", sacrificeCards, total));

        // Reanimator
        var reanimateCards = nonLands.Where(c => HasPattern(c, "return", "from", "graveyard", "to the battlefield") || HasPattern(c, "put", "from", "graveyard", "onto the battlefield")).ToList();
        if (reanimateCards.Count >= 3)
            tags.Add(MakeTag("Reanimator", reanimateCards, total));

        // Spellslinger (instants/sorceries matter)
        int spellCount = cards.Count(c => c.IsInstant || c.IsSorcery);
        var spellSynergyCards = nonLands.Where(c => HasPattern(c, "whenever you cast", "instant or sorcery") || HasPattern(c, "magecraft") || c.Keywords.Any(k => k.Equals("Magecraft", StringComparison.OrdinalIgnoreCase) || k.Equals("Storm", StringComparison.OrdinalIgnoreCase))).ToList();
        if (spellCount >= 25 || spellSynergyCards.Count >= 4)
            tags.Add(MakeTag("Spellslinger", spellSynergyCards.Count >= 4 ? spellSynergyCards : nonLands.Where(c => c.IsInstant || c.IsSorcery).Take(5).ToList(), total));

        // +1/+1 Counters
        var counterCards = nonLands.Where(c => HasPattern(c, "+1/+1 counter") || c.Keywords.Any(k => k.Equals("Proliferate", StringComparison.OrdinalIgnoreCase) || k.Equals("Modular", StringComparison.OrdinalIgnoreCase) || k.Equals("Adapt", StringComparison.OrdinalIgnoreCase))).ToList();
        if (counterCards.Count >= 5)
            tags.Add(MakeTag("+1/+1 Counters", counterCards, total));

        // Stax / Control
        var staxCards = nonLands.Where(c => HasPattern(c, "opponents can't") || HasPattern(c, "each opponent") && HasPattern(c, "pay") || HasPattern(c, "nonland permanent", "doesn't untap") || HasPattern(c, "tax")).ToList();
        if (staxCards.Count >= 3)
            tags.Add(MakeTag("Stax / Control", staxCards, total));

        // Mill
        var millCards = nonLands.Where(c => HasPattern(c, "mill") || HasPattern(c, "put the top", "into", "graveyard")).ToList();
        if (millCards.Count >= 4)
            tags.Add(MakeTag("Mill", millCards, total));

        // Lifegain
        var lifegainCards = nonLands.Where(c => HasPattern(c, "gain", "life") || HasPattern(c, "lifelink") || c.Keywords.Any(k => k.Equals("Lifelink", StringComparison.OrdinalIgnoreCase))).ToList();
        if (lifegainCards.Count >= 5)
            tags.Add(MakeTag("Lifegain", lifegainCards, total));

        // Graveyard value
        var graveyardCards = nonLands.Where(c => HasPattern(c, "from your graveyard") || HasPattern(c, "flashback") || HasPattern(c, "escape") || HasPattern(c, "unearth") || c.Keywords.Any(k => k.Equals("Flashback", StringComparison.OrdinalIgnoreCase) || k.Equals("Escape", StringComparison.OrdinalIgnoreCase) || k.Equals("Unearth", StringComparison.OrdinalIgnoreCase) || k.Equals("Dredge", StringComparison.OrdinalIgnoreCase))).ToList();
        if (graveyardCards.Count >= 4)
            tags.Add(MakeTag("Graveyard Value", graveyardCards, total));

        // Combo-focused
        if (result.Combos.Count >= 1)
        {
            int tutorCount = cards.Count(c => c.IsTutor);
            if (tutorCount >= 3 || result.Combos.Count >= 2)
                tags.Add(new StrategyTag { Name = "Combo", CardCount = result.Combos.Sum(c => c.Cards.Count), Percentage = Math.Round(100.0 * result.Combos.Sum(c => c.Cards.Count) / total, 1), ExampleCards = result.Combos.SelectMany(c => c.Cards).Distinct().Take(5).ToList() });
        }

        // Go-wide aggro
        int creatureCount = cards.Count(c => c.IsCreature && !c.IsLand);
        var aggroCards = nonLands.Where(c => c.IsCreature && c.Cmc <= 3).ToList();
        if (creatureCount >= 30 && aggroCards.Count >= 15)
            tags.Add(MakeTag("Aggro / Go-Wide", aggroCards.Take(10).ToList(), total));

        // Sort by card count descending
        tags = tags.OrderByDescending(t => t.CardCount).ToList();

        // Determine primary archetype
        string primary = tags.Count > 0 ? tags[0].Name : (creatureCount >= 20 ? "Creature Beatdown" : "Midrange / Goodstuff");

        // Build summary
        var tagNames = tags.Select(t => t.Name).ToList();
        string summary = tags.Count switch
        {
            0 => $"This deck appears to be a {primary} strategy focused on general value and creature-based attacks.",
            1 => $"This deck is primarily a {primary} strategy.",
            _ => $"This deck is primarily {tagNames[0]}, with elements of {string.Join(", ", tagNames.Skip(1))}."
        };

        result.Strategy = new DeckStrategy
        {
            PrimaryArchetype = primary,
            Tags = tags,
            Summary = summary,
        };
    }

    /// <summary>
    /// Extracts token types produced by cards in the deck.
    /// Parses oracle text for "create" + token patterns.
    /// </summary>
    private static void AnalyzeTokens(List<CardInfo> cards, DeckAnalysisResult result)
    {
        // Map token description → list of cards that produce it
        var tokenMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var card in cards)
        {
            var text = card.OracleText;
            if (string.IsNullOrEmpty(text)) continue;

            // Match patterns like "create a 1/1 white Vampire creature token"
            // or "create X 2/2 black Zombie creature tokens"
            var matches = TokenRegex().Matches(text);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var stats = match.Groups[1].Value.Trim();      // "1/1" or "2/2" etc.
                var descriptor = match.Groups[2].Value.Trim();  // "white Vampire creature" etc.

                // Clean up the descriptor
                var tokenDesc = $"{stats} {descriptor}".Trim();
                // Normalize: remove "tapped", "that's", extra spaces
                tokenDesc = tokenDesc.Replace("  ", " ").Trim();
                // Capitalize first letter
                if (tokenDesc.Length > 0)
                    tokenDesc = char.ToUpper(tokenDesc[0]) + tokenDesc[1..];

                if (!tokenMap.ContainsKey(tokenDesc))
                    tokenMap[tokenDesc] = [];
                if (!tokenMap[tokenDesc].Contains(card.Name))
                    tokenMap[tokenDesc].Add(card.Name);
            }

            // Also check for treasure/clue/food/blood tokens
            var specialTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["treasure token"] = "Treasure",
                ["clue token"] = "Clue",
                ["food token"] = "Food",
                ["blood token"] = "Blood",
                ["map token"] = "Map",
                ["powerstone token"] = "Powerstone",
                ["incubator token"] = "Incubator",
            };

            foreach (var (pattern, tokenName) in specialTokens)
            {
                if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    if (!tokenMap.ContainsKey(tokenName))
                        tokenMap[tokenName] = [];
                    if (!tokenMap[tokenName].Contains(card.Name))
                        tokenMap[tokenName].Add(card.Name);
                }
            }
        }

        // Also check commander-specific token creation (e.g., Edgar Markov creates 1/1 Vampires)
        // These are already caught by the regex above

        result.Tokens = tokenMap
            .OrderByDescending(kv => kv.Value.Count)
            .Select(kv => new TokenInfo
            {
                Description = kv.Key,
                ProducedBy = kv.Value,
            })
            .ToList();
    }

    private static void GenerateRecommendations(List<CardInfo> cards, DeckAnalysisResult result)
    {
        // Build existing card set ONCE, handling DFC names
        var existingCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in cards)
        {
            existingCards.Add(card.Name);
            // Also add front face of DFC cards for robust matching
            var slashIdx = card.Name.IndexOf(" // ", StringComparison.Ordinal);
            if (slashIdx > 0)
                existingCards.Add(card.Name[..slashIdx]);
        }

        // === CUT recommendations: lowest impact non-land cards ===
        var avgImpact = cards.Count > 0 ? cards.Average(c => c.Impact) : 5;
        var cutCandidates = cards
            .Where(c => !c.IsCommander && !c.IsLand)
            .OrderBy(c => c.Impact)
            .ThenBy(c => c.Playability)
            .Take(10)
            .ToList();

        foreach (var card in cutCandidates)
        {
            var reason = card.Playability < 15
                ? "Very low popularity in Commander — likely better options exist"
                : card.Impact < 2
                    ? "Minimal impact on the game"
                    : card.Impact < avgImpact * 0.5
                        ? $"Impact ({card.Impact:F1}) is well below your deck average ({avgImpact:F1})"
                        : card.Cmc > 5 && card.Impact < 5
                            ? $"High mana cost ({card.Cmc}) with limited impact ({card.Impact:F1})"
                            : $"Below average for this slot (impact {card.Impact:F1} vs avg {avgImpact:F1})";

            result.CutRecommendations.Add(new CardRecommendation
            {
                CardName = card.Name,
                Reason = reason,
                Category = GetCardCategory(card),
                EstimatedImpact = card.Impact,
                ImageUri = card.ImageUri,
            });
        }

        // === ADD recommendations: multiple categories, always provide variety ===
        var composition = result.Composition;
        var colors = result.ColorIdentity;
        int maxPerCategory = 3;

        // Category-based suggestions (only when the deck is weak in that area)
        if (composition.Ramp < 8)
            AddCategoryRecommendations(result, existingCards, colors, "Ramp", maxPerCategory, GetRampSuggestions());
        if (composition.CardDraw < 8)
            AddCategoryRecommendations(result, existingCards, colors, "Card Draw", maxPerCategory, GetDrawSuggestions());
        if (composition.Removal < 8)
            AddCategoryRecommendations(result, existingCards, colors, "Removal", maxPerCategory, GetRemovalSuggestions());
        if (composition.BoardWipes < 3)
            AddCategoryRecommendations(result, existingCards, colors, "Board Wipe", 2, GetBoardWipeSuggestions());

        // Always suggest: lands, fast mana, tutors, protection — based on what's missing
        AddLandRecommendations(result, existingCards, colors, composition);
        AddFastManaSuggestions(result, existingCards, cards);
        AddTutorSuggestions(result, existingCards, colors, composition);
        AddProtectionSuggestions(result, existingCards, colors, cards);

        // Always suggest: format staples the deck is missing
        AddStapleSuggestions(result, existingCards, colors);
    }

    private static void AnalyzeStrengthsWeaknesses(DeckAnalysisResult result)
    {
        var comp = result.Composition;
        var mana = result.ManaAnalysis;

        // Strengths
        if (comp.Ramp >= 10) result.Strengths.Add("Excellent ramp package for consistent acceleration");
        else if (comp.Ramp >= 8) result.Strengths.Add("Good ramp package");

        if (comp.CardDraw >= 10) result.Strengths.Add("Strong card draw engine");
        else if (comp.CardDraw >= 8) result.Strengths.Add("Solid card advantage");

        if (comp.Removal >= 10) result.Strengths.Add("Well-stocked removal suite");
        if (comp.BoardWipes >= 3) result.Strengths.Add("Adequate board wipe coverage");
        if (comp.Tutors >= 3) result.Strengths.Add("Good tutor package for consistency");
        if (comp.Counterspells >= 4) result.Strengths.Add("Strong countermagic package");

        if (result.AverageCmc <= 2.5) result.Strengths.Add("Very efficient mana curve");
        else if (result.AverageCmc <= 3.2) result.Strengths.Add("Good mana curve efficiency");

        if (result.BracketDetails.HasTwoCardCombos)
            result.Strengths.Add("Has win condition combo(s)");

        var fastManaCount = result.Cards.Count(c => c.IsFastMana);
        if (fastManaCount >= 3) result.Strengths.Add("Fast mana enables explosive starts");

        // Mana base quality — only add as strength if NOT also a weakness
        bool manaIsGood = mana.SweetSpot >= 40 && mana.ManaScrew <= 25;
        if (manaIsGood) result.Strengths.Add("Balanced mana base with low screw/flood risk");

        // Weaknesses — with remediation advice
        if (comp.Ramp < 6) result.Weaknesses.Add("Critically low ramp (only " + comp.Ramp + ") — add Sol Ring, Arcane Signet, Cultivate, or similar mana acceleration to 8-10 sources");
        else if (comp.Ramp < 8) result.Weaknesses.Add("Below average ramp (" + comp.Ramp + "/8 recommended) — consider adding 2-mana rocks like Fellwar Stone or Mind Stone");

        if (comp.CardDraw < 5) result.Weaknesses.Add("Critically low card draw (only " + comp.CardDraw + ") — add Rhystic Study, Night's Whisper, or Skullclamp to avoid running out of gas");
        else if (comp.CardDraw < 8) result.Weaknesses.Add("Below average card draw (" + comp.CardDraw + "/8 recommended) — consider adding Phyrexian Arena, Beast Whisperer, or similar");

        if (comp.Removal < 5) result.Weaknesses.Add("Very few removal spells (only " + comp.Removal + ") — add Swords to Plowshares, Beast Within, and Chaos Warp to handle threats");
        else if (comp.Removal < 8) result.Weaknesses.Add("Limited removal (" + comp.Removal + "/8 recommended) — consider more targeted removal");

        if (comp.BoardWipes < 2) result.Weaknesses.Add("Insufficient board wipes (only " + comp.BoardWipes + ") — add Wrath of God, Blasphemous Act, or Toxic Deluge to recover from losing positions");

        if (comp.Lands < 33) result.Weaknesses.Add("Dangerously low land count (" + comp.Lands + "/33 minimum) — add " + (33 - comp.Lands) + "+ lands to avoid mana screw");
        else if (comp.Lands < 35) result.Weaknesses.Add("Land count is on the low side (" + comp.Lands + ") — consider adding " + (35 - comp.Lands) + " more lands or low-CMC mana rocks");
        if (comp.Lands > 40) result.Weaknesses.Add("High land count (" + comp.Lands + ") may lead to flooding — consider cutting " + (comp.Lands - 38) + " lands for more spells");

        if (result.AverageCmc > 4.0) result.Weaknesses.Add("Very high average CMC (" + result.AverageCmc.ToString("F1") + ") — replace expensive spells (5+ CMC) with cheaper alternatives to improve speed");
        else if (result.AverageCmc > 3.5) result.Weaknesses.Add("High average CMC (" + result.AverageCmc.ToString("F1") + ") — try cutting some 5+ CMC cards for 2-3 CMC replacements");

        // Only show mana screw/flood as weakness if not already a strength
        if (!manaIsGood)
        {
            if (mana.ManaScrew > 30) result.Weaknesses.Add("High mana screw risk (" + mana.ManaScrew.ToString("F0") + "%) — add " + (35 - comp.Lands) + "+ lands or lower your curve; target 35-37 lands with 8+ ramp sources");
            else if (mana.ManaScrew > 25) result.Weaknesses.Add("Moderate mana screw risk (" + mana.ManaScrew.ToString("F0") + "%) — consider 1-2 more lands or mana-fixing rocks");
            if (mana.ManaFlood > 25) result.Weaknesses.Add("High mana flood risk (" + mana.ManaFlood.ToString("F0") + "%) — cut 2-3 lands for card draw or utility spells");
        }

        if (result.Strengths.Count == 0)
            result.Strengths.Add("Balanced build with no extreme specialization");
        if (result.Weaknesses.Count == 0)
            result.Weaknesses.Add("No critical weaknesses detected");
    }

    private static string GetCardCategory(CardInfo card)
    {
        if (card.IsCreature) return "Creature";
        if (card.IsInstant) return "Instant";
        if (card.IsSorcery) return "Sorcery";
        if (card.IsArtifact) return "Artifact";
        if (card.IsEnchantment) return "Enchantment";
        if (card.IsPlaneswalker) return "Planeswalker";
        if (card.IsLand) return "Land";
        return "Other";
    }

    private static bool HasPattern(CardInfo card, params string[] patterns)
    {
        var text = card.OracleText;
        return patterns.All(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static StrategyTag MakeTag(string name, List<CardInfo> cards, int totalNonLand)
    {
        return new StrategyTag
        {
            Name = name,
            CardCount = cards.Count,
            Percentage = Math.Round(100.0 * cards.Count / totalNonLand, 1),
            ExampleCards = cards.OrderByDescending(c => c.Impact).Take(5).Select(c => c.Name).ToList()
        };
    }

    private static IEnumerable<string> ExtractCreatureTypes(string typeLine)
    {
        // Types come after " — " or " - " in the type line
        var dashIdx = typeLine.IndexOf('—');
        if (dashIdx < 0) dashIdx = typeLine.IndexOf('-');
        if (dashIdx < 0 || dashIdx >= typeLine.Length - 2) yield break;

        var subtypes = typeLine[(dashIdx + 1)..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var t in subtypes)
        {
            var clean = t.Trim();
            // Skip non-creature subtypes
            if (!string.IsNullOrWhiteSpace(clean) && clean.Length > 1)
                yield return clean;
        }
    }

    private static HashSet<string> ExtractCreatureTypes(string typeLine, string oracleText)
    {
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Common creature types
        var knownTypes = new[]
        {
            "vampire", "zombie", "elf", "goblin", "merfolk", "dragon", "angel",
            "demon", "wizard", "warrior", "knight", "soldier", "human", "beast",
            "sliver", "dinosaur", "pirate", "cat", "elemental", "spirit",
            "cleric", "rogue", "shaman", "druid", "artifact creature", "dinosaur",
            "bird", "fish", "serpent", "wurm", "horror", "sphinx", "faerie",
            "noble", "rat", "bat", "skeleton", "shade", "berserker"
        };
        foreach (var t in knownTypes)
        {
            if (typeLine.Contains(t) || oracleText.Contains(t + "s") || oracleText.Contains(t + " "))
                types.Add(t);
        }
        return types;
    }

    private static HashSet<string> ExtractThemes(string oracleText)
    {
        var themes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var themeKeywords = new Dictionary<string, string[]>
        {
            ["tokens"] = ["create", "token"],
            ["counters"] = ["+1/+1 counter", "-1/-1 counter", "counter on"],
            ["sacrifice"] = ["sacrifice", "when", "dies"],
            ["graveyard"] = ["graveyard", "mill", "return from your graveyard"],
            ["lifegain"] = ["gain", "life"],
            ["lifeloss"] = ["lose life", "pay life"],
            ["combat"] = ["attack", "combat damage", "combat phase"],
            ["etb"] = ["enters the battlefield", "enters"],
            ["spellcast"] = ["whenever you cast", "whenever a player casts"],
            ["draw"] = ["draw a card", "draw cards"],
            ["equip"] = ["equip", "equipped creature"],
            ["enchant"] = ["enchant", "aura"],
            ["discard"] = ["discard", "hand"],
            ["exile"] = ["exile"],
            ["copy"] = ["copy", "copies"],
        };
        foreach (var (theme, keywords) in themeKeywords)
        {
            if (keywords.Any(k => oracleText.Contains(k)))
                themes.Add(theme);
        }
        return themes;
    }

    /// <summary>Checks if a card's color requirement is met by the deck's color identity.</summary>
    private static bool ColorsMatch(string requiredColors, List<string> deckColors)
    {
        if (string.IsNullOrEmpty(requiredColors)) return true; // colorless
        return requiredColors.All(c => deckColors.Contains(c.ToString()));
    }

    /// <summary>Generic helper: suggest up to N cards from a list, skipping existing cards and color mismatches.</summary>
    private static void AddCategoryRecommendations(
        DeckAnalysisResult result, HashSet<string> existingCards, List<string> colors,
        string category, int max, List<(string name, string colorReq, string reason, double impact)> suggestions)
    {
        int added = 0;
        foreach (var (name, colorReq, reason, impact) in suggestions)
        {
            if (added >= max) break;
            if (existingCards.Contains(name)) continue;
            if (!ColorsMatch(colorReq, colors)) continue;

            result.AddRecommendations.Add(new CardRecommendation
            {
                CardName = name,
                Reason = reason,
                Category = category,
                EstimatedImpact = impact,
            });
            added++;
        }
    }

    private static List<(string name, string colorReq, string reason, double impact)> GetRampSuggestions() =>
    [
        ("Sol Ring", "", "Essential mana rock for every Commander deck", 18),
        ("Arcane Signet", "", "Efficient color-fixing mana rock", 14),
        ("Fellwar Stone", "", "Versatile 2-mana rock that taps for opponents' colors", 8),
        ("Mind Stone", "", "2-mana rock that draws a card when no longer needed", 7),
        ("Cultivate", "G", "Reliable land ramp that fixes colors", 9),
        ("Kodama's Reach", "G", "Consistent land ramp and color fixing", 9),
        ("Nature's Lore", "G", "Efficient 2-mana land ramp into any Forest", 8),
        ("Three Visits", "G", "Premium 2-mana land ramp", 8),
        ("Rampant Growth", "G", "Simple, reliable 2-mana ramp", 6),
        ("Sakura-Tribe Elder", "G", "Creature-based ramp that blocks then sacrifices", 7),
        ("Thought Vessel", "", "2-mana rock with no max hand size", 7),
        ("Wayfarer's Bauble", "", "Colorless land ramp for non-green decks", 5),
        ("Solemn Simulacrum", "", "Land ramp + card draw on death in any color", 7),
        ("Burnished Hart", "", "Repeatable land ramp for non-green decks", 5),
    ];

    private static List<(string name, string colorReq, string reason, double impact)> GetDrawSuggestions() =>
    [
        ("Rhystic Study", "U", "Premier card draw enchantment in Commander", 16),
        ("Mystic Remora", "U", "Powerful early-game card advantage engine", 13),
        ("Esper Sentinel", "W", "White's best card draw engine", 12),
        ("Sylvan Library", "G", "Powerful card selection and draw for 2 mana", 14),
        ("Phyrexian Arena", "B", "Reliable recurring card draw each upkeep", 8),
        ("Beast Whisperer", "G", "Consistent draw for creature-heavy decks", 7),
        ("Night's Whisper", "B", "Efficient 2-mana draw-two spell", 6),
        ("Read the Bones", "B", "Scry 2 + draw 2 for just 3 mana", 5),
        ("Harmonize", "G", "Straightforward 3-card draw in green", 5),
        ("Brainstorm", "U", "Efficient card selection at instant speed", 8),
        ("Ponder", "U", "Premium 1-mana card selection", 8),
        ("Preordain", "U", "Top-tier 1-mana cantrip", 7),
        ("Sign in Blood", "B", "Simple 2-mana draw-two", 5),
        ("Skullclamp", "", "Draws 2 cards when equipped creature dies — absurdly efficient", 14),
        ("Guardian Project", "G", "Draw a card for each nontoken creature entering", 8),
        ("The One Ring", "", "Massive card advantage engine (draw increases each turn)", 15),
    ];

    private static List<(string name, string colorReq, string reason, double impact)> GetRemovalSuggestions() =>
    [
        ("Swords to Plowshares", "W", "Best single-target removal in the format", 14),
        ("Path to Exile", "W", "Premium exile-based removal for 1 mana", 10),
        ("Beast Within", "G", "Versatile removal that hits any permanent", 9),
        ("Generous Gift", "W", "Flexible removal that hits anything", 8),
        ("Chaos Warp", "R", "Red's best universal permanent removal", 8),
        ("Assassin's Trophy", "BG", "Efficient 2-mana removal for any permanent", 9),
        ("Anguished Unmaking", "WB", "Exile any nonland permanent for 3 mana", 8),
        ("Nature's Claim", "G", "1-mana artifact/enchantment removal", 7),
        ("Vandalblast", "R", "Powerful one-sided artifact removal", 8),
        ("Abrupt Decay", "BG", "Uncounterable removal for low-cost threats", 7),
        ("Despark", "WB", "Efficient removal for 4+ CMC permanents", 6),
        ("Reality Shift", "U", "Blue's best creature exile effect", 6),
        ("Rapid Hybridization", "U", "1-mana instant creature removal in blue", 6),
        ("Pongify", "U", "1-mana instant creature removal in blue", 5),
        ("Go for the Throat", "B", "Efficient 2-mana creature removal", 6),
        ("Infernal Grasp", "B", "2-mana instant removal at 2 life", 6),
    ];

    private static List<(string name, string colorReq, string reason, double impact)> GetBoardWipeSuggestions() =>
    [
        ("Cyclonic Rift", "U", "The best board wipe in Commander — one-sided bounce", 16),
        ("Toxic Deluge", "B", "Flexible, efficient board wipe that bypasses indestructible", 12),
        ("Blasphemous Act", "R", "Typically costs just 1 red mana to cast", 9),
        ("Wrath of God", "W", "Classic 4-mana board wipe", 7),
        ("Farewell", "W", "Versatile exile-based wipe hitting multiple categories", 9),
        ("Austere Command", "W", "Modal board wipe with huge flexibility", 7),
        ("Damnation", "B", "4-mana unconditional board wipe in black", 8),
        ("Supreme Verdict", "WU", "Uncounterable 4-mana board wipe", 8),
        ("Merciless Eviction", "WB", "Exile-based wipe that hits any permanent type", 7),
        ("Vanquish the Horde", "W", "Often costs just WW with many creatures in play", 6),
    ];

    private static void AddLandRecommendations(
        DeckAnalysisResult result, HashSet<string> existingCards, List<string> colors, DeckComposition composition)
    {
        var landSuggestions = new List<(string name, string colorReq, string reason, double impact)>();
        int colorCount = colors.Count;

        // Command Tower for multicolor decks
        if (colorCount >= 2)
            landSuggestions.Add(("Command Tower", "", "Taps for any color in your commander's identity", 12));

        // Fetch lands (for 2+ colors)
        if (colorCount >= 2)
        {
            var fetchLands = new List<(string name, string colorReq)>
            {
                ("Polluted Delta", "UB"), ("Flooded Strand", "WU"), ("Bloodstained Mire", "BR"),
                ("Wooded Foothills", "RG"), ("Windswept Heath", "WG"), ("Marsh Flats", "WB"),
                ("Scalding Tarn", "UR"), ("Verdant Catacombs", "BG"), ("Arid Mesa", "WR"),
                ("Misty Rainforest", "UG"),
            };
            foreach (var (name, req) in fetchLands)
            {
                if (ColorsMatch(req, colors))
                    landSuggestions.Add((name, "", $"Premium fetch land — fixes mana and shuffles library", 12));
            }

            // Budget fetches
            landSuggestions.Add(("Fabled Passage", "", "Budget-friendly fetch land for any basic", 7));
            landSuggestions.Add(("Prismatic Vista", "", "Fetches any basic land type", 9));
            landSuggestions.Add(("Terramorphic Expanse", "", "Budget fetch land for any basic", 4));
            landSuggestions.Add(("Evolving Wilds", "", "Budget fetch land for any basic", 4));
        }

        // Shock lands (for 2+ colors)
        if (colorCount >= 2)
        {
            var shockLands = new List<(string name, string colorReq)>
            {
                ("Hallowed Fountain", "WU"), ("Watery Grave", "UB"), ("Blood Crypt", "BR"),
                ("Stomping Ground", "RG"), ("Temple Garden", "WG"), ("Godless Shrine", "WB"),
                ("Steam Vents", "UR"), ("Overgrown Tomb", "BG"), ("Sacred Foundry", "WR"),
                ("Breeding Pool", "UG"),
            };
            foreach (var (name, req) in shockLands)
            {
                if (ColorsMatch(req, colors))
                    landSuggestions.Add((name, "", "Dual land type fetchable with shock lands and Nature's Lore", 9));
            }
        }

        // Utility lands for any deck
        landSuggestions.Add(("Reliquary Tower", "", "No maximum hand size — essential for card draw decks", 6));
        landSuggestions.Add(("War Room", "", "Colorless card draw land for any deck", 5));

        if (colorCount >= 3)
        {
            landSuggestions.Add(("Exotic Orchard", "", "Taps for any color opponents can produce", 7));
            landSuggestions.Add(("City of Brass", "", "5-color land at 1 life per tap", 7));
            landSuggestions.Add(("Mana Confluence", "", "5-color land at 1 life per tap", 7));
        }

        // Only add if missing — limit to 3 land suggestions
        AddCategoryRecommendations(result, existingCards, colors, "Land", 3, landSuggestions);
    }

    private static void AddFastManaSuggestions(
        DeckAnalysisResult result, HashSet<string> existingCards, List<CardInfo> cards)
    {
        int fastManaCount = cards.Count(c => c.IsFastMana);
        if (fastManaCount >= 5) return; // Already has plenty

        var suggestions = new List<(string name, string colorReq, string reason, double impact)>
        {
            ("Sol Ring", "", "The most essential fast mana in Commander", 18),
            ("Mana Crypt", "", "Free 2-mana rock — premier fast mana", 17),
            ("Chrome Mox", "", "Free mana at the cost of a card — explosive starts", 12),
            ("Mox Diamond", "", "Free mana rock trading a land — speeds up early turns", 12),
            ("Jeweled Lotus", "", "Black Lotus for your commander", 14),
            ("Mana Vault", "", "3 mana for 1 — massive acceleration", 13),
            ("Lotus Petal", "", "Free mana for one turn — enables explosive plays", 8),
            ("Ancient Tomb", "", "Land that taps for 2 colorless at 2 life", 12),
        };

        AddCategoryRecommendations(result, existingCards, result.ColorIdentity, "Fast Mana", 3, suggestions);
    }

    private static void AddTutorSuggestions(
        DeckAnalysisResult result, HashSet<string> existingCards, List<string> colors, DeckComposition composition)
    {
        if (composition.Tutors >= 5) return; // Already has plenty

        var suggestions = new List<(string name, string colorReq, string reason, double impact)>
        {
            ("Demonic Tutor", "B", "Best tutor in the format — searches for anything", 16),
            ("Vampiric Tutor", "B", "Instant-speed tutor to top of library for 1 mana", 14),
            ("Imperial Seal", "B", "1-mana sorcery tutor to top of library", 13),
            ("Enlightened Tutor", "W", "Instant-speed tutor for artifact or enchantment", 11),
            ("Mystical Tutor", "U", "Instant-speed tutor for instant or sorcery", 11),
            ("Worldly Tutor", "G", "Instant-speed creature tutor to top of library", 9),
            ("Diabolic Intent", "B", "2-mana tutor if you have a creature to sacrifice", 9),
            ("Gamble", "R", "1-mana red tutor with random discard risk", 8),
            ("Eladamri's Call", "WG", "2-mana instant creature tutor to hand", 9),
            ("Idyllic Tutor", "W", "3-mana enchantment tutor to hand", 6),
            ("Fabricate", "U", "3-mana artifact tutor to hand", 6),
            ("Green Sun's Zenith", "G", "Tutor + put green creature directly into play", 10),
            ("Chord of Calling", "G", "Instant-speed creature tutor to battlefield", 9),
            ("Finale of Devastation", "G", "Creature tutor that doubles as a finisher", 10),
        };

        AddCategoryRecommendations(result, existingCards, colors, "Tutor", 3, suggestions);
    }

    private static void AddProtectionSuggestions(
        DeckAnalysisResult result, HashSet<string> existingCards, List<string> colors, List<CardInfo> cards)
    {
        int counterspells = cards.Count(c => c.IsCounterspell);
        bool hasBlue = colors.Contains("U");

        var suggestions = new List<(string name, string colorReq, string reason, double impact)>
        {
            ("Teferi's Protection", "W", "Phases you out — ultimate protection from everything", 13),
            ("Heroic Intervention", "G", "Gives all your permanents hexproof and indestructible", 9),
            ("Flawless Maneuver", "W", "Free indestructible when your commander is out", 8),
            ("Deflecting Swat", "R", "Free redirect spell when your commander is out", 10),
            ("Fierce Guardianship", "U", "Free counterspell when your commander is out", 12),
            ("Force of Will", "U", "Free counterspell — the format's best protection", 14),
            ("Force of Negation", "U", "Free counter for noncreature spells on opponents' turns", 11),
            ("Swan Song", "U", "1-mana counter for instant/sorcery/enchantment", 9),
            ("Counterspell", "U", "Classic 2-mana hard counter", 8),
            ("Arcane Denial", "U", "Efficient 2-mana counter that replaces itself", 7),
            ("Dovin's Veto", "WU", "Uncounterable noncreature counter for 2 mana", 7),
            ("An Offer You Can't Refuse", "U", "1-mana counter giving opponent treasure tokens", 7),
            ("Lightning Greaves", "", "Free equip haste + shroud for your commander", 10),
            ("Swiftfoot Boots", "", "Haste + hexproof for your key creatures", 7),
        };

        // If already has 4+ counterspells, only suggest non-counter protection
        if (counterspells >= 4 && hasBlue)
        {
            suggestions = suggestions.Where(s =>
                !s.reason.Contains("counter", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(s.colorReq) || !s.colorReq.Contains('U')
            ).ToList();
        }

        AddCategoryRecommendations(result, existingCards, colors, "Protection", 3, suggestions);
    }

    private static void AddStapleSuggestions(
        DeckAnalysisResult result, HashSet<string> existingCards, List<string> colors)
    {
        // High-impact format staples that don't fit neatly into other categories
        var suggestions = new List<(string name, string colorReq, string reason, double impact)>
        {
            ("The One Ring", "", "Massive card advantage engine with protection on ETB", 15),
            ("Smothering Tithe", "W", "Generates treasure every time opponents draw", 14),
            ("Dockside Extortionist", "R", "Explosive treasure generation in artifact-heavy metas", 15),
            ("Orcish Bowmasters", "B", "Punishes card draw and creates a growing army", 13),
            ("Opposition Agent", "B", "Steals opponents' tutors and searches", 11),
            ("Drannith Magistrate", "W", "Shuts down commanders and cascade/flashback", 10),
            ("Thassa's Oracle", "U", "Compact win condition for combo decks", 12),
            ("Underworld Breach", "R", "Recursive engine that enables game-winning combos", 11),
            ("Carpet of Flowers", "G", "Free mana vs blue opponents", 10),
            ("Land Tax", "W", "Ensures land drops and thins the deck", 9),
            ("Sensei's Divining Top", "", "Repeatable card selection for 1 mana", 12),
            ("Bolas's Citadel", "B", "Play cards from library — generates insane advantage", 10),
            ("Seedborn Muse", "G", "Untap all permanents on each opponent's turn", 10),
            ("Aura Shards", "WG", "Destroys artifact/enchantment on each creature ETB", 9),
            ("Farewell", "W", "Exile-based board wipe hitting multiple categories", 9),
            ("Trouble in Pairs", "W", "Card draw whenever opponents play extra cards or attack", 8),
        };

        // Only suggest staples not already covered by other categories
        var alreadySuggested = result.AddRecommendations.Select(r => r.CardName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filteredSuggestions = suggestions
            .Where(s => !alreadySuggested.Contains(s.name))
            .ToList();

        AddCategoryRecommendations(result, existingCards, colors, "Staple", 3, filteredSuggestions);
    }

    [GeneratedRegex(@"create[^.]*?(\d+/\d+)\s+([^.]*?)tokens?", RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();
}

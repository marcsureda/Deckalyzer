using System.Text.RegularExpressions;
using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Application.Services;

public partial class DeckParserService : IDeckParser
{
    public (List<string> commanders, List<(int quantity, string name)> mainCards, List<(int quantity, string name)> sideboardCards, List<string> warnings) ParseDeckList(string deckList)
    {
        var commanders = new List<string>();
        var mainCards = new List<(int quantity, string name)>();
        var sideboardCards = new List<(int quantity, string name)>();
        var warnings = new List<string>();
        var allLines = deckList.Split(['\r', '\n']);

        // Track sections: commander, main/deck, sideboard, companion, maybeboard, post-sideboard
        string currentSection = "main"; // default section
        bool sawSideboard = false;

        // First pass: detect if there are blank-line-separated blocks (Moxfield/Archidekt format)
        var blocks = SplitIntoBlocks(allLines);

        // Track blank lines to detect post-sideboard commander
        bool lastLineWasBlank = false;

        foreach (var rawLine in allLines)
        {
            var line = rawLine.Trim();

            // Track blank lines — a blank line after sideboard could signal commander section
            if (string.IsNullOrWhiteSpace(line))
            {
                lastLineWasBlank = true;
                continue;
            }

            // If we were in sideboard and hit a blank line followed by a card,
            // this might be the commander at the end
            if (lastLineWasBlank && sawSideboard && currentSection == "sideboard")
            {
                currentSection = "post-sideboard";
            }
            lastLineWasBlank = false;

            // Check for section headers
            if (line.StartsWith("Commander", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "commander";
                var colonIdx = line.IndexOf(':');
                if (colonIdx >= 0 && colonIdx < line.Length - 1)
                {
                    var cmdName = ParseCardName(line[(colonIdx + 1)..]);
                    if (!string.IsNullOrWhiteSpace(cmdName) && !commanders.Contains(cmdName, StringComparer.OrdinalIgnoreCase))
                        commanders.Add(cmdName);
                }
                continue;
            }

            if (line.StartsWith("Deck", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Main", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "main";
                continue;
            }

            if (line.StartsWith("Sideboard", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "sideboard";
                sawSideboard = true;
                continue;
            }

            if (line.StartsWith("Companion", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Maybeboard", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "skip";
                continue;
            }

            // Skip comment lines
            if (line.StartsWith("//") || line.StartsWith('#'))
                continue;

            // Parse card entry
            var match = CardLineRegex().Match(line);
            if (match.Success)
            {
                var quantity = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1;
                var cardName = ParseCardName(match.Groups[2].Value);

                if (string.IsNullOrWhiteSpace(cardName))
                    continue;

                if (currentSection == "commander")
                {
                    if (!commanders.Contains(cardName, StringComparer.OrdinalIgnoreCase))
                        commanders.Add(cardName);
                    mainCards.Add((quantity, cardName));
                }
                else if (currentSection == "post-sideboard")
                {
                    // Cards after sideboard, separated by blank line = commander(s)
                    if (!commanders.Contains(cardName, StringComparer.OrdinalIgnoreCase))
                        commanders.Add(cardName);
                    mainCards.Add((quantity, cardName));
                }
                else if (currentSection == "sideboard")
                {
                    sideboardCards.Add((quantity, cardName));
                }
                else if (currentSection == "skip")
                {
                    // Companion / Maybeboard — ignore
                }
                else
                {
                    mainCards.Add((quantity, cardName));
                }
            }
        }

        // Auto-detect commander if no explicit section header was found
        if (commanders.Count == 0 && mainCards.Count > 0)
        {
            // Strategy 1: if there are blank-line-separated blocks, check if last block
            // is 1-2 cards (common Moxfield export: deck block, then blank line, then commander(s))
            if (blocks.Count >= 2)
            {
                var lastBlock = blocks[^1];
                if (lastBlock.Count is 1 or 2)
                {
                    foreach (var (_, name) in lastBlock)
                        commanders.Add(name);
                }
                // Also check first block if it's 1-2 cards (partner commanders at top)
                else if (blocks[0].Count is 1 or 2)
                {
                    foreach (var (_, name) in blocks[0])
                        commanders.Add(name);
                }
            }

            // Strategy 2: first card in the list is the commander (most common convention)
            if (commanders.Count == 0)
                commanders.Add(mainCards[0].name);
        }

        return (commanders, mainCards, sideboardCards, warnings);
    }

    private static string ParseCardName(string raw)
    {
        var cardName = raw.Trim();
        // Remove set codes like (ABC) or [ABC] at the end
        cardName = SetCodeRegex().Replace(cardName, "").Trim();
        // Remove collector number like *123* or #123
        cardName = CollectorNumberRegex().Replace(cardName, "").Trim();
        // Remove trailing numbers that some exporters add (e.g., "Sol Ring 1")
        cardName = TrailingNumberRegex().Replace(cardName, "").Trim();
        return cardName;
    }

    /// <summary>
    /// Splits the raw lines into blocks separated by blank lines.
    /// Each block is a list of parsed (quantity, name) pairs.
    /// </summary>
    private static List<List<(int quantity, string name)>> SplitIntoBlocks(string[] allLines)
    {
        var blocks = new List<List<(int quantity, string name)>>();
        var current = new List<(int quantity, string name)>();

        foreach (var rawLine in allLines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                if (current.Count > 0)
                {
                    blocks.Add(current);
                    current = [];
                }
                continue;
            }

            // Skip headers and comments
            if (line.StartsWith("Commander", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Deck", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Main", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Sideboard", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Companion", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Maybeboard", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("//") || line.StartsWith('#'))
                continue;

            var match = CardLineRegex().Match(line);
            if (match.Success)
            {
                var quantity = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1;
                var cardName = SetCodeRegex().Replace(match.Groups[2].Value, "").Trim();
                cardName = CollectorNumberRegex().Replace(cardName, "").Trim();
                if (!string.IsNullOrWhiteSpace(cardName))
                    current.Add((quantity, cardName));
            }
        }

        if (current.Count > 0)
            blocks.Add(current);

        return blocks;
    }

    [GeneratedRegex(@"^(?:(\d+)\s*[xX]?\s+)?(.+)$")]
    private static partial Regex CardLineRegex();

    [GeneratedRegex(@"\s*[\(\[][A-Z0-9]+[\)\]]\s*$")]
    private static partial Regex SetCodeRegex();

    [GeneratedRegex(@"\s*\*\d+\*\s*$")]
    private static partial Regex CollectorNumberRegex();

    [GeneratedRegex(@"\s+\d+$")]
    private static partial Regex TrailingNumberRegex();
}

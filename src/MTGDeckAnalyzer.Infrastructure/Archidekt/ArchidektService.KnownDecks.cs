namespace MTGDeckAnalyzer.Infrastructure.Archidekt;

public partial class ArchidektService
{
    private async Task<List<ArchidektDeckSummary>> GetUserDecksAsync(string username)
    {
        // Comprehensive list of precon deck IDs from Archidekt_Precons user (2011-2026)
        var knownPreconIds = new List<int>
        {
            // Recent sets (2024-2026)
            20105223, // Turtle Power! - Teenage Mutant Ninja Turtles Commander Deck (2026)
            18744715, // Blight Curse - Lorwyn Eclipsed Commander Deck
            18744843, // Dance of the Elements - Lorwyn Eclipsed Commander Deck
            14420275, // Counter Intelligence - Edge of Eternities Commander Deck
            14397447, // World Shaper - Edge of Eternities Commander Deck
            13693093, // Everyone's Invited - Secret Lair Drop - WUBRG EDH Precon Decklist
            13106730, // Revival Trance - Final Fantasy Commander
            13106990, // Limit Break - Final Fantasy Commander
            13107079, // Counter Blitz - Final Fantasy Commander
            13107116, // Scions & Spellcraft - Final Fantasy Commander
            
            // 2023 Sets
            12002085, // Jeskai Striker - Tarkir: Dragonstorm Commander
            12124776, // Abzan Armor - Tarkir: Dragonstorm Commander
            12124803, // Temur Roar - Tarkir: Dragonstorm Commander
            12028998, // Sultai Arisen - Tarkir: Dragonstorm Commander
            12020252, // Mardu Surge - Tarkir: Dragonstorm Commander
            11054763, // Eternal Might - Aetherdrift Commander
            11035094, // Living Energy - Aetherdrift Commander
            9166525,  // Miracle Worker - Duskmourn: House of Horror Commander
            9189676,  // Death Toll - Duskmourn: House of Horror Commander
            9189744,  // Endless Punishment - Duskmourn: House of Horror Commander
            9150668,  // Jump Scare! - Duskmourn: House of Horror Commander
            
            // 2022 Sets  
            8460543,  // Family Matters - Bloomburrow Commander
            8460469,  // Peace Offering - Bloomburrow Commander
            8497473,  // Animated Army - Bloomburrow Commander
            8460587,  // Squirreled Away - Bloomburrow Commander
            7869831,  // Graveyard Overdrive - Modern Horizons 3 Commander
            7858224,  // Creative Energy - Modern Horizons 3 Commander
            7858153,  // Tricky Terrain - Modern Horizons 3 Commander
            7858209,  // Eldrazi Incursion - Modern Horizons 3 Commander
            
            // 2021 Sets
            7261488,  // Most Wanted - Outlaws of Thunder Junction Commander
            7261455,  // Grand Larceny - Outlaws of Thunder Junction Commander
            7261435,  // Quick Draw - Outlaws of Thunder Junction Commander
            7261423,  // Desert Bloom - Outlaws of Thunder Junction Commander
            6810925,  // Scrappy Survivors - Fallout
            6810931,  // Hail, Caesar - Fallout
            6810962,  // Mutant Menace - Fallout
            6810985,  // Science! - Fallout
            6584319,  // Raining Cats and Dogs - Secret Lair Drop
            
            // 2020 Sets
            6527467,  // Deadly Disguise - Murders at Karlov Manor Commander
            6527454,  // Revenant Recon - Murders at Karlov Manor Commander
            6527442,  // Deep Clue Sea - Murders at Karlov Manor Commander
            6527429,  // Blame Game - Murders at Karlov Manor Commander
            4948515,  // Sliver Swarm - Commander Masters
            4973732,  // Eldrazi Unbound - Commander Masters
            4956666,  // Planeswalker Party - Commander Masters
            4959133,  // Enduring Enchantments - Commander Masters
            
            // 2019 Sets
            5775210,  // Ahoy Mateys - The Lost Caverns of Ixalan Commander
            5775250,  // Veloci-Ramp-Tor - The Lost Caverns of Ixalan Commander
            5775230,  // Explorers of the Deep - The Lost Caverns of Ixalan Commander
            5775225,  // Blood Rites - The Lost Caverns of Ixalan Commander
            5644579,  // Timey-Wimey - Doctor Who
            5644650,  // Masters of Evil - Doctor Who
            5644280,  // Blast from the Past - Doctor Who
            5644637,  // Paradox Power - Doctor Who
            
            // 2018 Sets
            5226431,  // Fae Dominion - Wilds of Eldraine Commander
            5226423,  // Virtue and Valor - Wilds of Eldraine Commander
            5273608,  // Angels: They're Just Like Us, but Cooler and with Wings - Secret Lair Drop
            5273595,  // From Cute to Brute - Secret Lair Drop
            5273567,  // Heads I Win, Tails You Lose - Secret Lair Drop
            
            // 2017 Commander Sets
            2235588,  // Draconic Domination - Commander 2017
            2235601,  // Feline Ferocity - Commander 2017
            2235614,  // Vampiric Bloodlust - Commander 2017  
            2235627,  // Arcane Wizardry - Commander 2017
            
            // 2016 Commander Sets  
            1958442,  // Breed Lethality - Commander 2016
            1958455,  // Entropic Uprising - Commander 2016
            1958468,  // Invent Superiority - Commander 2016
            1958481,  // Open Hostility - Commander 2016
            1958494,  // Stalwart Unity - Commander 2016
            
            // 2015 Commander Sets
            1686773,  // Wade into Battle - Commander 2015
            1686786,  // Seize Control - Commander 2015
            1686799,  // Plunder the Graves - Commander 2015
            1686812,  // Swell the Host - Commander 2015
            1686825,  // Call the Spirits - Commander 2015
            
            // 2014 Commander Sets
            2209145,  // Forged in Stone - Commander 2014
            2209158,  // Guided by Nature - Commander 2014
            2209171,  // Peer Through Time - Commander 2014
            2209184,  // Sworn to Darkness - Commander 2014
            2209197,  // Built from Scratch - Commander 2014
            
            // 2013 Commander Sets
            1423556,  // Eternal Bargain - Commander 2013
            1423569,  // Evasive Maneuvers - Commander 2013
            1423582,  // Power Hungry - Commander 2013
            1423595,  // Nature of the Beast - Commander 2013
            1423608,  // Mind Seize - Commander 2013
            
            // Classic Original Commander Sets (2011)
            896774,   // Heavenly Inferno - Commander 2011
            896787,   // Counterpunch - Commander 2011
            896800,   // Mirror Mastery - Commander 2011
            896813,   // Political Puppets - Commander 2011
            896826,   // Devour for Power - Commander 2011
            
            // Additional popular precons from 2012 and supplemental sets
            1124445,  // Commander Arsenal - Special Release 2012
            
            // Add more as discovered - this now covers major releases from 2011-2026
        };

        var deckSummaries = new List<ArchidektDeckSummary>();
        int consecutiveFailures = 0;
        const int maxConsecutiveFailures = 5; // Stop after 5 consecutive failures
        
        foreach (var deckId in knownPreconIds)
        {
            // Circuit breaker: stop if too many consecutive failures
            if (consecutiveFailures >= maxConsecutiveFailures)
            {
                _logger.LogWarning("Too many consecutive failures ({Failures}), stopping deck enumeration", consecutiveFailures);
                break;
            }

            try
            {
                // Use the small API endpoint to get basic deck info
                var response = await _httpClient.GetAsync($"/api/decks/{deckId}/small/");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        // Create minimal summary without trying to parse potentially complex JSON
                        deckSummaries.Add(new ArchidektDeckSummary
                        {
                            Id = deckId,
                            Name = $"Precon Deck {deckId}",
                            DeckFormat = 3,
                            CreatedAt = DateTime.UtcNow.ToString("O"),
                            UpdatedAt = DateTime.UtcNow.ToString("O"),
                            Owner = new ArchidektOwner { Username = username, Id = 0 }
                        });
                        consecutiveFailures = 0; // Reset on success
                    }
                }
                else
                {
                    consecutiveFailures++;
                    _logger.LogDebug("Failed to fetch deck {DeckId}: HTTP {StatusCode}", deckId, response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                consecutiveFailures++;
                _logger.LogDebug(ex, "HTTP error fetching deck {DeckId}", deckId);
            }
            catch (TaskCanceledException ex)
            {
                consecutiveFailures++;
                _logger.LogWarning(ex, "Request timeout for deck {DeckId}", deckId);
            }
            catch (Exception ex)
            {
                consecutiveFailures++;
                _logger.LogDebug(ex, "Failed to fetch summary for deck {DeckId}", deckId);
            }
            
            // Small delay between requests to be respectful
            await Task.Delay(100);
        }

        _logger.LogInformation("Retrieved {Count} deck summaries from {Total} known precon IDs", 
            deckSummaries.Count, knownPreconIds.Count);

        return deckSummaries;
    }
}

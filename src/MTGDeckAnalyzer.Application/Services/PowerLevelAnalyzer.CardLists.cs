namespace MTGDeckAnalyzer.Application.Services;

public partial class PowerLevelAnalyzer
{
    // Well-known card lists for classification
    private static readonly HashSet<string> ExtraTurnCards = new(StringComparer.OrdinalIgnoreCase)
    {
        "Time Warp", "Temporal Manipulation", "Time Stretch", "Expropriate",
        "Nexus of Fate", "Alrund's Epiphany", "Extra Turn", "Temporal Mastery",
        "Capture of Jingzhou", "Walk the Aeons", "Beacon of Tomorrows",
        "Part the Waterveil", "Karn's Temporal Sundering", "Savor the Moment",
        "Temporal Trespass", "Notorious Throng", "Medomai the Ageless",
        "Wanderwine Prophets"
    };

    private static readonly HashSet<string> MassLandDenialCards = new(StringComparer.OrdinalIgnoreCase)
    {
        "Armageddon", "Ravages of War", "Catastrophe", "Decree of Annihilation",
        "Obliterate", "Jokulhaups", "Devastation", "Boom // Bust",
        "Keldon Firebombers", "Ruination", "From the Ashes", "Blood Moon",
        "Back to Basics", "Winter Orb", "Static Orb", "Stasis",
        "Hokori, Dust Drinker", "Rising Waters", "Sunder", "Worldslayer",
        "Mycosynth Lattice"
    };

    // Official WotC Game Changers list (fallback — Scryfall's game_changer field is preferred)
    private static readonly HashSet<string> GameChangerCardsFallback = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ad Nauseam", "Ancient Tomb", "Aura Shards", "Biorhythm",
        "Bolas's Citadel", "Braids, Cabal Minion", "Chrome Mox",
        "Coalition Victory", "Consecrated Sphinx", "Crop Rotation",
        "Cyclonic Rift", "Demonic Tutor", "Drannith Magistrate",
        "Enlightened Tutor", "Farewell", "Field of the Dead",
        "Fierce Guardianship", "Force of Will", "Gaea's Cradle",
        "Gamble", "Gifts Ungiven", "Glacial Chasm",
        "Grand Arbiter Augustin IV", "Grim Monolith", "Humility",
        "Imperial Seal", "Intuition", "Jeska's Will",
        "Lion's Eye Diamond", "Mana Vault", "Mishra's Workshop",
        "Mox Diamond", "Mystical Tutor", "Narset, Parter of Veils",
        "Natural Order", "Necropotence", "Notion Thief",
        "Opposition Agent", "Orcish Bowmasters", "Panoptic Mirror",
        "Rhystic Study", "Seedborn Muse", "Serra's Sanctum",
        "Smothering Tithe", "Survival of the Fittest", "Teferi's Protection",
        "Tergrid, God of Fright", "Tergrid, God of Fright // Tergrid's Lantern",
        "Thassa's Oracle", "The One Ring",
        "The Tabernacle at Pendrell Vale", "Underworld Breach",
        "Vampiric Tutor", "Worldly Tutor"
    };

    private static readonly HashSet<string> FastManaCards = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sol Ring", "Mana Crypt", "Mana Vault", "Chrome Mox", "Mox Diamond",
        "Mox Opal", "Mox Amber", "Jeweled Lotus", "Lotus Petal",
        "Dark Ritual", "Cabal Ritual", "Elvish Spirit Guide",
        "Simian Spirit Guide", "Rite of Flame", "Desperate Ritual",
        "Pyretic Ritual", "Ancient Tomb", "Grim Monolith",
        "Lion's Eye Diamond"
    };

    private static readonly HashSet<string> TutorCards = new(StringComparer.OrdinalIgnoreCase)
    {
        "Demonic Tutor", "Vampiric Tutor", "Imperial Seal", "Mystical Tutor",
        "Enlightened Tutor", "Worldly Tutor", "Gamble", "Idyllic Tutor",
        "Diabolic Intent", "Grim Tutor", "Wishclaw Talisman",
        "Scheming Symmetry", "Demonic Consultation", "Tainted Pact",
        "Dimir Machinations", "Muddle the Mixture", "Drift of Phantasms",
        "Chord of Calling", "Green Sun's Zenith", "Finale of Devastation",
        "Natural Order", "Eldritch Evolution", "Neoform", "Birthing Pod",
        "Prime Speaker Vannifar", "Tooth and Nail", "Defense of the Heart",
        "Fabricate", "Whir of Invention", "Reshape", "Transmute Artifact",
        "Trinket Mage", "Trophy Mage", "Tribute Mage", "Urza's Saga",
        "Crop Rotation", "Intuition", "Gifts Ungiven", "Survival of the Fittest"
    };

    // Known infinite combo pairs with descriptions and Commander Spellbook URLs
    private static readonly Dictionary<string, HashSet<string>> ComboPairs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Thassa's Oracle"] = new(StringComparer.OrdinalIgnoreCase) { "Demonic Consultation", "Tainted Pact", "Leveler", "Paradigm Shift" },
        ["Demonic Consultation"] = new(StringComparer.OrdinalIgnoreCase) { "Thassa's Oracle", "Jace, Wielder of Mysteries", "Laboratory Maniac" },
        ["Tainted Pact"] = new(StringComparer.OrdinalIgnoreCase) { "Thassa's Oracle", "Jace, Wielder of Mysteries", "Laboratory Maniac" },
        ["Exquisite Blood"] = new(StringComparer.OrdinalIgnoreCase) { "Sanguine Bond", "Vito, Thorn of the Dusk Rose", "Marauding Blight-Priest" },
        ["Sanguine Bond"] = new(StringComparer.OrdinalIgnoreCase) { "Exquisite Blood" },
        ["Dramatic Reversal"] = new(StringComparer.OrdinalIgnoreCase) { "Isochron Scepter" },
        ["Isochron Scepter"] = new(StringComparer.OrdinalIgnoreCase) { "Dramatic Reversal" },
        ["Kiki-Jiki, Mirror Breaker"] = new(StringComparer.OrdinalIgnoreCase) { "Zealous Conscripts", "Pestermite", "Deceiver Exarch", "Combat Celebrant", "Felidar Guardian" },
        ["Zealous Conscripts"] = new(StringComparer.OrdinalIgnoreCase) { "Kiki-Jiki, Mirror Breaker" },
        ["Pestermite"] = new(StringComparer.OrdinalIgnoreCase) { "Kiki-Jiki, Mirror Breaker", "Splinter Twin" },
        ["Deceiver Exarch"] = new(StringComparer.OrdinalIgnoreCase) { "Kiki-Jiki, Mirror Breaker", "Splinter Twin" },
        ["Splinter Twin"] = new(StringComparer.OrdinalIgnoreCase) { "Pestermite", "Deceiver Exarch" },
        ["Heliod, Sun-Crowned"] = new(StringComparer.OrdinalIgnoreCase) { "Walking Ballista", "Triskelion" },
        ["Walking Ballista"] = new(StringComparer.OrdinalIgnoreCase) { "Heliod, Sun-Crowned", "Mikaeus, the Unhallowed" },
        ["Mikaeus, the Unhallowed"] = new(StringComparer.OrdinalIgnoreCase) { "Walking Ballista", "Triskelion" },
        ["Triskelion"] = new(StringComparer.OrdinalIgnoreCase) { "Mikaeus, the Unhallowed", "Heliod, Sun-Crowned" },
        ["Devoted Druid"] = new(StringComparer.OrdinalIgnoreCase) { "Vizier of Remedies", "Swift Reconfiguration" },
        ["Vizier of Remedies"] = new(StringComparer.OrdinalIgnoreCase) { "Devoted Druid" },
        ["Food Chain"] = new(StringComparer.OrdinalIgnoreCase) { "Misthollow Griffin", "Eternal Scourge", "Squee, the Immortal" },
        ["Underworld Breach"] = new(StringComparer.OrdinalIgnoreCase) { "Lion's Eye Diamond", "Brain Freeze" },
        ["Lion's Eye Diamond"] = new(StringComparer.OrdinalIgnoreCase) { "Underworld Breach", "Auriok Salvagers" },
        ["Deadeye Navigator"] = new(StringComparer.OrdinalIgnoreCase) { "Peregrine Drake", "Great Whale", "Palinchron" },
        ["Peregrine Drake"] = new(StringComparer.OrdinalIgnoreCase) { "Deadeye Navigator" },
        ["Palinchron"] = new(StringComparer.OrdinalIgnoreCase) { "Deadeye Navigator", "Phantasmal Image", "High Tide" },
        ["Nim Deathmantle"] = new(StringComparer.OrdinalIgnoreCase) { "Ashnod's Altar", "Krark-Clan Ironworks" },
        ["Ashnod's Altar"] = new(StringComparer.OrdinalIgnoreCase) { "Nim Deathmantle", "Animation Module" },
        ["Basalt Monolith"] = new(StringComparer.OrdinalIgnoreCase) { "Rings of Brighthearth", "Power Artifact", "Mesmeric Orb" },
        ["Rings of Brighthearth"] = new(StringComparer.OrdinalIgnoreCase) { "Basalt Monolith" },
        ["Power Artifact"] = new(StringComparer.OrdinalIgnoreCase) { "Basalt Monolith", "Grim Monolith" },
        ["Worldgorger Dragon"] = new(StringComparer.OrdinalIgnoreCase) { "Animate Dead", "Dance of the Dead", "Necromancy" },
        ["Animate Dead"] = new(StringComparer.OrdinalIgnoreCase) { "Worldgorger Dragon" },
    };

    // Placeholder — actual set is built after ThreeCardCombos is initialized (see below)

    // Combo descriptions for UI
    private static readonly Dictionary<string, string> ComboDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Thassa's Oracle+Demonic Consultation"] = "Cast Demonic Consultation naming a card not in your deck to exile your library, then win with Thassa's Oracle's ETB trigger.",
        ["Thassa's Oracle+Tainted Pact"] = "Cast Tainted Pact to exile your entire library (singleton deck), then win with Thassa's Oracle's ETB trigger.",
        ["Thassa's Oracle+Leveler"] = "Leveler exiles your library on ETB; Thassa's Oracle then wins with no cards in library.",
        ["Thassa's Oracle+Paradigm Shift"] = "Paradigm Shift puts your library into exile; Thassa's Oracle then wins.",
        ["Demonic Consultation+Jace, Wielder of Mysteries"] = "Exile your library with Demonic Consultation, then draw with Jace to win.",
        ["Demonic Consultation+Laboratory Maniac"] = "Exile your library with Demonic Consultation, then draw with an empty library to win via Lab Man.",
        ["Tainted Pact+Jace, Wielder of Mysteries"] = "Exile your library with Tainted Pact, then draw with Jace to win.",
        ["Tainted Pact+Laboratory Maniac"] = "Exile your library with Tainted Pact, then draw to win via Lab Man.",
        ["Exquisite Blood+Sanguine Bond"] = "When an opponent loses life, Exquisite Blood gains you life; this triggers Sanguine Bond, creating an infinite loop that kills all opponents.",
        ["Exquisite Blood+Vito, Thorn of the Dusk Rose"] = "Vito converts each lifegain into opponent life loss; Exquisite Blood converts each opponent life loss into lifegain — infinite loop.",
        ["Exquisite Blood+Marauding Blight-Priest"] = "Blight-Priest pings on lifegain; Exquisite Blood gains life on enemy life loss — infinite drain.",
        ["Dramatic Reversal+Isochron Scepter"] = "Imprint Dramatic Reversal on Isochron Scepter. With 3+ mana from nonland sources, activate Scepter → untap all nonland permanents → infinite mana.",
        ["Kiki-Jiki, Mirror Breaker+Zealous Conscripts"] = "Copy Zealous Conscripts with Kiki-Jiki; the copy untaps Kiki-Jiki when it enters → infinite hasty tokens.",
        ["Kiki-Jiki, Mirror Breaker+Pestermite"] = "Copy Pestermite with Kiki-Jiki; the copy untaps Kiki-Jiki → infinite hasty flying tokens.",
        ["Kiki-Jiki, Mirror Breaker+Deceiver Exarch"] = "Copy Deceiver Exarch with Kiki-Jiki; the copy untaps Kiki-Jiki → infinite hasty tokens.",
        ["Kiki-Jiki, Mirror Breaker+Combat Celebrant"] = "Copy Combat Celebrant with Kiki-Jiki; exert the copy for extra combat → infinite combat phases.",
        ["Kiki-Jiki, Mirror Breaker+Felidar Guardian"] = "Copy Felidar Guardian with Kiki-Jiki; it blinks Kiki-Jiki → infinite hasty tokens.",
        ["Splinter Twin+Pestermite"] = "Enchant Pestermite with Splinter Twin; tap to make a copy that untaps Pestermite → infinite hasty tokens.",
        ["Splinter Twin+Deceiver Exarch"] = "Enchant Deceiver Exarch with Splinter Twin; tap to make a copy that untaps it → infinite hasty tokens.",
        ["Heliod, Sun-Crowned+Walking Ballista"] = "Give Walking Ballista lifelink via Heliod; remove a +1/+1 counter to ping → gain life → Heliod adds counter back → infinite damage.",
        ["Heliod, Sun-Crowned+Triskelion"] = "Give Triskelion lifelink via Heliod; remove counters for damage → gain life → get counters back → infinite damage.",
        ["Mikaeus, the Unhallowed+Walking Ballista"] = "Ballista enters with +1/+1 from Mikaeus; remove counters to ping → it dies → undying brings it back → infinite damage.",
        ["Mikaeus, the Unhallowed+Triskelion"] = "Triskelion enters with extra counter from Mikaeus; ping opponents → it dies → undying → infinite damage.",
        ["Devoted Druid+Vizier of Remedies"] = "Vizier prevents -1/-1 counters; untap Devoted Druid infinitely → infinite green mana.",
        ["Devoted Druid+Swift Reconfiguration"] = "Swift Reconfiguration makes Devoted Druid a non-creature; it can still tap for mana and untap → infinite green mana.",
        ["Food Chain+Misthollow Griffin"] = "Exile Misthollow Griffin to Food Chain for mana; recast it from exile → infinite creature mana.",
        ["Food Chain+Eternal Scourge"] = "Exile Eternal Scourge to Food Chain; recast from exile → infinite creature mana.",
        ["Food Chain+Squee, the Immortal"] = "Exile Squee to Food Chain; cast Squee from exile → infinite creature mana.",
        ["Underworld Breach+Lion's Eye Diamond"] = "Cast LED from graveyard via Breach, crack for mana, recast via escape → infinite mana. Add Brain Freeze to win.",
        ["Underworld Breach+Brain Freeze"] = "With cheap spells + Breach, chain storm copies of Brain Freeze to mill opponents out.",
        ["Lion's Eye Diamond+Auriok Salvagers"] = "Return LED from graveyard with Salvagers, crack for mana, repeat → infinite mana of any color.",
        ["Deadeye Navigator+Peregrine Drake"] = "Pair Deadeye with Drake; blink Drake → untaps 5 lands, only costs 2 to blink → infinite mana.",
        ["Deadeye Navigator+Great Whale"] = "Pair Deadeye with Great Whale; blink to untap 7 lands → infinite mana.",
        ["Deadeye Navigator+Palinchron"] = "Pair Deadeye with Palinchron; blink to untap 7 lands for net mana → infinite mana.",
        ["Palinchron+Phantasmal Image"] = "Copy Palinchron with Phantasmal Image; untap 7 lands → bounce/recast → infinite mana.",
        ["Palinchron+High Tide"] = "With High Tide, Palinchron untaps islands for more than it costs to bounce → infinite mana.",
        ["Nim Deathmantle+Ashnod's Altar"] = "Sacrifice a creature that makes a token (like Grave Titan) → 2 mana from Altar → pay 4 to equip Deathmantle → creature returns → infinite tokens/mana.",
        ["Nim Deathmantle+Krark-Clan Ironworks"] = "Sac token-producer to KCI + Deathmantle loop → infinite mana and tokens.",
        ["Ashnod's Altar+Animation Module"] = "Sacrifice tokens for mana; spend mana to create more tokens via Animation Module → infinite tokens/mana with a counter source.",
        ["Basalt Monolith+Rings of Brighthearth"] = "Copy Basalt Monolith's untap with Rings (2 mana) → taps for 3 → net +1 → infinite colorless mana.",
        ["Basalt Monolith+Power Artifact"] = "Power Artifact reduces untap cost to 1; tap for 3, untap for 1 → infinite colorless mana.",
        ["Basalt Monolith+Mesmeric Orb"] = "Infinite untap triggers with Basalt Monolith → mills everyone via Mesmeric Orb.",
        ["Power Artifact+Grim Monolith"] = "Power Artifact reduces Grim Monolith's untap cost to 2; tap for 3 → infinite colorless mana.",
        ["Worldgorger Dragon+Animate Dead"] = "Animate Dead brings back Worldgorger, which exiles all other permanents including Animate Dead → Dragon dies → loop → infinite mana from lands entering.",
        ["Worldgorger Dragon+Dance of the Dead"] = "Same loop as Animate Dead — infinite mana from lands entering/leaving.",
        ["Worldgorger Dragon+Necromancy"] = "Same loop as Animate Dead variant — infinite ETB triggers and mana.",
    };

    // Describes what the infinite combo specifically produces
    private static readonly Dictionary<string, string> ComboEffects = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Thassa's Oracle+Demonic Consultation"] = "Infinite: Win the game (library exile)",
        ["Thassa's Oracle+Tainted Pact"] = "Infinite: Win the game (library exile)",
        ["Thassa's Oracle+Leveler"] = "Infinite: Win the game (library exile)",
        ["Thassa's Oracle+Paradigm Shift"] = "Infinite: Win the game (library exile)",
        ["Demonic Consultation+Jace, Wielder of Mysteries"] = "Infinite: Win the game (empty library draw)",
        ["Demonic Consultation+Laboratory Maniac"] = "Infinite: Win the game (empty library draw)",
        ["Tainted Pact+Jace, Wielder of Mysteries"] = "Infinite: Win the game (empty library draw)",
        ["Tainted Pact+Laboratory Maniac"] = "Infinite: Win the game (empty library draw)",
        ["Exquisite Blood+Sanguine Bond"] = "Infinite: Damage / Life drain",
        ["Exquisite Blood+Vito, Thorn of the Dusk Rose"] = "Infinite: Damage / Life drain",
        ["Exquisite Blood+Marauding Blight-Priest"] = "Infinite: Damage / Life drain",
        ["Dramatic Reversal+Isochron Scepter"] = "Infinite: Mana (with 3+ mana from nonlands)",
        ["Kiki-Jiki, Mirror Breaker+Zealous Conscripts"] = "Infinite: Creature tokens (hasty)",
        ["Kiki-Jiki, Mirror Breaker+Pestermite"] = "Infinite: Creature tokens (hasty, flying)",
        ["Kiki-Jiki, Mirror Breaker+Deceiver Exarch"] = "Infinite: Creature tokens (hasty)",
        ["Kiki-Jiki, Mirror Breaker+Combat Celebrant"] = "Infinite: Combat phases",
        ["Kiki-Jiki, Mirror Breaker+Felidar Guardian"] = "Infinite: Creature tokens (hasty)",
        ["Splinter Twin+Pestermite"] = "Infinite: Creature tokens (hasty)",
        ["Splinter Twin+Deceiver Exarch"] = "Infinite: Creature tokens (hasty)",
        ["Heliod, Sun-Crowned+Walking Ballista"] = "Infinite: Damage",
        ["Heliod, Sun-Crowned+Triskelion"] = "Infinite: Damage",
        ["Mikaeus, the Unhallowed+Walking Ballista"] = "Infinite: Damage",
        ["Mikaeus, the Unhallowed+Triskelion"] = "Infinite: Damage",
        ["Devoted Druid+Vizier of Remedies"] = "Infinite: Green mana",
        ["Devoted Druid+Swift Reconfiguration"] = "Infinite: Green mana",
        ["Food Chain+Misthollow Griffin"] = "Infinite: Creature mana",
        ["Food Chain+Eternal Scourge"] = "Infinite: Creature mana",
        ["Food Chain+Squee, the Immortal"] = "Infinite: Creature mana",
        ["Underworld Breach+Lion's Eye Diamond"] = "Infinite: Mana (any color)",
        ["Underworld Breach+Brain Freeze"] = "Infinite: Mill (storm)",
        ["Lion's Eye Diamond+Auriok Salvagers"] = "Infinite: Mana (any color)",
        ["Deadeye Navigator+Peregrine Drake"] = "Infinite: Mana",
        ["Deadeye Navigator+Great Whale"] = "Infinite: Mana",
        ["Deadeye Navigator+Palinchron"] = "Infinite: Mana",
        ["Palinchron+Phantasmal Image"] = "Infinite: Mana",
        ["Palinchron+High Tide"] = "Infinite: Mana",
        ["Nim Deathmantle+Ashnod's Altar"] = "Infinite: Tokens / Mana (with token-maker)",
        ["Nim Deathmantle+Krark-Clan Ironworks"] = "Infinite: Tokens / Mana",
        ["Ashnod's Altar+Animation Module"] = "Infinite: Tokens / Mana",
        ["Basalt Monolith+Rings of Brighthearth"] = "Infinite: Colorless mana",
        ["Basalt Monolith+Power Artifact"] = "Infinite: Colorless mana",
        ["Basalt Monolith+Mesmeric Orb"] = "Infinite: Mill (self or opponents)",
        ["Power Artifact+Grim Monolith"] = "Infinite: Colorless mana",
        ["Worldgorger Dragon+Animate Dead"] = "Infinite: Mana / ETB triggers",
        ["Worldgorger Dragon+Dance of the Dead"] = "Infinite: Mana / ETB triggers",
        ["Worldgorger Dragon+Necromancy"] = "Infinite: Mana / ETB triggers",
    };

    // Known 3-card infinite combos: (card1, card2, card3) → description
    private static readonly List<(string[] Cards, string Description, string Effect)> ThreeCardCombos =
    [
        // Nim Deathmantle loops
        (["Nim Deathmantle", "Ashnod's Altar", "Grave Titan"],
         "Sacrifice Grave Titan to Ashnod's Altar (2 mana), sacrifice both Zombie tokens (4 mana total). Pay 4 to equip Nim Deathmantle → Grave Titan returns with 2 new Zombies. Repeat for infinite tokens and mana.",
         "Infinite: Creature tokens / Mana"),
        (["Nim Deathmantle", "Ashnod's Altar", "Wurmcoil Engine"],
         "Sacrifice Wurmcoil Engine to Altar (2 mana), sacrifice both tokens (4 total). Pay 4 for Deathmantle → Wurmcoil returns. Repeat for infinite tokens and life.",
         "Infinite: Creature tokens / Life"),
        (["Nim Deathmantle", "Ashnod's Altar", "Siege-Gang Commander"],
         "Sacrifice Siege-Gang + Goblins to Altar for mana, pay 4 for Deathmantle → Commander returns with 3 Goblins. Repeat for infinite tokens/mana.",
         "Infinite: Creature tokens / Mana"),
        (["Nim Deathmantle", "Ashnod's Altar", "Marionette Master"],
         "Sacrifice Servo tokens + Marionette Master, each death drains opponents. Deathmantle brings Master back → infinite drain.",
         "Infinite: Damage / Life drain"),
        (["Nim Deathmantle", "Phyrexian Altar", "Grave Titan"],
         "Sacrifice Grave Titan + tokens to Phyrexian Altar for colored mana. Pay 4 for Deathmantle → infinite tokens and colored mana.",
         "Infinite: Creature tokens / Colored mana"),

        // Aristocrats loops
        (["Karmic Guide", "Reveillark", "Ashnod's Altar"],
         "Sacrifice Reveillark to Altar → returns Karmic Guide + another creature. Sacrifice Karmic Guide → dies. Sacrifice creatures for mana, loop with Reveillark's LTB. Infinite ETB/death triggers and mana.",
         "Infinite: ETB / Death triggers / Mana"),
        (["Karmic Guide", "Reveillark", "Goblin Bombardment"],
         "Loop Karmic Guide and Reveillark with a sacrifice outlet — each cycle pings with Bombardment for infinite damage.",
         "Infinite: Damage"),
        (["Karmic Guide", "Reveillark", "Altar of Dementia"],
         "Loop Karmic Guide and Reveillark — each cycle mills opponents via Altar of Dementia for infinite mill.",
         "Infinite: Mill"),
        (["Sun Titan", "Fiend Hunter", "Ashnod's Altar"],
         "Sacrifice Sun Titan to Altar, Fiend Hunter exiles/returns Sun Titan. Titan returns Fiend Hunter. Repeat for infinite mana/ETB.",
         "Infinite: Mana / ETB triggers"),
        (["Leonin Relic-Warder", "Animate Dead", "Goblin Bombardment"],
         "Animate Dead returns Relic-Warder which exiles Animate Dead. Relic-Warder dies, Animate Dead returns, loop. Sacrifice to Bombardment each cycle for infinite damage.",
         "Infinite: Damage"),
        (["Leonin Relic-Warder", "Animate Dead", "Blood Artist"],
         "Same loop as above — Blood Artist drains each time Relic-Warder dies and re-enters. Infinite life drain.",
         "Infinite: Damage / Life drain"),

        // Mana combos
        (["Deadeye Navigator", "Dockside Extortionist", "Capsize"],
         "Blink Dockside for treasures, use mana + Capsize (buyback) to bounce opponents' permanents, then reblink Dockside for more. Infinite mana with 3+ opponent artifacts/enchantments, infinite bounce.",
         "Infinite: Mana / Bounce"),
        (["Temur Sabertooth", "Dockside Extortionist", "Concordant Crossroads"],
         "Return Dockside to hand with Sabertooth (1G), recast for 1R, generate 5+ treasures. Repeat for infinite mana with enough opponent artifacts/enchantments.",
         "Infinite: Mana"),
        (["Peregrine Drake", "Ghostly Flicker", "Archaeomancer"],
         "Cast Ghostly Flicker targeting Peregrine Drake + Archaeomancer. Drake untaps 5 lands, Archaeomancer returns Ghostly Flicker. Net mana each cycle → infinite mana.",
         "Infinite: Mana"),
        (["Palinchron", "Deadeye Navigator", "Blue Sun's Zenith"],
         "Generate infinite mana with Palinchron + Deadeye, then cast Blue Sun's Zenith to deck each opponent.",
         "Infinite: Mana → Draw (wins the game)"),

        // Token combos
        (["Ghave, Guru of Spores", "Ashnod's Altar", "Cathars' Crusade"],
         "Remove counter from Ghave to make Saproling (triggers Crusade, putting +1/+1 on all). Sacrifice Saproling for 2 mana. Spend 1 to make another → net +1 mana and counter each cycle. Infinite tokens/mana.",
         "Infinite: Creature tokens / Mana / +1/+1 counters"),
        (["Ghave, Guru of Spores", "Earthcraft", "Doubling Season"],
         "Tap Saproling via Earthcraft to untap a land. Use mana to remove counter, Doubling Season doubles the token. Net positive → infinite tokens.",
         "Infinite: Creature tokens"),
        (["Slimefoot, the Stowaway", "Ashnod's Altar", "Parallel Lives"],
         "Create Saproling with Slimefoot (3 mana), Parallel Lives doubles it. Sacrifice both to Altar (4 mana). Net +1 mana, Slimefoot drains. Repeat → infinite drain.",
         "Infinite: Damage / Life drain"),

        // Storm / Spell combos
        (["Underworld Breach", "Lion's Eye Diamond", "Brain Freeze"],
         "Cast LED from graveyard via Breach (escape), crack for 3 mana. Recast LED via escape, repeat to increase storm count. Cast Brain Freeze with massive storm to mill all opponents.",
         "Infinite: Mill (storm)"),
        (["Isochron Scepter", "Dramatic Reversal", "Aetherflux Reservoir"],
         "Generate infinite mana and storm with Scepter + Reversal. Each cast triggers Aetherflux Reservoir for infinite life → pay 50 to kill each opponent.",
         "Infinite: Life → Damage (50 per activation)"),

        // Persist / Undying combos
        (["Murderous Redcap", "Metallic Mimic", "Viscera Seer"],
         "Mimic set to Goblin. Sacrifice Redcap to Seer, persist returns it, Mimic cancels -1/-1 counter. Redcap ETB pings. Repeat → infinite damage.",
         "Infinite: Damage"),
        (["Murderous Redcap", "Vizier of Remedies", "Goblin Bombardment"],
         "Sacrifice Redcap to Bombardment (1 damage). Persist returns it, Vizier prevents the -1/-1 counter. ETB pings again. Repeat → infinite damage.",
         "Infinite: Damage"),
        (["Mikaeus, the Unhallowed", "Puppeteer Clique", "Altar of Dementia"],
         "Sacrifice Puppeteer Clique to Altar (mills), undying returns it. Clique gets an opponent's creature. Sacrifice the stolen creature. Remove counter via Mikaeus interaction → infinite mill.",
         "Infinite: Mill / ETB triggers"),

        // Enchantment combos
        (["Sanguine Bond", "Vizkopa Guildmage", "Children of Korlis"],
         "Activate Vizkopa Guildmage's second ability. Sacrifice Children of Korlis to regain life lost this turn. Each life gained triggers a life loss, which Children then regains → infinite drain.",
         "Infinite: Damage / Life drain"),
        (["Rest in Peace", "Helm of Obedience", "Any opponent"],
         "With Rest in Peace out, activate Helm targeting an opponent. Cards are exiled instead of going to graveyard, so Helm never stops → exiles entire library.",
         "Infinite: Exile opponent's library"),

        // Artifact combos
        (["Sword of the Meek", "Thopter Foundry", "Ashnod's Altar"],
         "Sacrifice Sword to Foundry (1 mana) → make a 1/1 Thopter + gain 1 life. Sword returns from graveyard. Sacrifice Thopter to Altar (2 mana). Net +1 mana per cycle → infinite tokens/life/mana.",
         "Infinite: Creature tokens / Life / Mana"),
        (["Clock of Omens", "Myr Turbine", "Myr Galvanizer"],
         "Tap Turbine for a Myr token. Tap 2 Myr to untap Turbine via Clock. Use Galvanizer to untap all Myr → infinite Myr tokens.",
         "Infinite: Creature tokens"),

        // Mill combos
        (["Painter's Servant", "Grindstone", "Any opponent"],
         "Name a color with Painter's Servant, making all cards that color. Activate Grindstone → mills 2, they share a color, repeat → mills entire library.",
         "Infinite: Mill"),
    ];

    // Pre-built set of ALL card names involved in any combo (both 2-card and 3-card)
    // Placed after ThreeCardCombos to ensure correct static initialization order
    private static readonly HashSet<string> AllComboCardNames = BuildAllComboNames();

    private static HashSet<string> BuildAllComboNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in ComboPairs)
        {
            names.Add(kvp.Key);
            foreach (var partner in kvp.Value)
                names.Add(partner);
        }
        foreach (var combo in ThreeCardCombos)
        {
            foreach (var card in combo.Cards)
            {
                if (!card.StartsWith("Any", StringComparison.OrdinalIgnoreCase))
                    names.Add(card);
            }
        }
        return names;
    }
}

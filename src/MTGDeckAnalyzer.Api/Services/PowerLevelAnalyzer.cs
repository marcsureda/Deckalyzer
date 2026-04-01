using System.Text.RegularExpressions;
using MTGDeckAnalyzer.Api.Models;

namespace MTGDeckAnalyzer.Api.Services;

public partial class PowerLevelAnalyzer
{
    // Dynamic game changer names fetched from Scryfall at runtime
    private HashSet<string> _dynamicGameChangers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sets the dynamic game changer list fetched from Scryfall's is:gamechanger search.
    /// This supplements (and takes priority over) the static fallback list.
    /// </summary>
    public void SetDynamicGameChangers(HashSet<string> names)
    {
        _dynamicGameChangers = names;
    }

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

    public CardInfo AnalyzeCard(ScryfallCard scryfall, bool isCommander)
    {
        var card = new CardInfo
        {
            Name = scryfall.Name,
            Cmc = scryfall.Cmc,
            ManaCost = scryfall.ManaCost ?? scryfall.CardFaces?.FirstOrDefault()?.ManaCost ?? "",
            TypeLine = scryfall.TypeLine,
            OracleText = scryfall.OracleText ?? scryfall.CardFaces?.FirstOrDefault()?.OracleText ?? "",
            ColorIdentity = scryfall.ColorIdentity,
            Colors = scryfall.Colors ?? [],
            Keywords = scryfall.Keywords,
            Rarity = scryfall.Rarity,
            EdhrecRank = scryfall.EdhrecRank ?? 20000,
            IsCommander = isCommander,
            ScryfallUri = scryfall.ScryfallUri,
        };

        // Price
        if (scryfall.Prices?.Usd != null && double.TryParse(scryfall.Prices.Usd, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
            card.Price = price;
        if (scryfall.Prices?.Eur != null && double.TryParse(scryfall.Prices.Eur, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var priceEur))
            card.PriceEur = priceEur;

        // Image
        card.ImageUri = scryfall.ImageUris?.Normal
            ?? scryfall.CardFaces?.FirstOrDefault()?.ImageUris?.Normal
            ?? "";

        // Type classification
        var typeLine = card.TypeLine.ToLowerInvariant();
        card.IsLand = typeLine.Contains("land");
        card.IsCreature = typeLine.Contains("creature");
        card.IsArtifact = typeLine.Contains("artifact");
        card.IsEnchantment = typeLine.Contains("enchantment");
        card.IsInstant = typeLine.Contains("instant");
        card.IsSorcery = typeLine.Contains("sorcery");
        card.IsPlaneswalker = typeLine.Contains("planeswalker");

        var oracleText = card.OracleText.ToLowerInvariant();

        // Functional classification
        card.IsTutor = TutorCards.Contains(card.Name) || ClassifyAsTutor(oracleText);
        card.IsExtraTurn = ExtraTurnCards.Contains(card.Name) || oracleText.Contains("extra turn");
        card.IsMassLandDenial = MassLandDenialCards.Contains(card.Name) || ClassifyAsMLD(oracleText);
        // Use Scryfall's official game_changer field, then dynamic list, then fallback
        card.IsGameChanger = scryfall.GameChanger || IsGameChanger(card.Name);
        card.IsFastMana = FastManaCards.Contains(card.Name);
        card.IsInfiniteComboPiece = AllComboCardNames.Contains(card.Name);
        card.IsCounterspell = ClassifyAsCounterspell(oracleText, typeLine);
        card.IsBoardWipe = ClassifyAsBoardWipe(oracleText);
        card.IsRemoval = ClassifyAsRemoval(oracleText, typeLine);
        card.IsCardDraw = ClassifyAsCardDraw(oracleText);
        card.IsRamp = ClassifyAsRamp(oracleText, typeLine, card);

        // Calculate scores
        card.Playability = CalculatePlayability(card);
        card.Impact = CalculateImpact(card, scryfall.Reserved);
        card.PowerScore = CalculatePowerScore(card);

        return card;
    }

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

    public DeckAnalysisResult AnalyzeDeck(List<CardInfo> cards)
    {
        var result = new DeckAnalysisResult
        {
            TotalCards = cards.Count,
            Cards = cards.OrderByDescending(c => c.Impact).ToList()
        };

        // Commander info (supports partner/companion — multiple commanders)
        var commanderCards = cards.Where(c => c.IsCommander).ToList();
        if (commanderCards.Count > 0)
        {
            // Primary commander name + image for backwards compat
            result.CommanderName = string.Join(" + ", commanderCards.Select(c => c.Name));
            result.CommanderImageUri = commanderCards[0].ImageUri;

            // Full lists for partner display
            result.CommanderNames = commanderCards.Select(c => c.Name).ToList();
            result.CommanderImageUris = commanderCards.Select(c => c.ImageUri).ToList();

            // Merge color identities from all commanders
            result.ColorIdentity = commanderCards
                .SelectMany(c => c.ColorIdentity)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Composition
        result.Composition = new DeckComposition
        {
            Creatures = cards.Count(c => c.IsCreature && !c.IsLand),
            Instants = cards.Count(c => c.IsInstant),
            Sorceries = cards.Count(c => c.IsSorcery),
            Artifacts = cards.Count(c => c.IsArtifact && !c.IsCreature),
            Enchantments = cards.Count(c => c.IsEnchantment && !c.IsCreature),
            Planeswalkers = cards.Count(c => c.IsPlaneswalker),
            Lands = cards.Count(c => c.IsLand),
            Ramp = cards.Count(c => c.IsRamp),
            CardDraw = cards.Count(c => c.IsCardDraw),
            Removal = cards.Count(c => c.IsRemoval),
            BoardWipes = cards.Count(c => c.IsBoardWipe),
            Counterspells = cards.Count(c => c.IsCounterspell),
            Tutors = cards.Count(c => c.IsTutor),
        };
        result.Composition.Other = result.TotalCards -
            result.Composition.Creatures - result.Composition.Instants -
            result.Composition.Sorceries - result.Composition.Artifacts -
            result.Composition.Enchantments - result.Composition.Planeswalkers -
            result.Composition.Lands;
        if (result.Composition.Other < 0) result.Composition.Other = 0;

        // Mana Analysis
        AnalyzeMana(cards, result);

        // Bracket Analysis (initial — will be adjusted after power calculation)
        AnalyzeBracket(cards, result);

        // Power Metrics
        CalculatePowerMetrics(cards, result);

        // Adjust bracket based on power level (bracket 1 can't have power 7)
        AdjustBracketForPower(result);

        // Synergy with commander
        CalculateSynergies(cards);

        // Strategy / archetype detection
        AnalyzeStrategy(cards, result);

        // Token analysis
        AnalyzeTokens(cards, result);

        // Recommendations
        GenerateRecommendations(cards, result);

        // Strengths and Weaknesses
        AnalyzeStrengthsWeaknesses(result);

        return result;
    }

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

    /// <summary>
    /// Calculate synergy between each card and the commander.
    /// Considers: keyword overlap, type/tribal synergy, oracle text theme match,
    /// color affinity, and functional role relevance.
    /// </summary>
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

    [GeneratedRegex(@"draw \w+ cards")]
    private static partial Regex DrawXCardsRegex();

    [GeneratedRegex(@"create[^.]*?(\d+/\d+)\s+([^.]*?)tokens?", RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();
}

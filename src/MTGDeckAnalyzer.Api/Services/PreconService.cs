using MTGDeckAnalyzer.Api.Models;

namespace MTGDeckAnalyzer.Api.Services;

public interface IPreconService
{
    Task<PreconSearchResult> SearchPreconsAsync(string? query = null, string? year = null, string[]? colors = null, int page = 1, int pageSize = 20);
    Task<PreconDeck?> GetPreconByNameAsync(string name);
}

public class PreconService : IPreconService
{
    private static readonly List<PreconDeck> _precons = new()
    {
        // Commander 2011
        new PreconDeck
        {
            Name = "Heavenly Inferno",
            Commanders = ["Kaalia of the Vast"],
            Year = "2011",
            ColorIdentity = ["W", "B", "R"],
            Theme = "Angels, Demons, Dragons",
            ImageUrl = "https://cards.scryfall.io/art_crop/front/a/0/a0cc9eaf-c8d9-4da2-8fd8-8d423a02a3a8.jpg?1599708091",
            DeckList = "1 Kaalia of the Vast\n1 Command Tower\n1 Nomad Outpost\n1 Boros Garrison\n1 Orzhov Basilica\n1 Rakdos Carnarium\n1 Temple of the False God\n1 Bojuka Bog\n1 Reliquary Tower\n1 Barren Moor\n1 Forgotten Cave\n1 Secluded Steppe\n1 Evolving Wilds\n1 Terramorphic Expanse\n8 Plains\n8 Swamp\n8 Mountain\n1 Sol Ring\n1 Boros Signet\n1 Orzhov Signet\n1 Rakdos Signet\n1 Lightning Greaves\n1 Swiftfoot Boots\n1 Angel of Despair\n1 Akroma, Angel of Wrath\n1 Baneslayer Angel\n1 Reya Dawnbringer\n1 Serra Angel\n1 Tariel, Reckoner of Souls\n1 Demon of Death's Gate\n1 Lord of the Pit\n1 Rakdos the Defiler\n1 Bloodgift Demon\n1 Avatar of Woe\n1 Balefire Dragon\n1 Dragon Hatchling\n1 Hammerfist Giant\n1 Oni of Wild Places\n1 Victory's Herald\n1 Twilight Shepherd\n1 Marshal's Anthem\n1 Wrath of God\n1 Return to Dust\n1 Swords to Plowshares\n1 Path to Exile\n1 Lightning Bolt\n1 Terminate\n1 Mortify\n1 Vindicate\n1 Read the Bones\n1 Sign in Blood\n1 Diabolic Tutor\n1 Cultivate\n1 Kodama's Reach\n1 Rampant Growth\n1 Skullclamp\n1 Mind Stone\n1 Commander's Sphere\n1 Darksteel Ingot\n1 Worn Powerstone"
        },

        new PreconDeck
        {
            Name = "Mirror Mastery",
            Commanders = ["Riku of Two Reflections"],
            Year = "2011",
            ColorIdentity = ["U", "R", "G"],
            Theme = "Copy Spells",
            ImageUrl = "https://cards.scryfall.io/art_crop/front/7/0/70dd138f-391a-4956-bc2a-fe186429c71a.jpg?1599708286",
            DeckList = "1 Riku of Two Reflections\n1 Command Tower\n1 Frontier Bivouac\n1 Gruul Turf\n1 Izzet Boilerworks\n1 Simic Growth Chamber\n1 Temple of the False God\n1 Reliquary Tower\n1 Lonely Sandbar\n1 Forgotten Cave\n1 Tranquil Thicket\n1 Evolving Wilds\n1 Terramorphic Expanse\n7 Island\n7 Mountain\n7 Forest\n1 Sol Ring\n1 Gruul Signet\n1 Izzet Signet\n1 Simic Signet\n1 Lightning Greaves\n1 Swiftfoot Boots\n1 Animar, Soul of Elements\n1 Edric, Spymaster of Trest\n1 Body Double\n1 Chameleon Colossus\n1 Clone\n1 Dualcaster Mage\n1 Eternal Witness\n1 Farhaven Elf\n1 Flametongue Kavu\n1 Sakura-Tribe Elder\n1 Wood Elves\n1 Acidic Slime\n1 Mulldrifter\n1 Prime Speaker Zegana\n1 Avenger of Zendikar\n1 Primeval Titan\n1 Inferno Titan\n1 Crystal Shard\n1 Hull Breach\n1 Lightning Bolt\n1 Fork\n1 Twincast\n1 Rite of Replication\n1 Cultivate\n1 Kodama's Reach\n1 Rampant Growth\n1 Explosive Vegetation\n1 Harmonize\n1 Divination\n1 Fact or Fiction\n1 Skullclamp\n1 Mind Stone\n1 Commander's Sphere\n1 Darksteel Ingot\n1 Sylvan Library"
        },

        new PreconDeck
        {
            Name = "Counterpunch",
            Commanders = ["Karador, Ghost Chieftain"],
            Year = "2011",
            ColorIdentity = ["W", "B", "G"],
            Theme = "Graveyard",
            ImageUrl = "https://cards.scryfall.io/art_crop/front/c/7/c7eb0144-e3de-4662-8c3e-6b6b74c89e8c.jpg?1592714128",
            DeckList = "1 Karador, Ghost Chieftain\n1 Command Tower\n1 Sandsteppe Citadel\n1 Orzhov Basilica\n1 Golgari Rot Farm\n1 Selesnya Sanctuary\n1 Temple of the False God\n1 Bojuka Bog\n1 Reliquary Tower\n1 Barren Moor\n1 Secluded Steppe\n1 Tranquil Thicket\n1 Evolving Wilds\n1 Terramorphic Expanse\n7 Plains\n7 Swamp\n7 Forest\n1 Sol Ring\n1 Orzhov Signet\n1 Golgari Signet\n1 Selesnya Signet\n1 Lightning Greaves\n1 Swiftfoot Boots\n1 Eternal Witness\n1 Genesis\n1 Karmic Guide\n1 Reveillark\n1 Sun Titan\n1 Grave Titan\n1 Woodfall Primus\n1 Terastodon\n1 Yosei, the Morning Star\n1 Kokusho, the Evening Star\n1 Keiga, the Tide Star\n1 Ryusei, the Falling Star\n1 Jugan, the Rising Star\n1 Duplicant\n1 Solemn Simulacrum\n1 Acidic Slime\n1 Reclamation Sage\n1 Sakura-Tribe Elder\n1 Wood Elves\n1 Farhaven Elf\n1 Burnished Hart\n1 Pilgrim's Eye\n1 Fiend Hunter\n1 Banisher Priest\n1 Angel of Serenity\n1 Primeval Titan\n1 Avenger of Zendikar\n1 Wrath of God\n1 Return to Dust\n1 Swords to Plowshares\n1 Path to Exile\n1 Beast Within\n1 Krosan Grip\n1 Putrefy\n1 Mortify\n1 Vindicate\n1 Cultivate\n1 Kodama's Reach\n1 Rampant Growth\n1 Explosive Vegetation\n1 Harmonize\n1 Read the Bones\n1 Sign in Blood\n1 Diabolic Tutor\n1 Skullclamp\n1 Mind Stone\n1 Commander's Sphere\n1 Darksteel Ingot\n1 Worn Powerstone"
        },

        new PreconDeck
        {
            Name = "Political Puppets",
            Commanders = ["Zedruu the Greathearted"],
            Year = "2011",
            ColorIdentity = ["W", "U", "R"],
            Theme = "Group Hug",
            ImageUrl = "https://cards.scryfall.io/art_crop/front/a/b/ab696ac4-3c64-435f-96a5-7ac781c32c77.jpg?1562614856",
            DeckList = "1 Zedruu the Greathearted\n1 Command Tower\n1 Mystic Monastery\n1 Azorius Chancery\n1 Boros Garrison\n1 Izzet Boilerworks\n1 Temple of the False God\n1 Reliquary Tower\n1 Lonely Sandbar\n1 Forgotten Cave\n1 Secluded Steppe\n1 Evolving Wilds\n1 Terramorphic Expanse\n7 Plains\n7 Island\n7 Mountain\n1 Sol Ring\n1 Azorius Signet\n1 Boros Signet\n1 Izzet Signet\n1 Lightning Greaves\n1 Swiftfoot Boots\n1 Sun Titan\n1 Inferno Titan\n1 Consecrated Sphinx\n1 Sphinx of Uthuun\n1 Mulldrifter\n1 Solemn Simulacrum\n1 Burnished Hart\n1 Pilgrim's Eye\n1 Steel Hellkite\n1 Duplicant\n1 Meteor Golem\n1 Wurmcoil Engine\n1 Mindslaver\n1 Akroan Horse\n1 Grid Monitor\n1 Statecraft\n1 Curse of Inertia\n1 Jinxed Choker\n1 Pendant of Prosperity\n1 Aggressive Mining\n1 Jinxed Ring\n1 Custody Battle\n1 Wrath of God\n1 Return to Dust\n1 Swords to Plowshares\n1 Path to Exile\n1 Counterspell\n1 Negate\n1 Swan Song\n1 Lightning Bolt\n1 Shock\n1 Chaos Warp\n1 Brainstorm\n1 Ponder\n1 Preordain\n1 Divination\n1 Fact or Fiction\n1 Deep Analysis\n1 Treasure Cruise\n1 Cultivate\n1 Kodama's Reach\n1 Rampant Growth\n1 Skullclamp\n1 Mind Stone\n1 Commander's Sphere\n1 Darksteel Ingot\n1 Worn Powerstone"
        },

        new PreconDeck
        {
            Name = "Devour for Power",
            Commanders = ["The Mimeoplasm"],
            Year = "2011",
            ColorIdentity = ["U", "B", "G"],
            Theme = "Graveyard",
            ImageUrl = "https://cards.scryfall.io/art_crop/front/a/e/aedc7a6b-c481-4719-bc83-424eeb216aef.jpg?1562275521",
            DeckList = "1 The Mimeoplasm\n1 Command Tower\n1 Opulent Palace\n1 Dimir Aqueduct\n1 Golgari Rot Farm\n1 Simic Growth Chamber\n1 Temple of the False God\n1 Bojuka Bog\n1 Reliquary Tower\n1 Barren Moor\n1 Lonely Sandbar\n1 Tranquil Thicket\n1 Evolving Wilds\n1 Terramorphic Expanse\n7 Swamp\n7 Island\n7 Forest\n1 Sol Ring\n1 Dimir Signet\n1 Golgari Signet\n1 Simic Signet\n1 Lightning Greaves\n1 Swiftfoot Boots\n1 Eternal Witness\n1 Mulldrifter\n1 Prime Speaker Zegana\n1 Consecrated Sphinx\n1 Sphinx of Uthuun\n1 Grave Titan\n1 Primeval Titan\n1 Avenger of Zendikar\n1 Craterhoof Behemoth\n1 Terastodon\n1 Woodfall Primus\n1 Body Double\n1 Clone\n1 Phyrexian Metamorph\n1 Solemn Simulacrum\n1 Acidic Slime\n1 Reclamation Sage\n1 Sakura-Tribe Elder\n1 Wood Elves\n1 Farhaven Elf\n1 Burnished Hart\n1 Pilgrim's Eye\n1 Duplicant\n1 Steel Hellkite\n1 Meteor Golem\n1 Counterspell\n1 Negate\n1 Swan Song\n1 Mystic Snake\n1 Beast Within\n1 Krosan Grip\n1 Putrefy\n1 Murder\n1 Go for the Throat\n1 Hero's Downfall\n1 Cultivate\n1 Kodama's Reach\n1 Rampant Growth\n1 Explosive Vegetation\n1 Harmonize\n1 Divination\n1 Read the Bones\n1 Sign in Blood\n1 Fact or Fiction\n1 Deep Analysis\n1 Treasure Cruise\n1 Skullclamp\n1 Mind Stone\n1 Commander's Sphere\n1 Darksteel Ingot\n1 Worn Powerstone"
        },

        // Commander 2017
        new PreconDeck
        {
            Name = "Vampiric Bloodline",
            Commanders = ["Edgar Markov"],
            Year = "2017",
            ColorIdentity = ["W", "B", "R"],
            Theme = "Vampires",
            ImageUrl = "https://cards.scryfall.io/art_crop/front/8/d/8d94b8ec-ecda-43c8-a60e-1ba33e6a54a4.jpg?1562616128",
            DeckList = "1 Edgar Markov\n1 Command Tower\n1 Nomad Outpost\n1 Boros Garrison\n1 Orzhov Basilica\n1 Rakdos Carnarium\n1 Temple of the False God\n1 Bojuka Bog\n1 Reliquary Tower\n1 Barren Moor\n1 Forgotten Cave\n1 Secluded Steppe\n1 Evolving Wilds\n1 Terramorphic Expanse\n8 Plains\n8 Swamp\n8 Mountain\n1 Sol Ring\n1 Boros Signet\n1 Orzhov Signet\n1 Rakdos Signet\n1 Lightning Greaves\n1 Swiftfoot Boots\n1 Bloodlord of Vaasgoth\n1 Drana, Liberator of Malakir\n1 Vampire Nighthawk\n1 Stromkirk Noble\n1 Captivating Vampire\n1 Olivia Voldaren\n1 Anowon, the Ruin Sage\n1 Bloodghast\n1 Falkenrath Noble\n1 Kalastria Highborn\n1 Malakir Bloodwitch\n1 Vampire Hexmage\n1 Necropolis Regent\n1 Olivia, Mobilized for War\n1 Sanctum Seeker\n1 Vona, Butcher of Magan\n1 Patron of the Vein\n1 Bloodline Necromancer\n1 Mathas, Fiend Seeker\n1 Licia, Sanguine Tribune\n1 Return to Dust\n1 Swords to Plowshares\n1 Path to Exile\n1 Lightning Bolt\n1 Terminate\n1 Mortify\n1 Vindicate\n1 Read the Bones\n1 Sign in Blood\n1 Diabolic Tutor\n1 Cultivate\n1 Kodama's Reach\n1 Rampant Growth\n1 Skullclamp\n1 Mind Stone\n1 Commander's Sphere\n1 Darksteel Ingot\n1 Worn Powerstone\n1 New Blood\n1 Feast of Blood\n1 Urge to Feed\n1 Blade of the Bloodchief\n1 Door of Destinies\n1 Coat of Arms\n1 Shared Animosity"
        },

        // Commander 2019
        new PreconDeck
        {
            Name = "Merciless Rage",
            Commanders = ["Anje Falkenrath"],
            Year = "2019",
            ColorIdentity = ["B", "R"],
            Theme = "Madness",
            ImageUrl = "https://cards.scryfall.io/art_crop/front/9/1/913dd06f-ed2f-4128-9c9d-9cd0d8a55425.jpg?1568003632",
            DeckList = "1 Anje Falkenrath\n1 Command Tower\n1 Rakdos Guildgate\n1 Temple of Malice\n1 Rakdos Carnarium\n1 Temple of the False God\n1 Bojuka Bog\n1 Reliquary Tower\n1 Barren Moor\n1 Forgotten Cave\n1 Evolving Wilds\n1 Terramorphic Expanse\n12 Swamp\n12 Mountain\n1 Sol Ring\n1 Rakdos Signet\n1 Lightning Greaves\n1 Swiftfoot Boots\n1 Chainer, Nightmare Adept\n1 K'rrik, Son of Yawgmoth\n1 Geth, Lord of the Vault\n1 Archfiend of Spite\n1 Bloodhall Priest\n1 Gorgon Recluse\n1 Neheb, Dreadhorde Champion\n1 Big Game Hunter\n1 Bone Miser\n1 Dark Withering\n1 Fiery Temper\n1 From Under the Floorboards\n1 Grave Scrabbler\n1 Malevolent Whispers\n1 Nightshade Assassin\n1 Reckless Wurm\n1 Twins of Maurer Estate\n1 Violent Eruption\n1 Basking Rootwalla\n1 Bloodmad Vampire\n1 Call to the Netherworld\n1 Circular Logic\n1 Deep Analysis\n1 Gamble\n1 Lightning Axe\n1 Murderous Compulsion\n1 Obsessive Search\n1 Prescription for Death\n1 Putrid Imp\n1 Turnabout\n1 Wild Mongrel\n1 Wonder\n1 Faithless Looting\n1 Careful Study\n1 Frantic Search\n1 Breakthrough\n1 Skirsdag High Priest\n1 Mindwrack Demon\n1 Avatar of Woe\n1 Zombie Infestation\n1 Squee, Goblin Nabob\n1 Library of Leng\n1 Bag of Holding\n1 Skirge Familiar"
        },

        // Commander 2020
        new PreconDeck
        {
            Name = "Arcane Maelstrom",
            Commanders = ["Kalamax, the Stormsire"],
            Year = "2020",
            ColorIdentity = ["U", "R", "G"],
            Theme = "Spells Matter",
            ImageUrl = "https://cards.scryfall.io/art_crop/front/f/9/f990cd78-2165-446f-a116-ae55d7a0f00d.jpg?1568003927",
            DeckList = "1 Kalamax, the Stormsire\n1 Command Tower\n1 Exotic Orchard\n1 Frontier Bivouac\n1 Opal Palace\n1 Temple of the False God\n1 Mosswort Bridge\n1 Bonders' Enclave\n1 Ash Barrens\n1 Blighted Woodland\n1 Myriad Landscape\n1 Gruul Turf\n1 Izzet Boilerworks\n1 Simic Growth Chamber\n1 Evolving Wilds\n1 Terramorphic Expanse\n5 Island\n5 Mountain\n5 Forest\n1 Sol Ring\n1 Arcane Signet\n1 Izzet Signet\n1 Gruul Signet\n1 Simic Signet\n1 Cultivate\n1 Kodama's Reach\n1 Rampant Growth\n1 Farseek\n1 Growth Spiral\n1 Explosive Vegetation\n1 Harrow\n1 Migration Path\n1 Nature's Lore\n1 Skyshroud Claim\n1 Spell Burst\n1 Counterspell\n1 Mystic Snake\n1 Negate\n1 Swan Song\n1 Beast Within\n1 Chaos Warp\n1 Krosan Grip\n1 Putrefy\n1 Hull Breach\n1 Decimate\n1 Acidic Slime\n1 Reclamation Sage\n1 Lightning Bolt\n1 Shock\n1 Wild Ricochet\n1 Twincast\n1 Fork\n1 Reverberate\n1 Increasing Vengeance\n1 Fury Storm\n1 Ral's Outburst\n1 Primal Empathy\n1 Shared Summons\n1 Sawtusk Demolisher\n1 Charmbreaker Devils\n1 Dualcaster Mage\n1 Melek, Izzet Paragon\n1 Riku of Two Reflections\n1 Wort, the Raidmother\n1 Talrand, Sky Summoner\n1 Young Pyromancer\n1 Murmuring Mystic\n1 Rashmi, Eternities Crafter\n1 Edric, Spymaster of Trest\n1 Prime Speaker Zegana\n1 Apex Altisaur\n1 Avenger of Zendikar\n1 Greenwarden of Murasa\n1 Kodama of the East Tree\n1 Verdant Force\n1 Primeval Protector\n1 Titan Hunter\n1 Lightning Greaves\n1 Swiftfoot Boots\n1 Commander's Sphere\n1 Mind Stone\n1 Hedron Archive\n1 Worn Powerstone\n1 Thran Dynamo\n1 Gilded Lotus"
        }
    };

    public Task<PreconSearchResult> SearchPreconsAsync(string? query = null, string? year = null, string[]? colors = null, int page = 1, int pageSize = 20)
    {
        var filtered = _precons.AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            var q = query.ToLower();
            filtered = filtered.Where(p =>
                p.Name.ToLower().Contains(q) ||
                p.Commanders.Any(c => c.ToLower().Contains(q)) ||
                p.Theme.ToLower().Contains(q));
        }

        if (!string.IsNullOrEmpty(year))
        {
            filtered = filtered.Where(p => p.Year == year);
        }

        if (colors != null && colors.Length > 0)
        {
            var colorSet = colors.Select(c => c.ToUpper()).ToHashSet();
            filtered = filtered.Where(p => p.ColorIdentity.Any(ci => colorSet.Contains(ci)));
        }

        var total = filtered.Count();
        var precons = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PreconSearchResult
        {
            Precons = precons,
            TotalCount = total
        });
    }

    public Task<PreconDeck?> GetPreconByNameAsync(string name)
    {
        var precon = _precons.FirstOrDefault(p => 
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(precon);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public class CardPoolArgs
{
    [JsonPropertyName("sacrifice")]
    public int SacrificeCardCount { get; set; }
    [JsonPropertyName("common")]
    public Dictionary<CardBloodCost, int> CommonCardsCounts { get; set; }
    [JsonPropertyName("uncommon")]
    public Dictionary<CardBloodCost, int> UncommonCardsCounts { get; set; }
    [JsonPropertyName("rare")]
    public Dictionary<CardBloodCost, int> RareCardsCounts { get; set; }
}

public static class CardGenerator
{
    private static readonly int MaxCardAbilities = 2;

    private static readonly string ResourceDeckGeneratorDataPath = Path.Combine(Constants.GameSettingsDirectory, "cards.data.json");
    private static readonly string UserDeckGeneratorDataPath = Path.Combine(Constants.UserDataDirectory, "cards.data.json");

    public class GeneratorData
    {
        [JsonPropertyName("starting_deck")]
        public StartingDeckData StartingDeck { get; set; }

        [JsonPropertyName("stats")]
        public StatsData Stats { get; set; }

        [JsonPropertyName("nouns")]
        public Dictionary<string, NounData> Nouns { get; set; }

        [JsonPropertyName("adjectives")]
        public Dictionary<string, AdjectiveData> Adjectives { get; set; }
    }

    public class StartingDeckData
    {
        [JsonPropertyName("starting_deck_size")]
        public int StartingDeckSize { get; set; }
        [JsonPropertyName("starting_hand_size")]
        public int StartingHandSize { get; set; }
        [JsonPropertyName("starting_sacrifice_count")]
        public int StartingSacrificeCount { get; set; }
    }

    public class StatsData
    {
        [JsonPropertyName("pool_size")]
        public CardPoolArgs DefaultCardPoolArgs { get; set; }

        [JsonPropertyName("ability_costs")]
        public Dictionary<string, int> AbilityCosts { get; set; }

        [JsonPropertyName("ability_tooltips")]
        public Dictionary<CardAbilities, AbilityTooltip> AbilityTooltips { get; set; }

        [JsonPropertyName("card_templates")]
        public CardTemplates Templates { get; set; }

        public class AbilityTooltip
        {
            [JsonPropertyName("label")]
            public string Label { get; set; }
            [JsonPropertyName("description")]
            public string Description { get; set; }
        }

        public class CardTemplates
        {
            [JsonPropertyName("sacrifice")]
            public CardTemplate SacrificeTemplate { get; set; }
            [JsonPropertyName("common")]
            public Dictionary<CardBloodCost, CardTemplate[]> CommonTemplates { get; set; }
            [JsonPropertyName("uncommon")]
            public Dictionary<CardBloodCost, CardTemplate[]> UncommonTemplates { get; set; }
            [JsonPropertyName("rare")]
            public Dictionary<CardBloodCost, CardTemplate[]> RareTemplates { get; set; }
        }

        public class CardTemplate
        {
            [JsonPropertyName("prob")]
            public int Probability { get; set; } = 1;

            [JsonPropertyName("attack")]
            public int Attack { get; set; }

            [JsonPropertyName("health")]
            public int Health { get; set; }

            [JsonPropertyName("ability_points")]
            public int AbilityPoints { get; set; } = 0;
        }
    }

    public class NounData
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }
        [JsonPropertyName("avatars")]
        public List<string> AvatarResources { get; set; }
    }

    public class AdjectiveData
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }
    }

    public static CardPool GenerateRandomCardPool(string name, CardPoolArgs args = null)
    {
        GeneratorData data = LoadGeneratorData();

        if (args == null)
        {
            args = data.Stats.DefaultCardPoolArgs;
        }

        var cards = new List<CardInfo>();
        for (int i = 0; i < args.SacrificeCardCount; i++)
        {
            cards.Add(GenerateRandomCard(CardRarity.Sacrifice, CardBloodCost.Zero, data));
        }

        foreach (var cardCost in args.CommonCardsCounts)
        {
            for (int i = 0; i < cardCost.Value; i++)
            {
                cards.Add(GenerateRandomCard(CardRarity.Common, cardCost.Key, data));
            }
        }

        foreach (var cardCost in args.UncommonCardsCounts)
        {
            for (int i = 0; i < cardCost.Value; i++)
            {
                cards.Add(GenerateRandomCard(CardRarity.Uncommon, cardCost.Key, data));
            }
        }

        foreach (var cardCost in args.RareCardsCounts)
        {
            for (int i = 0; i < cardCost.Value; i++)
            {
                cards.Add(GenerateRandomCard(CardRarity.Rare, cardCost.Key, data));
            }
        }

        return new CardPool(cards, name);
    }

    private class StatAction
    {
        public string StatName { get; set; }

        public Func<CardInfo, CardInfo> ApplyStat { get; set; }

        public Func<CardInfo, bool> CanApply { get; set; }

        public bool CanAffort(GeneratorData data, int points)
        {
            if (data.Stats.AbilityCosts.TryGetValue(StatName, out var cost))
            {
                return cost <= points;
            }
            return false;
        }

        public int Cost(GeneratorData data)
        {
            return data.Stats.AbilityCosts[StatName];
        }
    }

    private static List<StatAction> StatActions = new List<StatAction>()
    {
        new StatAction()
        {
            StatName = "flying",
            CanApply = (cardInfo) =>
                cardInfo.Abilities.Count < MaxCardAbilities &&
                !cardInfo.Abilities.Contains(CardAbilities.Flying) &&
                !cardInfo.Abilities.Contains(CardAbilities.Tall), // semi-redundant
            ApplyStat = (cardInfo) => { cardInfo.Abilities.Add(CardAbilities.Flying); return cardInfo; }
        },
        new StatAction()
        {
            StatName = "attack",
            CanApply = (_) => true,
            ApplyStat = (cardInfo) => { cardInfo.Attack += 1; return cardInfo; }
        },
        new StatAction()
        {
            StatName = "tall",
            CanApply = (cardInfo) =>
                cardInfo.Abilities.Count < MaxCardAbilities &&
                !cardInfo.Abilities.Contains(CardAbilities.Tall) &&
                !cardInfo.Abilities.Contains(CardAbilities.Flying), // redundant
            ApplyStat = (cardInfo) => { cardInfo.Abilities.Add(CardAbilities.Tall); return cardInfo; }
        },
        new StatAction()
        {
            StatName = "health",
            CanApply = (_) => true,
            ApplyStat = (cardInfo) => { cardInfo.Health += 1; return cardInfo; }
        },
        new StatAction()
        {
            StatName = "lethal",
            CanApply = (cardInfo) => cardInfo.Abilities.Count < MaxCardAbilities &&
                !cardInfo.Abilities.Contains(CardAbilities.Lethal),
            ApplyStat = (cardInfo) => { cardInfo.Abilities.Add(CardAbilities.Lethal); return cardInfo; }
        },
    };

    public static CardInfo GenerateRandomCard(CardRarity rarity, CardBloodCost cost, GeneratorData data = null)
    {
        if (data == null)
        {
            data = LoadGeneratorData();
        }

        var rnd = new RandomGenerator();

        var nouns = GetNounsForCardLevel(data, cost);
        var noun = rnd.SelectRandom(nouns);

        var adjectives = GetAdjectivesForCardLevel(data, rarity);
        var adjective = rnd.SelectRandom(adjectives);

        var avatar = rnd.SelectRandom(noun.Value.AvatarResources);

        StatsData.CardTemplate[] templates;
        string cardFoilHexcode = null;

        if (rarity == CardRarity.Rare)
        {
            templates = data.Stats.Templates.RareTemplates[cost];
            cardFoilHexcode = new Color(rnd.Nextf(0.25f, 0.75f), rnd.Nextf(0.25f, 0.80f), rnd.Nextf(0.25f, 0.75f)).ToHtml();
        }
        else if (rarity == CardRarity.Uncommon)
        {
            templates = data.Stats.Templates.UncommonTemplates[cost];
        }
        else if (rarity == CardRarity.Common)
        {
            templates = data.Stats.Templates.CommonTemplates[cost];
        }
        else
        {
            templates = new[] { data.Stats.Templates.SacrificeTemplate };
        }

        var template = rnd.SelectRandomOdds(templates, templates.Select(t => t.Probability).ToArray());

        int remainingPoints = template.AbilityPoints;
        var cardInfo = new CardInfo
        {
            NameAdjective = adjective.Key,
            NameNoun = noun.Key,
            AvatarResource = avatar,
            Attack = template.Attack,
            Health = template.Health,
            Abilities = new List<CardAbilities>(),
            BloodCost = cost,
            Rarity = rarity,
            CardFoilHexcode = cardFoilHexcode,
        };

        // TODO: Many different options for improvements
        //		- Filter identical stat lines or "strictly better than" options
        //		- Enumerate all possible stats to find interesting options - instead of the random market approach
        for (int i = 0; i < 1000; i++)
        {
            List<StatAction> possibleActions = StatActions
                .Where((action) => action.CanAffort(data, remainingPoints) && action.CanApply(cardInfo))
                .ToList();

            if (possibleActions.Count == 0)
            {
                return cardInfo;
            }

            var statAction = rnd.SelectRandom(possibleActions);
            cardInfo = statAction.ApplyStat(cardInfo);
            remainingPoints -= statAction.Cost(data);
        }

        cardInfo.Abilities.Sort();
        return cardInfo;
    }

    public static CardPool OverrideCardPool(CardPool cardPool, RandomGenerator rnd, int level, string nounOverride, string adjectiveOverride)
    {
        var data = LoadGeneratorData();

        // Resources use the following levels to pick noun/adjectives:
        //      Noun: 0=ZeroCost; 1=OneCost; 2=TwoCost; 3=ThreeCost;
        //      Adj: 0=Sacrifice; 1=Common; 2=Uncommon&Rare;
        if (nounOverride == "*")
        {
            int nounLevel;
            if (level == 1) nounLevel = 0;
            else if (level <= 4) nounLevel = 1;
            else if (level <= 7) nounLevel = 2;
            else nounLevel = 3;

            nounOverride = rnd.SelectRandom(data.Nouns.Where(n => n.Value.Level == nounLevel).Select(n => n.Key));
        }

        if (adjectiveOverride == "*")
        {
            int adjLevel;
            if (level == 1) adjLevel = 0;
            else if (level <= 5) adjLevel = 1;
            else adjLevel = 2;

            adjectiveOverride = rnd.SelectRandom(data.Adjectives.Where(a => a.Value.Level == adjLevel).Select(a => a.Key));
        }

        var overrideCards = cardPool.Cards.Select(card => 
            OverrideCardFields(
                card,
                data,
                rnd,
                noun: nounOverride,
                adjective: adjectiveOverride));
        
        return new CardPool(overrideCards, $"Override_{cardPool.Name}");
    }

    public static CardInfo OverrideCardFields(CardInfo originalCard, GeneratorData data, RandomGenerator rnd, string noun, string adjective)
    {
        CardInfo card = originalCard; // struct copy by value

        if (!string.IsNullOrEmpty(noun))
        {
            NounData nounData = data.Nouns.FirstOrDefault(n => n.Key == noun).Value;
            if (nounData == null)
            {
                GD.PrintErr($"[OverrideCard] Noun {noun} not found in generator data.");
            }
            else
            {
                card.NameNoun = noun;
                card.AvatarResource = rnd.SelectRandom(nounData.AvatarResources);
            }
        }

        if (!string.IsNullOrEmpty(adjective))
        {
            AdjectiveData adjData = data.Adjectives.FirstOrDefault(n => n.Key == adjective).Value;
            if (adjData == null)
            {
                GD.PrintErr($"[OverrideCard] Adjective {adjective} not found in generator data.");
            }
            else
            {
                card.NameAdjective = adjective;   
            }
        }

        return card;
    }

    public static void ResetCardGeneratorSettings()
    {
        DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
        // Bug Fix: The string "res://" is resolved as a relative path in a packaged game. So it fails during the CopyAbsolute method.
        // DirAccess.CopyAbsolute(TemplateDeckGeneratorDataPath, UserDeckGeneratorDataPath);
        var dir = DirAccess.Open("res://");
        dir.Copy(ResourceDeckGeneratorDataPath, UserDeckGeneratorDataPath);
    }

    private static Dictionary<string, NounData> GetNounsForCardLevel(GeneratorData data, CardBloodCost cost)
    {
        int nounLevel = (int)cost;
        return data.Nouns.Where(n => n.Value.Level == nounLevel).ToDictionary(n => n.Key, n => n.Value);
    }

    private static Dictionary<string, AdjectiveData> GetAdjectivesForCardLevel(GeneratorData data, CardRarity rarity)
    {
        int adjectiveLevel;
        if (rarity == CardRarity.Sacrifice)
        {
            adjectiveLevel = 0;
        }
        else if (rarity == CardRarity.Common)
        {
            adjectiveLevel = 1;
        }
        else
        {
            adjectiveLevel = 2;
        }

        return data.Adjectives.Where(a => a.Value.Level == adjectiveLevel).ToDictionary(a => a.Key, a => a.Value);
    }

    private static GeneratorData _cachedGeneratorData = null;
    public static GeneratorData LoadGeneratorData()
    {
        if (_cachedGeneratorData != null) return _cachedGeneratorData;

        if (OS.IsDebugBuild() || !Godot.FileAccess.FileExists(UserDeckGeneratorDataPath))
        {
            GD.Print("Copying over cards.data.json...");
            ResetCardGeneratorSettings();
        }

        var dataStr = Godot.FileAccess.GetFileAsString(UserDeckGeneratorDataPath);
        var data = JsonSerializer.Deserialize<GeneratorData>(dataStr, new JsonSerializerOptions() { IncludeFields = true });
        GD.Print($"Loaded Deck Generator Data with {data.Nouns?.Count} nouns and {data.Adjectives?.Count} adjectives.");
        
         _cachedGeneratorData = data;
        return data;
    }
}
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
        [JsonPropertyName("stats")]
        public StatsData Stats { get; set; }

        [JsonPropertyName("nouns")]
        public Dictionary<string, NounData> Nouns { get; set; }

        [JsonPropertyName("adjectives")]
        public Dictionary<string, AdjectiveData> Adjectives { get; set; }
    }

    public class StatsData
    {
        [JsonPropertyName("pool_size")]
        public CardPoolArgs DefaultCardPoolArgs { get; set; }

        [JsonPropertyName("ability_costs")]
        public Dictionary<string, int> AbilityCosts { get; set; }

        [JsonPropertyName("card_templates")]
        public CardTemplates Templates { get; set; }

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
            StatName = "agile",
            CanApply = (cardInfo) =>
                cardInfo.Abilities.Count < MaxCardAbilities &&
                !cardInfo.Abilities.Contains(CardAbilities.Agile) &&
                !cardInfo.Abilities.Contains(CardAbilities.Guard), // semi-redundant
            ApplyStat = (cardInfo) => { cardInfo.Abilities.Add(CardAbilities.Agile); return cardInfo; }
        },
        new StatAction()
        {
            StatName = "attack",
            CanApply = (_) => true,
            ApplyStat = (cardInfo) => { cardInfo.Attack += 1; return cardInfo; }
        },
        new StatAction()
        {
            StatName = "guard",
            CanApply = (cardInfo) =>
                cardInfo.Abilities.Count < MaxCardAbilities &&
                !cardInfo.Abilities.Contains(CardAbilities.Guard) &&
                !cardInfo.Abilities.Contains(CardAbilities.Agile), // redundant
            ApplyStat = (cardInfo) => { cardInfo.Abilities.Add(CardAbilities.Guard); return cardInfo; }
        },
        new StatAction()
        {
            StatName = "health",
            CanApply = (_) => true,
            ApplyStat = (cardInfo) => { cardInfo.Health += 1; return cardInfo; }
        },
        new StatAction()
        {
            StatName = "poisoned",
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
        else if (rarity == CardRarity.Common || rarity == CardRarity.Uncommon)
        {
            adjectiveLevel = 1;
        }
        else
        {
            adjectiveLevel = 2;
        }

        return data.Adjectives.Where(a => a.Value.Level == adjectiveLevel).ToDictionary(a => a.Key, a => a.Value);
    }

    private static GeneratorData LoadGeneratorData()
    {
        if (OS.IsDebugBuild() || !Godot.FileAccess.FileExists(UserDeckGeneratorDataPath))
        {
            GD.Print("Copying over cards.data.json...");
            ResetCardGeneratorSettings();
        }

        var dataStr = Godot.FileAccess.GetFileAsString(UserDeckGeneratorDataPath);
        var data = JsonSerializer.Deserialize<GeneratorData>(dataStr, new JsonSerializerOptions() { IncludeFields = true });
        GD.Print($"Loaded Deck Generator Data with {data.Nouns?.Count} nouns and {data.Adjectives?.Count} adjectives.");
        return data;
    }
}
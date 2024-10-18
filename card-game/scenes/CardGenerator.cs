using System;
using System.Collections.Generic;
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
	private static readonly string TemplateDeckGeneratorDataPath = "res://settings/data.json";
	private static readonly string UserDeckGeneratorDataPath = "user://data/data.json";

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

		[JsonPropertyName("ability_point_limits")]
		public AbilityPointLimit AbilityPointLimits { get; set; }

		public class AbilityPointLimit
		{
			[JsonPropertyName("sacrifice")]
			public int SacrificeCardPointLimit { get; set; }
			[JsonPropertyName("common")]
			public Dictionary<CardBloodCost, int> CommonCardsPointLimit { get; set; }
			[JsonPropertyName("uncommon")]
			public Dictionary<CardBloodCost, int> UncommonCardsPointLimit { get; set; }
			[JsonPropertyName("rare")]
			public Dictionary<CardBloodCost, int> RareCardsPointLimit { get; set; }
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

	private static int NextGeneratedCardId = 0;
	public static CardPool GenerateRandomCardPool(string name, CardPoolArgs args = null)
	{
		NextGeneratedCardId = 0;
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
			StatName = "attack",
			CanApply = (_) => true,
			ApplyStat = (cardInfo) => { cardInfo.Attack += 1; return cardInfo; }
		},
		new StatAction()
		{
			StatName = "health",
			CanApply = (_) => true,
			ApplyStat = (cardInfo) => { cardInfo.Health += 1; return cardInfo; }
		},
	};

	public static CardInfo GenerateRandomCard(CardRarity rarity, CardBloodCost cost, GeneratorData data = null)
	{
		if (data == null)
		{
			data = LoadGeneratorData();
		}

		var nouns = GetNounsForCardLevel(data, cost);
		var noun = SelectRandom(nouns);

		var avatar = SelectRandom(noun.Value.AvatarResources);

		var adjectives = GetAdjectivesForCardLevel(data, rarity);
		var adjective = SelectRandom(adjectives);

		var cardInfo = new CardInfo
		{
			Id = NextGeneratedCardId++,
			Name = $"{adjective.Key} {noun.Key}",
			AvatarResource = avatar,
			Attack = 0,
			Health = 1,
			BloodCost = cost,
			Rarity = rarity,
		};

		int remainingPoints = 0;
		var abilityLimits = data.Stats.AbilityPointLimits;
		if (rarity == CardRarity.Sacrifice)
		{
			remainingPoints = abilityLimits.SacrificeCardPointLimit;
		}
		else if (rarity == CardRarity.Common)
		{
			remainingPoints = abilityLimits.CommonCardsPointLimit[cost];
		}
		else if (rarity == CardRarity.Uncommon)
		{
			remainingPoints = abilityLimits.UncommonCardsPointLimit[cost];
		}
		else if (rarity == CardRarity.Rare)
		{
			remainingPoints = abilityLimits.RareCardsPointLimit[cost];
		}

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

			var statAction = SelectRandom(possibleActions);
			cardInfo = statAction.ApplyStat(cardInfo);
			remainingPoints -= statAction.Cost(data);
		}
		return cardInfo;
	}

	public static void ResetCardGeneratorSettings()
	{
		DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
		DirAccess.CopyAbsolute(TemplateDeckGeneratorDataPath, UserDeckGeneratorDataPath);
	}

	private static T SelectRandom<T>(ICollection<T> collection)
	{
		int index = Random.Shared.Next(collection.Count);
		return collection.Skip(index).First();
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
		if (!FileAccess.FileExists(UserDeckGeneratorDataPath))
		{
			ResetCardGeneratorSettings();
		}

		var dataStr = FileAccess.GetFileAsString(UserDeckGeneratorDataPath);
		var data = JsonSerializer.Deserialize<GeneratorData>(dataStr, new JsonSerializerOptions() { IncludeFields = true });
		GD.Print($"Loaded Generator Data with {data.Nouns?.Count} nouns and {data.Adjectives?.Count} adjectives.");
		return data;
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public struct NewCardPoolArgs
{
	public int SacrificeCardCount { get; set; }
	public Dictionary<CardBloodCost, int> CommonCardsCounts { get; set; } 
	public Dictionary<CardBloodCost, int> UncommonCardsCounts { get; set; } 
	public Dictionary<CardBloodCost, int> RareCardsCounts { get; set; } 
} 

public static class CardGenerator
{
	private static readonly string DeckGeneratorDataPath = "res://decks/generator/data.json";

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
		[JsonPropertyName("ability_costs")]
		public Dictionary<string, int> AbilityCosts { get; set; }

		[JsonPropertyName("point_limits")]
		public List<AbilityPointLimit> AbilityPointLimits { get; set; }

		public class AbilityPointLimit
		{
			[JsonPropertyName("cost")]
			public CardBloodCost Cost { get; set; }
			[JsonPropertyName("rarity")]
			public CardRarity Rarity { get; set; }
			[JsonPropertyName("limit")]
			public int Limit { get; set; }
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

	public static readonly NewCardPoolArgs DefaultArgs = new NewCardPoolArgs()
	{
		SacrificeCardCount = 10,
		CommonCardsCounts = new Dictionary<CardBloodCost, int>
		{
			{ CardBloodCost.Zero, 7 },
			{ CardBloodCost.One, 6 },
			{ CardBloodCost.Two, 6 },
			{ CardBloodCost.Three, 5 },
		},
		UncommonCardsCounts = new Dictionary<CardBloodCost, int>
		{
			{ CardBloodCost.Zero, 2 },
			{ CardBloodCost.One, 3 },
			{ CardBloodCost.Two, 3 },
			{ CardBloodCost.Three, 3 },
		},
		RareCardsCounts = new Dictionary<CardBloodCost, int>
		{
			{ CardBloodCost.Zero, 1 },
			{ CardBloodCost.One, 1 },
			{ CardBloodCost.Two, 1 },
			{ CardBloodCost.Three, 2 },
		},
	};

	private static int NextGeneratedCardId = 0;
	public static CardPool GenerateRandomCardPool(NewCardPoolArgs args, string name)
	{
		NextGeneratedCardId = 0;
		GeneratorData data = LoadGeneratorData();

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

		var cardInfo =  new CardInfo {
			Id = NextGeneratedCardId++,
			Name = $"{adjective.Key} {noun.Key}",
			AvatarResource = avatar,
			Attack = 0,
			Health = 1,
			BloodCost = cost,
			Rarity = rarity,
		};

		var abilityPointsLimit = data.Stats.AbilityPointLimits.Where(limit => limit.Cost == cost && limit.Rarity == rarity).FirstOrDefault();
		if (abilityPointsLimit == null)
		{
			GD.PushError($"No stats limit defined for cost: {cost} and rarity: {rarity}!");
			return cardInfo;
		}

		// TODO: Many different options for improvements
		//		- Filter identical stat lines or "strictly better than" options
		//		- Enumerate all possible stats to find interesting options - instead of the random market approach
		int remainingPoints = abilityPointsLimit.Limit;
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
		var dataStr = Godot.FileAccess.GetFileAsString(DeckGeneratorDataPath);
		var data = JsonSerializer.Deserialize<GeneratorData>(dataStr, new JsonSerializerOptions() { IncludeFields = true });
		GD.Print($"Loaded Generator Data with {data.Nouns?.Count} nouns and {data.Adjectives?.Count} adjectives.");
		return data;
	}
}
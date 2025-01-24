using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Godot;

public static class CardPoolAnalyzer
{
    public static void AnalyzeCardPools(int poolsCount)
	{
		var totalCount = new Counter<CardAnalysisKey>();
		var cardCollisionHist = new Counter<int>();
		var adjCollisionHist = new Counter<int>();
		var nounCollisionHist = new Counter<int>();
		var nameCollisionHist = new Counter<int>();
		for (int i = 0; i < poolsCount; i++)
		{
			CardPool cardPool = CardGenerator.GenerateRandomCardPool("AnalysisPool");

			var cardInfoCount = new Counter<CardAnalysisKey>();
			var adjCount = new Counter<string>();
			var nounCount = new Counter<string>();
			var nameCount = new Counter<string>();
			foreach (CardInfo card in cardPool.Cards)
			{
				totalCount.Add(new CardAnalysisKey(card));
				cardInfoCount.Add(new CardAnalysisKey(card));

				var nameParts = card.Name.Split(" ");
				var adj = nameParts[0];
				var noun = nameParts[1]; // this is broken if any adjectives have a space...
				adjCount.Add(adj);
				nounCount.Add(noun);
				nameCount.Add(card.Name);
			}

			// Count up how many card infos or name parts have collisions - and how severe the collision is.
			// Where a collision is defined as a generated card with more than 1 usage in the card pool.
			cardCollisionHist.AddHistogram(cardInfoCount.Result);
			adjCollisionHist.AddHistogram(adjCount.Result);
			nounCollisionHist.AddHistogram(nounCount.Result);
			nameCollisionHist.AddHistogram(nameCount.Result);
		}

		totalCount.WriteCsv(
			"CardPoolAnalysis_TotalCardInfo.csv",
			new List<string> { "Rarity", "BloodCost", "Attack", "Health", "AbilitiesCount", "Abilities" },
			(key) => new List<object> { key.Rarity.ToString(), key.BloodCost.ToString(), key.Attack, key.Health, key.Abilities.Count, key.Abilities.Count > 0 ? string.Join("-", key.Abilities) : CardAbilities.None.ToString() });

		cardCollisionHist.WriteCsv(
			"CardPoolAnalysis_CardInfoHist.csv",
			new List<string> { "Card Stats Collision Count" },
			(key) => new List<object> { key });

		adjCollisionHist.WriteCsv(
			"CardPoolAnalysis_AdjHist.csv",
			new List<string> { "Adjective Collision Count" },
			(key) => new List<object> { key });

		nounCollisionHist.WriteCsv(
			"CardPoolAnalysis_NounHist.csv",
			new List<string> { "Noun Collision Count" },
			(key) => new List<object> { key });

		nameCollisionHist.WriteCsv(
			"CardPoolAnalysis_NameHist.csv",
			new List<string> { "Card Name Collision Count" },
			(key) => new List<object> { key });
	}

    public class CardAnalysisKey : IComparable<CardAnalysisKey>
	{
		public CardRarity Rarity { get; private set; }
		public CardBloodCost BloodCost { get; private set; }
		public List<CardAbilities> Abilities { get; private set; }
		public int Attack { get; private set; }
		public int Health { get; private set; }

		public CardAnalysisKey(CardInfo info)
		{
			Rarity = info.Rarity;
			BloodCost = info.BloodCost;
			Attack = info.Attack;
			Health = info.Health;
			Abilities = info.Abilities.ToList();
			Abilities.Sort();
		}

		public override bool Equals(object obj)
		{
			CardAnalysisKey o = obj as CardAnalysisKey;
			if (o == null)
			{
				return false;
			}

			bool abilitiesMatch = o.Abilities.Count == Abilities.Count;
			if (abilitiesMatch)
			{
				for (int i = 0; i < Abilities.Count; i++)
				{
					if (o.Abilities[i] != Abilities[i])
					{
						abilitiesMatch = false;
					}
				}
			}

			return o.Rarity == Rarity &&
				o.BloodCost == BloodCost &&
				o.Attack == Attack &&
				o.Health == Health &&
				abilitiesMatch;
		}

		public override int GetHashCode()
		{
			string abilitiesStr = string.Join("", Abilities);
			return HashCode.Combine(Rarity, BloodCost, Attack, Health, abilitiesStr);
		}

		public int CompareTo(CardAnalysisKey other)
		{
			if (other == null) return 1;

			int cmp;

			cmp = BloodCost.CompareTo(other.BloodCost);
			if (cmp != 0) return cmp;

			cmp = Rarity.CompareTo(other.Rarity);
			if (cmp != 0) return cmp;

			cmp = Attack.CompareTo(other.Attack);
			if (cmp != 0) return cmp;

			cmp = Health.CompareTo(other.Health);
			if (cmp != 0) return cmp;

			cmp = Abilities.Count.CompareTo(other.Abilities.Count);
			if (cmp != 0) return cmp;

			for (int i = 0; i < Abilities.Count; i++)
			{
				cmp = Abilities[i].CompareTo(other.Abilities[i]);
				if (cmp != 0) return cmp;
			}

			return 0;
		}

	}

	public class Counter<T> where T : IComparable<T>
	{
		private Dictionary<T, int> _stats = new Dictionary<T, int>();

		public ReadOnlyDictionary<T, int> Result => new ReadOnlyDictionary<T, int>(_stats);

		public void Add(T key)
		{
			if (!_stats.ContainsKey(key))
			{
				_stats[key] = 0;
			}

			_stats[key] = _stats[key] + 1;
		}

		public void AddHistogram<K>(IDictionary<K, T> bins)
		{
			foreach (var binKvp in bins)
			{
				T binCount = binKvp.Value;
				Add(binCount);
			}
		}

		public void WriteCsv(string filename, List<string> colHeaders, Func<T, List<object>> keyToCols)
		{
			DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
			var file = FileAccess.Open($"{Constants.UserDataDirectory}/{filename}", FileAccess.ModeFlags.Write);

			var csvBuilder = new StringBuilder();
			csvBuilder.AppendLine(string.Join(", ", colHeaders) + ", Count");

			var sortedStats = new List<KeyValuePair<T, int>>(_stats);
			sortedStats.Sort((k1, k2) => k1.Key.CompareTo(k2.Key));

			foreach (var kvp in sortedStats)
			{
				csvBuilder.AppendLine(string.Join(", ", keyToCols(kvp.Key)) + ", " + kvp.Value.ToString());
			}

			file.StoreString(csvBuilder.ToString());
			file.Close();
		}
	}
}
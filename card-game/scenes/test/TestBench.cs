using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

public partial class TestBench : Node2D
{
	public override void _Ready()
	{
	}

	public void Click_GenerateCardPool()
	{
		var cardPool = CardGenerator.GenerateRandomCardPool("TestBench Card Pool");
		DeckPopUp.PopUp(GetChild(0), cardPool.Cards, fadeBackground: true);
	}

	public class CardAnalysisKey : IComparable<CardAnalysisKey>
	{
		public CardRarity Rarity { get; private set; }
		public CardBloodCost BloodCost { get; private set; }
		public int Attack { get; private set; }
		public int Health { get; private set; }

		public CardAnalysisKey(CardInfo info)
		{
			Rarity = info.Rarity;
			BloodCost = info.BloodCost;
			Attack = info.Attack;
			Health = info.Health;
		}

        public override bool Equals(object obj)
        {
            CardAnalysisKey o = obj as CardAnalysisKey;
			if (o == null)
			{
				return false;
			}

			return o.Rarity == Rarity && o.BloodCost == BloodCost && o.Attack == Attack && o.Health == Health;
        }

		public override int GetHashCode()
		{
			return Rarity.GetHashCode() ^ BloodCost.GetHashCode() ^ Attack.GetHashCode() ^ Health.GetHashCode();
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

	public void Click_AnalyzeCardPool()
	{
		var textEdit = FindChild("AnalyzeCardsCount") as TextEdit;
		var label = FindChild("AnalyzeCardsLabel") as Label;
		int poolsCount;
		if (textEdit == null ||
			!int.TryParse(textEdit.Text, out poolsCount) ||
			poolsCount < 1 || poolsCount > 1000)
		{
			label.Text = "Invalid Sample Size";
			return;
		}

		var startTime = DateTime.Now;
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
			new List<string> { "Rarity", "BloodCost", "Attack", "Health" },
			(key) => new List<object> { key.Rarity.ToString(), key.BloodCost.ToString(), key.Attack, key.Health });

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
		
		var analysisTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
		label.Text = $"Analyzed {poolsCount} in {analysisTime}ms";
	}

	public void Click_ResetSettings()
	{
		CardGenerator.ResetCardGeneratorSettings();
	}

	public void Click_BackToMainMenu()
	{
		SceneLoader.Instance.LoadMainMenu();
	}

	#region One-off Experiments
	public void Click_SerializationTest()
	{
		GameLoader.Debug_TestEndToEnd();
	}

	public void Click_PlaySound()
	{
		float pitchScale = 1.0f;
		var pitchTextBox = FindChild("PitchScaleLineEdit") as LineEdit;
		if (!string.IsNullOrEmpty(pitchTextBox.Text))
		{
			float.TryParse(pitchTextBox.Text, out pitchScale);
		}

		var volumeSlider = FindChild("VolumeSlider") as Slider;
		float volume = (float)volumeSlider.Value;
		GD.Print("Volume = ", volume);

		AudioManager.Instance.Play(Constants.Audio.CardsShuffle, pitch: pitchScale, volume: volume);
	}

	public void Click_PlayManySounds()
	{
		this.StartCoroutine(Debug_PlaySoundCoroutine());
	}

	public void Click_SampleCoroutine()
	{
		this.StartCoroutine(Debug_TestCoroutine());
	}

	private IEnumerable Debug_PlaySoundCoroutine()
	{
		yield return AudioManager.Instance.Play(Constants.Audio.CardsShuffle);
		yield return new CoroutineDelay(1.0);
		yield return AudioManager.Instance.Play(Constants.Audio.CardsShuffle, pitch: 0.5f);
		yield return new CoroutineDelay(1.0);
		yield return AudioManager.Instance.Play(Constants.Audio.CardsShuffle, pitch: 1.25f);
	}

	private IEnumerable Debug_TestCoroutine()
	{
		GD.Print("Testing the coroutine!");
		yield return new CoroutineDelay(2.0);
		GD.Print("I waited 2 seconds!");
		yield return null;
		GD.Print("And that time I didn't wait at all!");
		for (int i = 10; i > 0; i--)
		{
			GD.Print($"{i}...");
			yield return new CoroutineDelay(0.2);
		}

		GD.Print("Blastoff!");
		yield return new CoroutineDelay(5);
		GD.Print("Get ready for a big one...");
		yield return new CoroutineDelay(1);
		for (int i = 100; i > 0; i--)
		{
			GD.Print($"{i}...!");
			yield return null;
		}

		GD.Print("Okay I'm done! Bye!");
	}
	#endregion
}

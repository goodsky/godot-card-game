using Godot;
using System;
using System.Collections;

public partial class TestBench : Node2D
{
	public override void _Ready()
	{
	}

	public void Click_AnalyzeGameBalance()
	{
		var cardPoolTextEdit = FindChild("CardPoolCount") as TextEdit;
		var gamesToSimTextEdit = FindChild("CardPoolCount") as TextEdit;
		var resultsLabel = FindChild("BalanceResultsLabel") as Label;

		if (!TryReadTextEdit(cardPoolTextEdit, 1, 100, out int cardPoolCount))
		{
			resultsLabel.Text = "Invalid Card Pool Size";
			return;
		}

		if (!TryReadTextEdit(gamesToSimTextEdit, 1, 100, out int gamesCount))
		{
			resultsLabel.Text = "Invalid Games To Simulate Count";
			return;
		}

		var startTime = DateTime.Now;
		try
		{
			GameAnalyzer.AnalyzeGameBalance(cardPoolCount, gamesCount, minLevel: 1, maxLevel: 12);
		}
		catch (Exception e)
		{
			resultsLabel.Text = $"EXCEPTION: {e.Message}";
			GD.Print(e.ToString());
			return;
		}

		var analysisTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
		resultsLabel.Text = $"Analysis completed in {analysisTime}ms";
	}

	public void Click_SimulateSaveFile()
	{
		var resultsLabel = FindChild("SimulatorResultsLabel") as Label;

		GameManager.Instance.RefreshSavedGame();

		GameProgress progress = GameManager.Instance.Progress;
		if (progress == null || progress.CurrentState != LobbyState.PlayGame)
		{
			resultsLabel.Text = "Error: Make sure current save is an active game!";
			return;
		}

		var (sacrificeDeck, creatureDeck, gameLevel) = GameLobby.InitializeGame();

		SimulatorArgs args = new SimulatorArgs
		{
			EnableLogging = true,
			StartingHandSize = progress.HandSize,
			SacrificesDeck = sacrificeDeck.Cards,
			CreaturesDeck = creatureDeck.Cards,
			AI = gameLevel.AI,
		};

		var startTime = DateTime.Now;
		try
		{
			new GameSimulator(maxBranchPerTurn: 2).Simulate(args);
		}
		catch (Exception e)
		{
			resultsLabel.Text = $"EXCEPTION: {e.Message}";
			GD.Print(e.ToString());
			return;
		}

		var analysisTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
		resultsLabel.Text = $"Simulation completed in {analysisTime}ms";
	}

	public void Click_GameSimulatorTests()
	{
		var resultsLabel = FindChild("SimulatorResultsLabel") as Label;

		bool allPass = GameSimulatorTests.Go();
		resultsLabel.Text = $"{(allPass ? "All tests passed!" : "TEST FAILURES")}";
	}

	public void Click_GenerateCardPool()
	{
		var cardPool = CardGenerator.GenerateRandomCardPool("TestBench Card Pool");
		DeckPopUp.PopUp(GetChild(0), cardPool.Cards, fadeBackground: true);
	}

	public void Click_AnalyzeCardPool()
	{
		var textEdit = FindChild("AnalyzeCardsCount") as TextEdit;
		var label = FindChild("AnalyzeCardsLabel") as Label;

		if (!TryReadTextEdit(textEdit, 1, 1000, out int poolsCount))
		{
			label.Text = "Invalid Card Pool Count";
			return;
		}
		
		var startTime = DateTime.Now;
		CardPoolAnalyzer.AnalyzeCardPools(poolsCount);

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

	private bool TryReadTextEdit(TextEdit textEdit, int minValue, int maxValue, out int value)
	{
		if (textEdit == null ||
			!int.TryParse(textEdit.Text, out value) ||
			value < minValue || value > maxValue)
		{
			value = default;
			return false;
		}

		return true;
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

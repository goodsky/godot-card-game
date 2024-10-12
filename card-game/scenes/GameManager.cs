using System.Collections.Generic;
using Godot;

public class GameProgress
{
	public int Level { get; set; }

	public int Score { get; set; }

	public CardPool CardPool { get; set; }

	public List<CardInfo> DeckCards { get; set; }

    public LobbyState CurrentState { get; set; }

    public int Seed { get; set; }

    public int SeedN { get; set; }
}


public partial class GameManager : Node
{
    public GameProgress Progress { get; private set; }

    public RandomGenerator Random { get; private set;}

    public static GameManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        InitializeSettings();

        if (GameLoader.SavedGameExists())
        {
            (Progress, Random) = GameLoader.LoadGame();
        }
        else
        {
            Progress = null;
            Random = new RandomGenerator();
        }
    }

    public void RefreshSavedGame()
    {
        (Progress, Random) = GameLoader.LoadGame();
    }

    public void SaveGame()
    {
        if (Progress == null)
        {
            GD.PushError("Cannot save game! No game is in progress.");
            return;
        }

        GameLoader.SaveGame(Progress);
    }

    public void StartNewGame(CardPool cardPool)
    {
        Random = new RandomGenerator();
        Progress = new GameProgress
        {
            Level = 1,
			Score = 0,
			CardPool = cardPool,
			DeckCards = new List<CardInfo>(),
            Seed = Random.Seed,
            SeedN = Random.N,
        };

        // Note: don't save until the new deck is created
    }

    // A few rules for updating progress becuase of the Random Seed
    //   * Save before using generating any random values - we want it to line up after loading a save
    public void UpdateProgress(LobbyState currentState, int? level = null, int? score = null, List<CardInfo> updatedDeck = null, bool updateSeed = false, bool resetSeed = false)
    {
        if (resetSeed)
        {
            updateSeed = true;
            Random = new RandomGenerator();
        }

        Progress = new GameProgress
        {
            CurrentState = currentState,
            Level = level ?? Progress.Level,
            Score = score ?? Progress.Score,
            DeckCards = updatedDeck ?? Progress.DeckCards,
            CardPool = Progress.CardPool,
            Seed = updateSeed ? Random.Seed: Progress.Seed,
            SeedN = updateSeed ? Random.N : Progress.SeedN,
        };

        SaveGame();
    }

    public void ClearGame()
    {
        GameLoader.ClearGame();
        Progress = null;
        Random = null;
    }

    private void InitializeSettings()
	{
		var settings = GameLoader.LoadSettings();
		AudioManager.Instance.UpdateEffectsVolume(settings.EffectsVolume);
	}
}
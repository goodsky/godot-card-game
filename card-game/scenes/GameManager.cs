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

    public RandomGenerator Random { get; private set; }

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
        // Reload the game from disk - useful for resetting the random seed to an expected state
        (Progress, Random) = GameLoader.LoadGame();
    }

    public void ResetRandomSeed(int seed, int? n = null)
    {
        // Don't forget to update the game progress on disk after resetting a seed if you need to reload
        Random = new RandomGenerator(seed, n);
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

    public void UpdateProgress(LobbyState currentState, int? level = null, int? score = null, List<CardInfo> updatedDeck = null, bool updateSeed = false, bool resetSeed = false)
    {
        // It's super important that you save a game seed before generating random values for a scene that can be reloaded.
        // The current implementation relies on random seeds generating the same values after load!
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
            Seed = updateSeed ? Random.Seed : Progress.Seed,
            SeedN = updateSeed ? Random.N : Progress.SeedN,
        };

        GameLoader.SaveGame(Progress);
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
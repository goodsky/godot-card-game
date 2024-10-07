using System.Collections.Generic;
using Godot;

public class GameProgress
{
	public int Level { get; set; }

	public int Score { get; set; }

	public CardPool CardPool { get; set; }

	public List<CardInfo> DeckCards { get; set; }

    public LobbyState CurrentState { get; set; }
}


public partial class GameManager : Node
{
    public GameProgress Progress { get; private set; }

    public static GameManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        InitializeSettings();

        if (GameLoader.SavedGameExists())
        {
            Progress = GameLoader.LoadGame();
        }
        else
        {
            Progress = null;
        }
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
        Progress = new GameProgress
        {
            Level = 1,
			Score = 0,
			CardPool = cardPool,
			DeckCards = new List<CardInfo>(),
        };

        // Note: don't save until the new deck is created
    }

    public void UpdateProgress(LobbyState currentState, int? level = null, int? score = null, List<CardInfo> updatedDeck = null)
    {
        Progress = new GameProgress
        {
            CurrentState = currentState,
            Level = level ?? Progress.Level,
            Score = score ?? Progress.Score,
            DeckCards = updatedDeck ?? Progress.DeckCards,
            CardPool = Progress.CardPool,
        };

        SaveGame();
    }

    public void ClearGame()
    {
        GameLoader.ClearGame();
        Progress = null;
    }

    private void InitializeSettings()
	{
		var settings = GameLoader.LoadSettings();
		AudioManager.Instance.UpdateEffectsVolume(settings.EffectsVolume);
	}
}
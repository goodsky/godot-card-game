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


public partial class GameProgressManager : Node
{
    public GameProgress State { get; private set; }

    public static GameProgressManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        if (GameLoader.SavedGameExists())
        {
            State = GameLoader.LoadGame();
        }
        else
        {
            State = null;
        }
    }

    public void SaveGame()
    {
        if (State == null)
        {
            GD.PushError("Cannot save game! No game is in progress.");
            return;
        }

        GameLoader.SaveGame(State);
    }

    public void StartNewGame(CardPool cardPool)
    {
        State = new GameProgress
        {
            Level = 0,
			Score = 0,
			CardPool = cardPool,
			DeckCards = new List<CardInfo>(),
        };

        // Note: don't save until the new deck is created
    }

    public void UpdateProgress(LobbyState currentState, int? level = null, int? score = null, List<CardInfo> updatedDeck = null)
    {
        State = new GameProgress
        {
            CurrentState = currentState,
            Level = level ?? State.Level,
            Score = score ?? State.Score,
            DeckCards = updatedDeck ?? State.DeckCards,
            CardPool = State.CardPool,
        };

        SaveGame();
    }
}
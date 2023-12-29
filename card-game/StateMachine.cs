using System.Runtime.Serialization;

namespace CardGame.Core;

public enum CardGameState
{
    Initializing = -1,
    StartTurn = 0,
    DrawCard = 100,
    PlayCard = 200,
    PlayStagedCards = 210, // AI-only
    PlayCardCost = 201,
    StartCombat = 300,
    EndCombat = 399,
    AddStagedCards = 400, // AI-only
    EndTurn = 999,
}

public enum CardZone
{
    Deck,
    Hand,
    PlayArea,
}

public interface IGameEventListener
{
    Card DrawCard(int deckId);
    void HoverOverCard(CardZone zone, int cardId);
    void ClickOnCard(CardZone zone, int cardId);
    void ClickEndTurn();
}

public interface IGameStateListener
{
    void OnEnter(CardGameState oldState);
    void OnLeave(CardGameState newState);
}

public class StateMachine : IGameEventListener
{
    public CardGameState CurrentState { get; private set; }

    public Card DrawCard(int deckId)
    {
        throw new NotImplementedException();
    }

    public void HoverOverCard(CardZone zone, int cardId)
    {
        throw new NotImplementedException();
    }

    public void ClickOnCard(CardZone zone, int cardId)
    {
        throw new NotImplementedException();
    }

    public void ClickEndTurn()
    {
        throw new NotImplementedException();
    }
}
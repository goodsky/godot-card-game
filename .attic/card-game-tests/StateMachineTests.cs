using NSubstitute;

namespace CardGame.Tests;

public class StateMachineTests
{
    private static readonly int MOCK_CARD_ID = 123;

    [Fact]
    public void Initializes()
    {
        var stateMachine = new StateMachine();
        Assert.Equal(GameState.NotInitialized, stateMachine.CurrentState);
        Assert.Equal(GameTurn.PlayerTurn, stateMachine.CurrentTurn);

        stateMachine.Initialize();
        Assert.Equal(GameState.DrawCard, stateMachine.CurrentState);
        Assert.Equal(GameTurn.PlayerTurn, stateMachine.CurrentTurn);
    }

    [Fact]
    public void CanTransitionState()
    {
        var stateMachine = new StateMachine();
        stateMachine.OverrideCurrentState(GameState.AddStagedCards, GameTurn.AITurn);
        Assert.Equal(GameTurn.AITurn, stateMachine.CurrentTurn);
        Assert.Equal(GameState.AddStagedCards, stateMachine.CurrentState);
    }

    [Fact]
    public void EventListener_Add_Remove()
    {
        var stateMachine = new StateMachine();
        var mockEventListener = Substitute.For<IGameEventListener>();
        stateMachine.addEventListener(mockEventListener);

        stateMachine.ClickOnCard(CardZone.Deck, MOCK_CARD_ID);
        mockEventListener.Received().OnClickOnCard(CardZone.Deck, MOCK_CARD_ID);

        stateMachine.removeEventListener(mockEventListener);

        stateMachine.ClickEndTurn();
        mockEventListener.DidNotReceive().OnClickEndTurn();
    }

    [Fact]
    public void EventListener_ReceivesEvents()
    {
        var stateMachine = new StateMachine();
        var mockEventListener = Substitute.For<IGameEventListener>();
        stateMachine.addEventListener(mockEventListener);

        stateMachine.HoverOverCard(CardZone.Hand, MOCK_CARD_ID);
        mockEventListener.Received().OnHoverOverCard(CardZone.Hand, MOCK_CARD_ID);

        stateMachine.ClickOnCard(CardZone.PlayArea, MOCK_CARD_ID);
        mockEventListener.Received().OnClickOnCard(CardZone.PlayArea, MOCK_CARD_ID);

        stateMachine.ClickEndTurn();
        mockEventListener.Received().OnClickEndTurn();
    }

    [Fact]
    public void EventListener_ReceivesEvents_ForState()
    {
        var stateMachine = new StateMachine();
        var mockEventListener = Substitute.For<IGameEventListener>();
        stateMachine.addEventListener(mockEventListener, state: GameState.PlayCard);

        stateMachine.OverrideCurrentState(GameState.DrawCard, GameTurn.PlayerTurn);

        stateMachine.HoverOverCard(CardZone.Hand, MOCK_CARD_ID);
        mockEventListener.DidNotReceive().OnHoverOverCard(Arg.Any<CardZone>(), Arg.Any<int>());

        stateMachine.ClickOnCard(CardZone.PlayArea, MOCK_CARD_ID);
        mockEventListener.DidNotReceive().OnClickOnCard(Arg.Any<CardZone>(), Arg.Any<int>());

        stateMachine.ClickEndTurn();
        mockEventListener.DidNotReceive().OnClickEndTurn();

        stateMachine.OverrideCurrentState(GameState.PlayCard, GameTurn.PlayerTurn);

        stateMachine.HoverOverCard(CardZone.Hand, MOCK_CARD_ID);
        mockEventListener.Received().OnHoverOverCard(CardZone.Hand, MOCK_CARD_ID);

        stateMachine.ClickOnCard(CardZone.PlayArea, MOCK_CARD_ID);
        mockEventListener.Received().OnClickOnCard(CardZone.PlayArea, MOCK_CARD_ID);

        stateMachine.ClickEndTurn();
        mockEventListener.Received().OnClickEndTurn();
    }

    [Fact]
    public void EventListener_ReceivesEvents_ForTurn()
    {
        var stateMachine = new StateMachine();
        var mockEventListener = Substitute.For<IGameEventListener>();
        stateMachine.addEventListener(mockEventListener, turn: GameTurn.AITurn);

        stateMachine.OverrideCurrentState(GameState.DrawCard, GameTurn.PlayerTurn);

        stateMachine.HoverOverCard(CardZone.Hand, MOCK_CARD_ID);
        mockEventListener.DidNotReceive().OnHoverOverCard(Arg.Any<CardZone>(), Arg.Any<int>());

        stateMachine.ClickOnCard(CardZone.PlayArea, MOCK_CARD_ID);
        mockEventListener.DidNotReceive().OnClickOnCard(Arg.Any<CardZone>(), Arg.Any<int>());

        stateMachine.ClickEndTurn();
        mockEventListener.DidNotReceive().OnClickEndTurn();

        stateMachine.OverrideCurrentState(GameState.DrawCard, GameTurn.AITurn);

        stateMachine.HoverOverCard(CardZone.Hand, MOCK_CARD_ID);
        mockEventListener.Received().OnHoverOverCard(CardZone.Hand, MOCK_CARD_ID);

        stateMachine.ClickOnCard(CardZone.PlayArea, MOCK_CARD_ID);
        mockEventListener.Received().OnClickOnCard(CardZone.PlayArea, MOCK_CARD_ID);

        stateMachine.ClickEndTurn();
        mockEventListener.Received().OnClickEndTurn();
    }

    [Fact]
    public void StateListener_Add_Remove()
    {
        var stateMachine = new StateMachine();
        var mockCombatListener = Substitute.For<IGameStateListener>();
        stateMachine.addStateListener(mockCombatListener, state: GameState.Combat);

        stateMachine.OverrideCurrentState(GameState.Combat, GameTurn.PlayerTurn);
        mockCombatListener.Received().OnEnter(Arg.Any<GameState>(), Arg.Any<GameTurn>());

        stateMachine.removeStateListener(mockCombatListener);
        
        stateMachine.OverrideCurrentState(GameState.DrawCard, GameTurn.AITurn);
        mockCombatListener.DidNotReceive().OnLeave(Arg.Any<GameState>(), Arg.Any<GameTurn>());
    }

    [Fact]
    public void StateListener_ReceivesStateTransition()
    {
        var stateMachine = new StateMachine();

        var mockNotInitializedListener = Substitute.For<IGameStateListener>();
        stateMachine.addStateListener(mockNotInitializedListener, state: GameState.NotInitialized);

        var mockDrawCardListener = Substitute.For<IGameStateListener>();
        stateMachine.addStateListener(mockDrawCardListener, state: GameState.DrawCard);

        stateMachine.OverrideCurrentState(GameState.DrawCard, GameTurn.PlayerTurn);

        mockNotInitializedListener.Received().OnLeave(GameState.DrawCard, GameTurn.PlayerTurn);
        mockDrawCardListener.Received().OnEnter(GameState.NotInitialized, GameTurn.PlayerTurn);
    }

    [Fact]
    public void StateListener_ReceivesTurnTransition()
    {
        var stateMachine = new StateMachine();
        var mockTurnListener = Substitute.For<IGameStateListener>();
        stateMachine.addStateListener(mockTurnListener, turn: GameTurn.PlayerTurn);

        stateMachine.OverrideCurrentState(GameState.PlayStagedCards, GameTurn.AITurn);
        mockTurnListener.Received().OnLeave(GameState.PlayStagedCards, GameTurn.AITurn);

        stateMachine.OverrideCurrentState(GameState.Combat, GameTurn.AITurn);
        mockTurnListener.DidNotReceive().OnEnter(Arg.Any<GameState>(), Arg.Any<GameTurn>());

        stateMachine.OverrideCurrentState(GameState.DrawCard, GameTurn.PlayerTurn);
        mockTurnListener.Received().OnEnter(GameState.Combat, GameTurn.AITurn);
    }
}

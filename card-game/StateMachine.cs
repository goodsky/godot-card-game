namespace CardGame.Core;

public enum GameTurn
{
    PlayerTurn = 0,
    AITurn,
}

public enum GameState
{
    NotInitialized = 0,
    DrawCard = 100,
    PlayCard = 200,
    PlayCardCost = 201,
    PlayStagedCards = 210, // AI-only
    Combat = 300,
    AddStagedCards = 400, // AI-only
}

public enum CardZone
{
    Deck,
    Hand,
    PlayArea,
}

public interface IGameEventListener
{
    void OnHoverOverCard(CardZone zone, int cardId);
    void OnClickOnCard(CardZone zone, int cardId);
    void OnClickEndTurn();
}

public interface IGameStateListener
{
    void OnEnter(GameState oldState, GameTurn oldTurn);
    void OnLeave(GameState newState, GameTurn newTurn);
}

public class StateMachine
{
    private List<RegisteredListener<IGameEventListener>> _eventListeners = new List<RegisteredListener<IGameEventListener>>();
    private List<RegisteredListener<IGameStateListener>> _stateListeners = new List<RegisteredListener<IGameStateListener>>();

    public GameState CurrentState { get; private set; } = GameState.NotInitialized;
    public GameTurn CurrentTurn { get; private set; } = GameTurn.PlayerTurn;

    public void Initialize()
    {
        CurrentState = GameState.DrawCard;
        CurrentTurn = GameTurn.PlayerTurn;
    }

    public void HoverOverCard(CardZone zone, int cardId)
    {
        foreach (var listener in GetCurrentEventListeners())
        {
            listener.OnHoverOverCard(zone, cardId);
        }
    }

    public void ClickOnCard(CardZone zone, int cardId)
    {
        foreach (var listener in GetCurrentEventListeners())
        {
            listener.OnClickOnCard(zone, cardId);
        }
    }

    public void ClickEndTurn()
    {
        foreach (var listener in GetCurrentEventListeners())
        {
            listener.OnClickEndTurn();
        }
    }


    public void addEventListener<TEventListener>(TEventListener eventListener, GameState? state = null, GameTurn? turn = null) where TEventListener : IGameEventListener
    {
        _eventListeners.Add(new RegisteredListener<IGameEventListener>(eventListener, state, turn));
    }

    public void removeEventListener<TEventListener>(TEventListener eventListener) where TEventListener : IGameEventListener
    {
        var registeredListenersToRemove = _eventListeners.Where(x => x.Listener == (IGameEventListener)eventListener).ToList();
        foreach (var registeredListener in registeredListenersToRemove)
        {
            _eventListeners.Remove(registeredListener);
        }
    }

    public void addStateListener<TStateListener>(TStateListener stateListener, GameState? state = null, GameTurn? turn = null) where TStateListener : IGameStateListener
    {
        _stateListeners.Add(new RegisteredListener<IGameStateListener>(stateListener, state, turn));
    }

    public void removeStateListener<TStateListener>(TStateListener stateListener) where TStateListener : IGameStateListener
    {
        var registeredListenersToRemove = _stateListeners.Where(x => x.Listener == (IGameStateListener)stateListener).ToList();
        foreach (var registeredListener in registeredListenersToRemove)
        {
            _stateListeners.Remove(registeredListener);
        }
    }

    protected void TransitionToState(GameState nextState, GameTurn nextTurn)
    {
        GameState oldState = CurrentState;
        GameTurn oldTurn = CurrentTurn;

        var (transitionedOut, transitionedIn) = GetTriggeredStateListeners(nextState, nextTurn);

        foreach (var listener in transitionedOut)
        {
            listener.OnLeave(nextState, nextTurn);
        }

        CurrentState = nextState;
        CurrentTurn = nextTurn;

        foreach (var listener in transitionedIn)
        {
            listener.OnEnter(oldState, oldTurn);
        }
    }

    private IEnumerable<IGameEventListener> GetCurrentEventListeners()
    {
        GameState currentState = CurrentState;
        GameTurn currentTurn = CurrentTurn;
        return _eventListeners
            .Where(registeredListener => registeredListener.IsActive(currentState, currentTurn))
            .Select(registeredListener => registeredListener.Listener);
    }

    private (IEnumerable<IGameStateListener> transitionedOut, IEnumerable<IGameStateListener> transitionedIn) GetTriggeredStateListeners(GameState nextState, GameTurn nextTurn)
    {
        GameState currentState = CurrentState;
        GameTurn currentTurn = CurrentTurn;

        var transitionedOut = _stateListeners
            .Where(registeredListener => registeredListener.IsActive(currentState, currentTurn) && !registeredListener.IsActive(nextState, nextTurn))
            .Select(registeredListner => registeredListner.Listener);

        var transitionedIn = _stateListeners
            .Where(registeredListener => !registeredListener.IsActive(currentState, currentTurn) && registeredListener.IsActive(nextState, nextTurn))
            .Select(registeredListener => registeredListener.Listener);

        return (transitionedOut, transitionedIn);
    }

    public abstract class BaseListener
    {
        private StateMachine _stateMachine;
        public BaseListener(StateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        protected void TransitionToState(GameState nextState)
        {
            _stateMachine.TransitionToState(nextState, _stateMachine.CurrentTurn);
        }

        protected void TransitionToState(GameState nextState, GameTurn nextTurn)
        {
            _stateMachine.TransitionToState(nextState, nextTurn);
        }
    }

    private struct RegisteredListener<T>
    {
        public GameState? State { get; }
        public GameTurn? Turn { get; }
        public T Listener { get; }

        public RegisteredListener(T listener, GameState? state, GameTurn? turn)
        {
            State = state;
            Turn = turn;
            Listener = listener;
        }

        public bool IsActive(GameState state, GameTurn turn) => (State == null || State == state) && (Turn == null || Turn == turn);
    }
}
namespace CardGame.Tests;

public static class StateMachineExtensions
{
    public static void OverrideCurrentState(this StateMachine stateMachine, GameState state, GameTurn turn)
    {
        new ManualStateMachineTransitioner(stateMachine).SetCurrentState(state, turn);
    }

    /// <summary>
    /// This is a misuse of the BaseListener class to provide test-only functionality.
        /// </summary>
    private class ManualStateMachineTransitioner : StateMachine.BaseListener
    {
        public ManualStateMachineTransitioner(StateMachine stateMachine) : base(stateMachine) {}

        public void SetCurrentState(GameState state, GameTurn turn)
        {
            TransitionToState(state, turn);
        }
    }
}


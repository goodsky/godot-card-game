namespace CardGame.Test;

public class StateMachineTests
{
    [Fact]
    public void StateMachine_Initializes()
    {
        var stateMachine = new StateMachine();
        Assert.Equal(CardGameState.Initializing, stateMachine.CurrentState);
    }
}
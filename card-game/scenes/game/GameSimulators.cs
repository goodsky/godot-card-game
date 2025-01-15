
using System.Collections.Generic;
using System.Linq;

public class GreedyHeuristicGameSimulator : GameSimulatorBase
{
    protected override IEnumerable<PlayerTurnAction> GetPlayerActions(SimulatorState state)
    {
        var bestPlayerActions = PlayerTurnAction.TakeTop(
            count: 1,
            minValue: 0,
            CalculateBestPlayerActionsDrawCreature(state),
            CalculateBestPlayerActionsDrawSacrifice(state));

        PlayerTurnAction bestAction = bestPlayerActions.FirstOrDefault();
        
        if (bestAction == null)
        {
            state.Logger.Log($"No best action found! No available moves or all actions have negative heuristic. Let's just draw a card.");
            DrawAction drawAction = DrawAction.NoDraw;
            if (state.Creatures.RemainingCardCount > 0)
            {
                drawAction = DrawAction.DrawFromCreatures;
            }
            else if (state.Sacrifices.RemainingCardCount > 0)
            {
                drawAction = DrawAction.DrawFromSacrifices;
            }

            bestAction = new PlayerTurnAction() { DrawAction = drawAction, CardActions = new PlayCardAction[0] };
        }

        state.Logger.LogHeader("BEST ACTION");
        state.Logger.LogAction(bestAction);
        return new[] { bestAction };
    }

    private static IEnumerable<PlayerTurnAction> CalculateBestPlayerActionsDrawCreature(SimulatorState state)
    {
        if (state.Creatures.RemainingCardCount == 0)
        {
            state.Logger.Log("Creatures deck is empty! - Skipping player actions for drawing creatures");
            return Enumerable.Empty<PlayerTurnAction>();
        }

        var newCreature = state.Creatures.PeekTop();
        var newHand = state.Hand.Append(newCreature).ToList();

        state.Logger.LogHeader("[Calculate Action] DRAW CREATURE");
        return CalculateBestPlayerActions(DrawAction.DrawFromCreatures, newHand, state.Lanes, state.Logger);
    }

    private static IEnumerable<PlayerTurnAction> CalculateBestPlayerActionsDrawSacrifice(SimulatorState state)
    {
        if (state.Sacrifices.RemainingCardCount == 0)
        {
            state.Logger.Log("Sacrifices deck is empty! - Skipping player actions for drawing sacrifices");
            return Enumerable.Empty<PlayerTurnAction>();
        }

        var newSacrifice = state.Sacrifices.PeekTop();
        var newHand = state.Hand.Append(newSacrifice).ToList();

        state.Logger.LogHeader("[Calculate Action] DRAW SACRIFICE");
        return CalculateBestPlayerActions(DrawAction.DrawFromSacrifices, newHand, state.Lanes, state.Logger);
    }
}
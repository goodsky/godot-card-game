using System;
using System.Collections.Generic;
using System.Linq;

public class SimulatorArgs
{
    public int StartingHandSize { get; set; }
    public Deck Creatures { get; set; }
    public Deck Sacrifices { get; set; }
    public EnemyAI AI { get; set; }
    public IPlayerTurnHandler TurnHandler { get; set; }
}

public class SimulatorResult
{
    public int Turns { get; set; }
    public int PlayerDamage { get; set; }
    public List<CardInfo> PlayerDeadCards { get; set; }
    public Dictionary<CardInfo, int> PlayerCardDamage { get; set; }
    public int EnemyDamage { get; set; }
    public List<CardInfo> EnemyDeadCards { get; set; }
    public Dictionary<CardInfo, int> EnemyCardDamage { get; set; }
}

public interface IPlayerTurnHandler
{

}

public class CombatSimulator
{
    private static readonly int PLAYER_LANE_INDEX = 0;
	private static readonly int ENEMY_LANE_INDEX = 1;
	private static readonly int ENEMY_STAGE_LANE_INDEX = 2;

    private class SimulatorState
    {
        public List<CardInfo> Hand { get; set; }
        public CardInfo[,] Lanes { get; set; }
        public Deck Creatures { get; set; }
        public Deck Sacrifices { get; set; }
        public EnemyAI AI { get; set; }
        public IPlayerTurnHandler TurnHandler { get; set; }
    }

    public SimulatorResult Simulate(SimulatorArgs args)
    {
        var state = new SimulatorState
        {
            Hand = new List<CardInfo>(),
            Lanes = new CardInfo[4, 3],
            Creatures = args.Creatures,
            Sacrifices = args.Sacrifices,
            AI = args.AI,
            TurnHandler = args.TurnHandler,
        };

        for (int i = 0; i < args.StartingHandSize; i++)
        {
            state.Hand.Add(state.Creatures.DrawFromTop());
        }

        return SimulateInternal(state);
    }

    private SimulatorResult SimulateInternal(SimulatorState state)
    {
        var result = new SimulatorResult
        {
            Turns = 0,
            PlayerDamage = 0,
            PlayerCardDamage = new Dictionary<CardInfo, int>(),
            PlayerDeadCards = new List<CardInfo>(),
            EnemyDamage = 0,
            EnemyCardDamage = new Dictionary<CardInfo, int>(),
            EnemyDeadCards = new List<CardInfo>(),
        };

        while (Math.Abs(result.PlayerDamage - result.EnemyDamage) < 5)
        {
            // If Player Turn
            // Draw

            // Play Cards

            // Player Combat

            // If Enemy Turn
            // Enemy Promote Staged Cards

            // Enemy Combat

            // Enemy Stage Next Cards
        }

        return result;
    }
}
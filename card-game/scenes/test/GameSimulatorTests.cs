using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

[AttributeUsage(AttributeTargets.Method)]
public class GameSimulatorTest : Attribute {}

// You may ask yourself, why not just use NUnit or some other testing framework?
// Godot APIs (like logging) kept blowing up outside of the engine.
// So I'm going to do this for now instead of debugging that.
// You can run these from the TestBench scene in the game.
public static class GameSimulatorTests
{
    public static bool Go()
    {
        var methods = typeof(GameSimulatorTests)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<GameSimulatorTest>() != null);

        int failedCount = 0;
        int totalCount = methods.Count();
        GD.Print($"Running {totalCount} GameSimulator tests...");
        foreach (var method in methods)
        {
            try
            {
                method.Invoke(null, null);
                GD.Print($"    ✅ Test {method.Name} passed");
            }
            catch (Exception e)
            {
                ++failedCount;
                
                e = e.InnerException != null ? e.InnerException : e; // TargetInvocationException wraps the original exception
                GD.Print($"    ❌ Test {method.Name} failed: {e.Message}");
            }
        }
        GD.Print($"Testing complete. {totalCount - failedCount}/{totalCount} tests passed.");
        return failedCount == 0;
    }

    [GameSimulatorTest]
    public static void Test_GameSimulator_OnlyPlayerCard()
    {
        const bool LOG_SIMULATION = true;

        var args = new SimulatorArgs
        {
            EnableLogging = LOG_SIMULATION,
            StartingHandSize = 2,
            SacrificesDeck = new List<CardInfo> {
                SetupCardInfo("Sacrifice", "1", 0, 1, CardBloodCost.Zero),
                SetupCardInfo("Sacrifice", "2", 0, 1, CardBloodCost.Zero),
                SetupCardInfo("Sacrifice", "3", 0, 1, CardBloodCost.Zero),
            },
            CreaturesDeck = new List<CardInfo> {
                SetupCardInfo("Creature", "1", 1, 1, CardBloodCost.One),
                SetupCardInfo("Creature", "2", 2, 1, CardBloodCost.Two),
                SetupCardInfo("Creature", "3", 3, 1, CardBloodCost.Three),
            },
            AI = SetupEnemyAI(new ScriptedMove[0]),
        };
        
        var result = new GreedyHeuristicGameSimulator().Simulate(args);
        Assert(result.Rounds.Count == 1, "Should have only played 1 round");
        
        var roundResult = result.Rounds[0];
        Assert(roundResult.PlayerWon, "Player should have won");
    }

    [GameSimulatorTest]
    public static void Test_LaneCombatAnalysis_OnlyPlayerCard()
    {
        var playerCard = SetupCardInfo("Player", "Card", 1, 1);
        var analysis = GameSimulatorBase.AnalyzeLaneCombat(
            playerCard: new SimulatorCard(playerCard),
            enemyCard: null,
            stagedCard: null,
            turnCount: 3,
            playerMovesFirst: true
        );

        Assert(analysis.EnemyDamageReceived == 3, "Enemy should have taken 3 damage");
        Assert(analysis.EnemyCardDamageReceived == 0, "Enemy did not have a card");
        Assert(analysis.PlayerDamageReceived == 0, "Player did not take damage");
        Assert(analysis.PlayerCardDamageReceived == 0, "Player card did not take damage");
    }

    [GameSimulatorTest]
    public static void Test_LaneCombatAnalysis_PlayerAndEnemyCard()
    {
        var playerCard = SetupCardInfo("Player", "Card", 1, 2);
        var enemyCard = SetupCardInfo("Enemy", "Card", 1, 2);
        var analysis = GameSimulatorBase.AnalyzeLaneCombat(
            playerCard: new SimulatorCard(playerCard),
            enemyCard: new SimulatorCard(enemyCard),
            stagedCard: null,
            turnCount: 3,
            playerMovesFirst: true
        );

        Assert(analysis.EnemyDamageReceived == 1, "Enemy should have taken 1 damage");
        Assert(analysis.EnemyCardDamageReceived == 2, "Enemy card should have taken 2 damage");
        Assert(analysis.PlayerDamageReceived == 0, "Player did not take damage");
        Assert(analysis.PlayerCardDamageReceived == 1, "Player card should have taken 1 damage");
    }

    [GameSimulatorTest]
    public static void Test_LaneCombatAnalysis_PlayerAndEnemyStagedCard()
    {
        var playerCard = SetupCardInfo("Player", "Card", 3, 3);
        var enemyStagedCard = SetupCardInfo("Enemy", "Card", 2, 4);
        var analysis = GameSimulatorBase.AnalyzeLaneCombat(
            playerCard: new SimulatorCard(playerCard),
            enemyCard: null,
            stagedCard: new SimulatorCard(enemyStagedCard),
            turnCount: 3,
            playerMovesFirst: true
        );

        Assert(analysis.EnemyDamageReceived == 3, "Enemy should have taken 3 damage");
        Assert(analysis.EnemyCardDamageReceived == 3, "Enemy card should have taken 3 damage");
        Assert(analysis.PlayerDamageReceived == 2, "Player should have taken 2 damage");
        Assert(analysis.PlayerCardDamageReceived == 4, "Player card should have taken 4 damage");
    }

    [GameSimulatorTest]
    public static void Test_TakeTop_PlayerActions()
    {
        var actions1 = new[] { new PlayerTurnAction() { HeuristicScore = 0 }, new PlayerTurnAction() { HeuristicScore = 5 }, new PlayerTurnAction() { HeuristicScore = 5 } };
        var actions2 = new[] { new PlayerTurnAction() { HeuristicScore = 3 }, new PlayerTurnAction() { HeuristicScore = 0 }, new PlayerTurnAction() { HeuristicScore = -10 } };
        var actions3 = new[] { new PlayerTurnAction() { HeuristicScore = 7 }, new PlayerTurnAction() { HeuristicScore = -1 }, new PlayerTurnAction() { HeuristicScore = 0 } };

        var top1 = PlayerTurnAction.TakeTop(1, 0, actions1, actions2, actions3);
        Assert(top1.Count() == 1, "Should have taken 1 action");
        Assert(top1[0].HeuristicScore == 7, "Should have taken the best action");

        var top3 = PlayerTurnAction.TakeTop(3, 0, actions1, actions2, actions3);
        Assert(top3.Count() == 3, "Should have taken 3 actions");
        Assert(top3[0].HeuristicScore == 7, "Index 0: Should have taken the best action");
        Assert(top3[1].HeuristicScore == 5, "Index 1: Should have taken the best action");
        Assert(top3[2].HeuristicScore == 5, "Index 2: Should have taken the best action");

        var top3_high_cutoff = PlayerTurnAction.TakeTop(3, 100, actions1, actions2, actions3);
        Assert(top3_high_cutoff.Count() == 0, "Should have taken 0 actions");

        var top100 = PlayerTurnAction.TakeTop(100, 0, actions1, actions2, actions3);
        Assert(top100.Count() == 4, "Should have taken all positive actions");
    }

    private static void Assert(bool condition, string message = "Assert failed!")
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    private static EnemyAI SetupEnemyAI(ScriptedMove[] moves)
    {
        var emptyCardPool = new CardPool(new CardInfo[0], "Empty Card Pool"); // Assume all scripted moves are fully specified - no randomness!
        var rnd = new RandomGenerator();
        return new EnemyAI(emptyCardPool, moves.ToList(), rnd);
    }

    private static CardInfo SetupCardInfo(string adj, string noun, int atk, int health, CardBloodCost cost = CardBloodCost.One, params CardAbilities[] abilities)
    {
        return new CardInfo
        {
            NameAdjective = adj,
            NameNoun = noun,
            Attack = atk,
            Health = health,
            BloodCost = cost,
            Rarity = CardRarity.Common,
            Abilities = abilities.ToList(),
        };
    }
}
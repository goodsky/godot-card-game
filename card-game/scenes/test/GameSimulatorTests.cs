using System;
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
    public static void Test_LaneCombatAnalysis_OnlyPlayerCard()
    {
        var playerCard = SetupCardInfo("Player", "Card", 1, 1);
        var analysis = SingleRoundCombatSimulator.AnalyzeLaneCombat(
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
        var analysis = SingleRoundCombatSimulator.AnalyzeLaneCombat(
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
        var analysis = SingleRoundCombatSimulator.AnalyzeLaneCombat(
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
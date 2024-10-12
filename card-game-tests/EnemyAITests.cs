namespace CardGame.Tests;

public class EnemyAITests
{
    private readonly ITestOutputHelper _output;

    public EnemyAITests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ResolvesCorrectCardId()
    {
        int cardId = 7;
        var cards = new CardPool(TestUtils.GenerateCardInfo(), "EnemyAITestsDeck");
        var moves = new List<ScriptedMove> {
            new ScriptedMove(0, cardId),
        };

        var ai = new EnemyAI(cards, moves, new RandomGenerator());
        List<PlayedCard> resolvedMoves = ai.GetMovesForTurn(0, new bool[4]);

        Assert.Single(resolvedMoves);
        Assert.Equal(cardId, resolvedMoves[0].Card.Id);
    }

    [Theory]
    [InlineData(CardBloodCost.Zero)]
    [InlineData(CardBloodCost.One)]
    [InlineData(CardBloodCost.Two)]
    [InlineData(CardBloodCost.Three)]
    public void ResolvesCorrectCost(CardBloodCost cost)
    {
        var cards = new CardPool(TestUtils.GenerateCardInfo(), "EnemyAITestsDeck");
        var moves = new List<ScriptedMove> {
            new ScriptedMove(0, cost),
        };

        var ai = new EnemyAI(cards, moves, new RandomGenerator());
        List<PlayedCard> resolvedMoves = ai.GetMovesForTurn(0, new bool[4]);

        Assert.Single(resolvedMoves);
        Assert.Equal(cost, resolvedMoves[0].Card.BloodCost);
    }

    [Theory]
    [InlineData(CardBloodCost.Zero, CardRarity.Sacrifice)]
    [InlineData(CardBloodCost.One, CardRarity.Uncommon)]
    [InlineData(CardBloodCost.Two, CardRarity.Rare)]
    [InlineData(CardBloodCost.Three, CardRarity.Common)]
    public void ResolvesCorrectCostAndRarity(CardBloodCost cost, CardRarity rarity)
    {
        var cards = new CardPool(TestUtils.GenerateCardInfo(), "EnemyAITestsDeck");
        var moves = new List<ScriptedMove> {
            new ScriptedMove(0, cost, rarity),
        };

        var ai = new EnemyAI(cards, moves, new RandomGenerator());
        List<PlayedCard> resolvedMoves = ai.GetMovesForTurn(0, new bool[4]);

        Assert.Single(resolvedMoves);
        Assert.Equal(cost, resolvedMoves[0].Card.BloodCost);
        Assert.Equal(rarity, resolvedMoves[0].Card.Rarity);
    }

    [Fact]
    public void ResolvesMultipleTurns()
    {
        var cards = new CardPool(TestUtils.GenerateCardInfo(), "EnemyAITestsDeck");
        var moves = new List<ScriptedMove>();
        for (int turn = 0; turn < 4; turn++)
        {
            // On turn N - script N moves
            for (int i = 0; i <= turn; i++)
            {
                moves.Add(new ScriptedMove(turn, CardBloodCost.Zero));
            }
        }

        var ai = new EnemyAI(cards, moves, new RandomGenerator());
        for (int turn = 0; turn < 4; turn++)
        {
            List<PlayedCard> resolvedMoves = ai.GetMovesForTurn(turn, new bool[4]);
            Assert.Equal(turn + 1, resolvedMoves.Count);
            for (int i = 0; i < resolvedMoves.Count; i++)
            {
                for (int j = i + 1; j < resolvedMoves.Count; j++)
                {
                    // Make sure the lanes are not overlapping
                    Assert.NotEqual(resolvedMoves[i].Lane, resolvedMoves[j].Lane);
                }
            }
        }
    }

    [Fact]
    public void RespectsOccupiedLanes()
    {
        var cards = new CardPool(TestUtils.GenerateCardInfo(), "EnemyAITestsDeck");
        var moves = new List<ScriptedMove> {
            new ScriptedMove(0, CardBloodCost.Zero),
        };

        for (int openLane = 0; openLane < 4; openLane++)
        {
            bool[] laneHasCard = new bool[4];
            Array.Fill(laneHasCard, true);
            laneHasCard[openLane] = false;

            var ai = new EnemyAI(cards, moves, new RandomGenerator());
            List<PlayedCard> resolvedMoves = ai.GetMovesForTurn(0, laneHasCard);

            Assert.Single(resolvedMoves);
            Assert.Equal(openLane, resolvedMoves[0].Lane);
        }
    }

    [Fact]
    public void RespectsScriptedLanes()
    {
        var cards = new CardPool(TestUtils.GenerateCardInfo(), "EnemyAITestsDeck");

        int expectedLane = 3;
        var moves = new List<ScriptedMove> {
            new ScriptedMove(0, CardBloodCost.Zero, lane: expectedLane),
            new ScriptedMove(1, CardBloodCost.Zero, lane: expectedLane),
            new ScriptedMove(2, CardBloodCost.Zero, lane: expectedLane),
        };

        var ai = new EnemyAI(cards, moves, new RandomGenerator());
        for (int turn = 0; turn < 3; turn++)
        {
            List<PlayedCard> resolvedMoves = ai.GetMovesForTurn(turn, new bool[4]);

            Assert.Single(resolvedMoves);
            Assert.Equal(expectedLane, resolvedMoves[0].Lane);
        }
    }

    [Fact]
    public void ScriptedLanesWaitToBeAvailable()
    {
        var cards = new CardPool(TestUtils.GenerateCardInfo(), "EnemyAITestsDeck");
        var moves = new List<ScriptedMove> {
            new ScriptedMove(0, CardBloodCost.Zero, lane: 0),
            new ScriptedMove(0, CardBloodCost.Zero, lane: 1),
            new ScriptedMove(0, CardBloodCost.Zero, lane: 2),
            new ScriptedMove(0, CardBloodCost.Zero, lane: 3),
        };

        var ai = new EnemyAI(cards, moves, new RandomGenerator());
        for (int openLane = 0; openLane < 4; openLane++)
        {
            bool[] laneHasCard = new bool[4];
            Array.Fill(laneHasCard, true);
            laneHasCard[openLane] = false;

            List<PlayedCard> resolvedMoves = ai.GetMovesForTurn(openLane, laneHasCard);

            Assert.Single(resolvedMoves);
            Assert.Equal(openLane, resolvedMoves[0].Lane);
        }
    }
}
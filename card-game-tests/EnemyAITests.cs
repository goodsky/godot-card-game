namespace Cardgame.Tests;

public class EnemyAITests
{
    private readonly ITestOutputHelper _output;

    public EnemyAITests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ResolveScriptedTurns()
    {
        var cards = new CardInfo[] {
            new CardInfo { Id = 0, Name = "0_Sacrifice", Attack = 0, Health = 1, BloodCost = CardBloodCost.Zero, Rarity = CardRarity.Sacrifice },
        };

        var cardPool = new CardPool(cards, "EnemyAITestsDeck");
        _output.WriteLine("TESTING");
    }
}
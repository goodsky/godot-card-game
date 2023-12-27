namespace CardGame.Test;

public class UnitTest1
{
    [Fact]
    public void Can_Load_Library()
    {
        var card = new Card("Test Card", new ResourceCost { Type = CostType.UnitSacrifice, Amount = 1 });

        Assert.Equal("Test Card", card.Name);
    }
}
namespace CardGame.Core;

public class Card
{
    public string Name { get; private set; }
    public string Avatar {get; private set; } // TODO: this does nothing yet...
    public ResourceCost Cost { get; private set; }

    public Card(string name, ResourceCost cost)
    {
        this.Name = name;
        this.Cost = cost;

        this.Avatar = "TODO";
    }
}
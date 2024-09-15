using System.Collections.Generic;

public class CardPool
{
    public string Name { get; private set; }

    public List<CardInfo> Cards { get; private set; }

    public CardPool(IEnumerable<CardInfo> cards, string name)
    {
        Name = name;
        Cards = new List<CardInfo>(cards);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Deck
{
    public string Name { get; private set; }

    public List<CardInfo> Cards { get; private set; } = new List<CardInfo>();

    public Deck(IEnumerable<CardInfo> cards, string name)
    {
        Name = name;
        Cards = new List<CardInfo>(cards);
        ShuffleCards();
    }

    public CardInfo DrawFromTop()
    {
        int drawIndex = Cards.Count - 1;
        CardInfo cardInfo = Cards[drawIndex];
        Cards.RemoveAt(drawIndex);
        return cardInfo;
    }

    private void ShuffleCards()
    {
        var swap = (int src, int dst) => {
            CardInfo temp = Cards[src];
            Cards[src] = Cards[dst];
            Cards[dst] = temp;
        };

        for (int i = Cards.Count - 1; i >= 1; i--)
        {
            int j = Random.Shared.Next(i + 1);
            swap(i, j);
        }
    }
}
using System;
using System.Collections.Generic;

public class Deck
{
    public List<CardInfo> Cards { get; private set; } = new List<CardInfo>();

    public int Count => Cards.Count;

    public Deck(IEnumerable<CardInfo> cards)
    {
        Cards = new List<CardInfo>(cards);
        ShuffleCards();
    }

    public CardInfo? PeekTop()
    {
        if (Count == 0)
        {
            return null;
        }

        int drawIndex = Cards.Count - 1;
        CardInfo cardInfo = Cards[drawIndex];
        return cardInfo;
    }

    public CardInfo DrawFromTop()
    {
        if (Count == 0)
        {
            return new CardInfo { Id = -1, Name = "ERROR", Attack = 0, Health = 0, AvatarResource = Constants.ErrorAvatarPath, BloodCost = CardBloodCost.Zero, Rarity = CardRarity.Rare };
        }

        int drawIndex = Cards.Count - 1;
        CardInfo cardInfo = Cards[drawIndex];
        Cards.RemoveAt(drawIndex);
        return cardInfo;
    }

    private void ShuffleCards()
    {
        var swap = (int src, int dst) =>
        {
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
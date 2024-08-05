using Godot;
using System;
using System.Collections.Generic;


public struct HandCardSlot
{
	public Card Card;
	public CardSlot CardSlot;
}

public partial class Hand : Node2D
{
	private List<HandCardSlot> _slots = new List<HandCardSlot>();

	public void AddCard(Card card)
	{
		
	}
}

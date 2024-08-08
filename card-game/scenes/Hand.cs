using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Hand : CardDrop
{
	private bool _isHoverOver = false;
	private Dictionary<Card, CollisionObject2D.InputEventEventHandler> _cardCallbacks = new Dictionary<Card, CollisionObject2D.InputEventEventHandler>(); 

	[Export]
	public int HandSize { get; set; }

    protected override int MaxCards => HandSize;

	public override bool TryAddCard(Card card)
	{
		if (base.TryAddCard(card))
		{
			_cardCallbacks[card] = (Node viewport, InputEvent inputEvent, long shape_idx) => Card_OnArea2DInputEvent(card, inputEvent);
			card.Area.InputEvent += _cardCallbacks[card];
			UpdateCardPositions();
			return true;
		}
		return false;
	}

	public override bool TryRemoveCard(Card card)
	{
		if (base.TryRemoveCard(card))
		{
			card.Area.InputEvent -= _cardCallbacks[card];
			_cardCallbacks.Remove(card);
			UpdateCardPositions();
			return true;
		}
		return false;
	}

    public override void _Process(double delta)
    {
        if (_isHoverOver)
		{
			// Card draggingCard = GetTree()
			// 	.GetNodesInGroup(Constants.DraggingCardGroup)
			// 	.Where(node => node is Card)
			// 	.Select(node => node as Card)
			// 	.FirstOrDefault();

			// if (draggingCard != null)
			// {

			// }
		}
    }

    public void Card_OnArea2DInputEvent(Card card, InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("click"))
		{
			card.StartDragging();
		}
	}

	public void HoverOver()
	{
		_isHoverOver = true;
	}

	public void HoverOut()
	{
		_isHoverOver = false;
	}

	public void Debug_DrawCard()
	{
		var card = Constants.CardScene.Instantiate<Card>();
		card.SetCardDrop(this);
	}

	private void UpdateCardPositions()
	{
		Card[] cards = GetChildCards();
		for (int i = 0; i < cards.Length; i++)
		{
			Card card = cards[i];
			var relativePosition = new Vector2(100 * i - (100 * cards.Length) / 2, 0);
			card.TargetPosition = GlobalPosition + relativePosition;
		}
	}
}

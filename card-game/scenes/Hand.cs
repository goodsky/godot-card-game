using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Hand : CardDrop
{
	private static readonly int DefaultCardSpacing = 85;
	private Vector2 _area;
	private bool _isHoverOver = false;
	private bool _hasGhostCard = false;
	private Dictionary<Card, CollisionObject2D.InputEventEventHandler> _cardCallbacks = new Dictionary<Card, CollisionObject2D.InputEventEventHandler>(); 

	[Export]
	public int HandSize { get; set; }

	[Export]
	public Area2D Area { get; set; }

    protected override int MaxCards => HandSize;

    public override void _Ready()
    {
        base._Ready();

		var rect = Area.GetNode<CollisionShape2D>("CollisionShape2D").Shape as RectangleShape2D;
		_area = rect.Size;
    }

    public override bool TryAddCard(Card card, Vector2? globalPosition)
	{
		if (base.TryAddCard(card, globalPosition))
		{
			int cardIndex = GetCardIndex(card, CardCount);
			// GD.Print($"Placing card in hand: GlobalPosition: {card.GlobalPosition}; LocalPosition: {ToLocal(card.GlobalPosition)}; CardCount: {CardCount}; HandIndex: {cardIndex};");
			CardsNode.MoveChild(card, cardIndex);

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
        if (_isHoverOver && CardManager.DraggingCard != null)
		{
			_hasGhostCard = true;
			UpdateCardPositions(CardManager.DraggingCard);
		}
		else if (_hasGhostCard)
		{
			_hasGhostCard = false;
			UpdateCardPositions();
		}
    }

    public void Card_OnArea2DInputEvent(Card card, InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("click"))
		{
			CardManager.StartDragging(card);
		}
	}

	public void HoverOver()
	{
		_isHoverOver = true;
		CardManager.ActivateCardDrop(this);
	}

	public void HoverOut()
	{
		_isHoverOver = false;
		CardManager.DeactivateCardDrop(this);
	}

	private static int DrawnCardCount = 0;
	public void Debug_DrawCard()
	{
		var card = Constants.CardScene.Instantiate<Card>();
		card.Name = $"Card_{DrawnCardCount++}";
		card.GlobalPosition = GlobalPosition + new Vector2(300, 0);

		GD.Print($"Drawing card {card.Name}");
		CardManager.SetCardDrop(card, this);
	}

	private void UpdateCardPositions(Card ghostCard = null)
	{
		Card[] cards = GetChildCards();
		int handSize = cards.Length;
		float spacePerCard = GetCardSpacing(handSize);

		int? ghostCardIndex = null;
		if (ghostCard != null)
		{
			if (CardsNode.IsAncestorOf(ghostCard))
			{
				// If the ghost card is already in the hand we don't need to make the hand larger
				// Move the existing hand cards around to make space for the dragging card.
				int currentCardIndex = ghostCard.GetIndex();
				int reorderCardIndex = GetCardIndex(ghostCard, handSize);
				if (currentCardIndex < reorderCardIndex)
				{
					for (int i = currentCardIndex; i < reorderCardIndex; i++)
					{
						cards[i] = cards[i + 1];
					}
				}
				else if (reorderCardIndex < currentCardIndex)
				{
					for (int i = currentCardIndex; i > reorderCardIndex; i--)
					{
						cards[i] = cards[i - 1];
					}
				}
				cards[reorderCardIndex] = ghostCard;
			}
			else
			{
				// If the ghost card is not a part of the hand already make a new gap for it.
				handSize += 1;
				spacePerCard = GetCardSpacing(handSize);
				ghostCardIndex = GetCardIndex(ghostCard, handSize);
			}
		}

		for (int i = 0; i < cards.Length; i++)
		{
			Card card = cards[i];
			int handIndex = i;
			if (ghostCardIndex.HasValue && ghostCardIndex.Value <= i)
			{
				handIndex += 1; // Leave space for the ghost card
			}

			var relativePosition = new Vector2(spacePerCard * handIndex - (spacePerCard * (handSize-1) / 2), 0);
			// GD.Print($"UpdateCardPosition[{i}]: SpacePerCard: {spacePerCard}; HandSize: {handSize}; HandIndex: {handIndex}; NewPos: {relativePosition}");
			card.TargetPosition = GlobalPosition + relativePosition;
		}
	}
	
	private float GetCardSpacing(int handSize)
	{
		if (handSize == 0)
		{
			return DefaultCardSpacing;
		}

		float spacePerCard = (_area.X - DefaultCardSpacing) / handSize;
		return Math.Min(spacePerCard, DefaultCardSpacing);
	}

	private int GetCardIndex(Card card, int handSize)
	{
		float spacePerCard = GetCardSpacing(handSize);
		Vector2 localPosition = ToLocal(card.GlobalPosition);
		int index = Mathf.FloorToInt((localPosition.X + (spacePerCard * handSize / 2)) / spacePerCard);
		return Math.Max(0, Math.Min(handSize - 1, index));
	}
}

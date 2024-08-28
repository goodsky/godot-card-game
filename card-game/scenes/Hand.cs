using System;
using System.Collections.Generic;
using Godot;

public partial class Hand : CardDrop
{
	private static readonly int DefaultCardSpacing = 110;
	private Vector2 _area;
	private bool _isHoverOver = false;
	private bool _hasGhostCard = false;
	private Dictionary<Card, HandCardCallbacks> _cardCallbacks = new Dictionary<Card, HandCardCallbacks>(); 

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

			_cardCallbacks.Add(card, new HandCardCallbacks(card, this));
			_cardCallbacks[card].AddCallbacks();

			UpdateCardPositions();
			return true;
		}
		return false;
	}

	public override bool TryRemoveCard(Card card)
	{
		if (base.TryRemoveCard(card))
		{
			_cardCallbacks[card]?.RemoveCallbacks();
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
		card.AddToGroup("DebugCard");
		card.Name = $"Card_{DrawnCardCount++}";
		card.GlobalPosition = GlobalPosition + new Vector2(300, 0);
		card.CardInfo = new CardInfo()
		{
			Name = "FooBar the Great!",
			Attack = Random.Shared.Next(1, 6),
			Defense = Random.Shared.Next(1, 11),
			BloodCost = Random.Shared.Next(1, 4),
		};

		GD.Print($"Drawing card {card.Name}");
		CardManager.SetCardDrop(card, this);
	}

	public void Debug_ClearCards()
	{
		var debugCards = GetTree().GetNodesInGroup("DebugCard");
		foreach (var card in debugCards)
		{
			CardManager.SetCardDrop(card as Card, null);
			card.QueueFree();
		}
	}

	protected void UpdateCardPositions(Card ghostCard = null)
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
				ReorderCardInHand(cards, currentCardIndex, reorderCardIndex);
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

	private void ReorderCardInHand(Card[] cards, int sourceIndex, int targetIndex)
	{
		Card reorderingCard = cards[sourceIndex];
		if (sourceIndex < targetIndex)
		{
			for (int i = sourceIndex; i < targetIndex; i++)
			{
				cards[i] = cards[i + 1];
			}
		}
		else if (targetIndex < sourceIndex)
		{
			for (int i = sourceIndex; i > targetIndex; i--)
			{
				cards[i] = cards[i - 1];
			}
		}
		cards[targetIndex] = reorderingCard;
	}

	private class HandCardCallbacks
	{
		// public static Card <-- keep track of which card is hovered over

		private static readonly float HoverOverOffset = 25f;
		private CollisionShape2D _areaShape;
		private Vector2 _defaultAreaSize;

		private Card _card;
		private Hand _hand;
		private CardManager _cardManager;

		public HandCardCallbacks(Card card, Hand hand)
		{
			_areaShape = card.Area.GetCollisionShape();
			_defaultAreaSize = card.Area.GetRectangleShape().Size;

			_card = card;
			_hand = hand;
			_cardManager = hand.CardManager;
		}

		public void AddCallbacks()
		{
			_card.Area.AreaStartDragging += StartDragging;
			_card.Area.AreaStopDragging += StopDragging;
			_card.Area.AreaMouseOver += StartHovering;
			_card.Area.AreaMouseOut += StopHovering;
		}

		public void RemoveCallbacks()
		{
			_card.Area.AreaStartDragging -= StartDragging;
			_card.Area.AreaStopDragging -= StopDragging;
			_card.Area.AreaMouseOver -= StartHovering;
			_card.Area.AreaMouseOut -= StopHovering;

			StopHovering();
		}

		public void StartDragging()
		{
			StopHovering();
			_card.StartDragging();
		}

		public void StopDragging()
		{
			_card.StopDragging();
		}

		private void StartHovering()
		{
			_card.ZIndex = 9;
			_card.TargetPositionOffset = new Vector2(0, -HoverOverOffset); // negative to hover up
			var areaRect = _areaShape.Shape as RectangleShape2D;
			areaRect.Size = _defaultAreaSize + new Vector2(0, HoverOverOffset); // positive to increase area size
			_areaShape.Position = new Vector2(0, HoverOverOffset / 2); // the rectangle is centered, so move the center halfway
		}

		private void StopHovering()
		{
			_card.ZIndex = 0;
			_card.TargetPositionOffset = null;
			var areaRect = _areaShape.Shape as RectangleShape2D;
			areaRect.Size = _defaultAreaSize;
			_areaShape.Position = new Vector2(0, 0);
		}
	}
}

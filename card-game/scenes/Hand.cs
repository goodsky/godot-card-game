using System;
using System.Collections.Generic;
using System.Linq;
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
        if (_isHoverOver && CardManager.Instance.DraggingCard != null)
		{
			_hasGhostCard = true;
			UpdateCardPositions(CardManager.Instance.DraggingCard);
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
		CardManager.Instance.ActivateCardDrop(this);
	}

	public void HoverOut()
	{
		_isHoverOver = false;
		CardManager.Instance.DeactivateCardDrop(this);
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
		// Keep track of which cards are hovered over, since when cards overlap in the hand it could be multiple.
		private static readonly List<HandCardCallbacks> HoveredOverCards = new List<HandCardCallbacks>();
		private static HandCardCallbacks GetTopCardFromList(List<HandCardCallbacks> callbacks)
		{
			if (callbacks.Count == 0) return null;
			if (callbacks.Count == 1) return callbacks[0];

			Hand hand = callbacks[0]._hand;
			Card[] childCards = hand.GetChildCards();

			int maxIndex = -1;
			HandCardCallbacks topCardCallbacks = null;
			foreach (HandCardCallbacks callback in callbacks)
			{
				int cardIndex = Array.IndexOf(childCards, callback._card);
				if (cardIndex > maxIndex)
				{
					maxIndex = cardIndex;
					topCardCallbacks = callback;
				}
			}

			return topCardCallbacks;
		}

		private static readonly float HoverOverOffset = 25f;
		private CollisionShape2D _areaShape;
		private Vector2 _defaultAreaSize;
		protected Hand _hand;
		protected Card _card;

		public HandCardCallbacks(Card card, Hand hand)
		{
			_areaShape = card.Area.GetCollisionShape();
			_defaultAreaSize = card.Area.GetRectangleShape().Size;

			_card = card;
			_hand = hand;
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
			if (CardManager.Instance.DraggingCard != null)
			{
				Card[] childCards = _hand.GetChildCards();
				int draggingCardIndex = Array.IndexOf(childCards, CardManager.Instance.DraggingCard);
				int myCardIndex = Array.IndexOf(childCards, _card);

				// GD.Print($"Already dragging card {CardManager.Instance.DraggingCard.Name}({draggingCardIndex}) while starting drag on {_card.Name}({myCardIndex})");
				if (draggingCardIndex > myCardIndex)
				{
					return;
				}
				else
				{
					CardManager.Instance.DraggingCard.StopDragging();
				}
			}

			HoverDown();
			_card.StartDragging();
		}

		public void StopDragging()
		{
			if (CardManager.Instance.DraggingCard == _card)
			{
				_card.StopDragging();
			}
		}

		public void StartHovering()
		{
			foreach (var callbacks in HoveredOverCards)
			{
				callbacks.HoverDown();
			}

			HoveredOverCards.Add(this);
			if (CardManager.Instance.DraggingCard == null)
			{
				GetTopCardFromList(HoveredOverCards)?.HoverUp();
			}
		}

		public void StopHovering()
		{
			foreach (var callbacks in HoveredOverCards)
			{
				callbacks.HoverDown();
			}

			HoveredOverCards.RemoveAll((callback) => callback == this);
			if (CardManager.Instance.DraggingCard == null)
			{
				GetTopCardFromList(HoveredOverCards)?.HoverUp();
			}
		}

		private void HoverUp()
		{
			if (_card.ZIndex == 0) _card.ZIndex = 9; // make sure we don't affect the ZIndex of the dragging card (which will be 10)
			_card.TargetPositionOffset = new Vector2(0, -HoverOverOffset); // negative to hover up
			var areaRect = _areaShape.Shape as RectangleShape2D;
			areaRect.Size = _defaultAreaSize + new Vector2(0, HoverOverOffset); // positive to increase area size
			_areaShape.Position = new Vector2(0, HoverOverOffset / 2); // the rectangle is centered, so move the center halfway
		}

		private void HoverDown()
		{
			if (_card.ZIndex == 9) _card.ZIndex = 0; // make sure we don't affect the ZIndex of the dragging card (which will be 10)
			_card.TargetPositionOffset = null;
			var areaRect = _areaShape.Shape as RectangleShape2D;
			areaRect.Size = _defaultAreaSize;
			_areaShape.Position = new Vector2(0, 0);
		}
	}
}

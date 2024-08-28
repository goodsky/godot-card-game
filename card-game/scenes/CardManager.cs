using System.Collections.Generic;
using Godot;

public static class CardManagerExtension
{
	private static CardManager _instance;
	private static object _instanceLock = new object();
	public static CardManager GetCardManager(this Node node)
	{
		if (_instance != null)
		{
			return _instance;
		}

		lock (_instanceLock)
		{
			if (_instance == null)
			{
				_instance = new CardManager(node.GetTree().Root);
			}
			return _instance;
		}
	}
}

public class CardManager
{
	private Node _rootNode;

	public Card DraggingCard { get; private set; } = null;

	public Card SelectedCard { get; private set; } = null;

	public CardDrop ActiveCardDrop { get; private set; } = null;

	public CardManager(Node rootNode)
	{
		_rootNode = rootNode;
	}

	public void ActivateCardDrop(CardDrop cardDrop)
	{
		if (ActiveCardDrop != null)
		{
			GD.Print($"Can't activate card drop. {ActiveCardDrop.Name} is already active.");
			return;
		}
		GD.Print($"Activated card drop: {cardDrop.Name}");
		ActiveCardDrop = cardDrop;
	}

	public void DeactivateCardDrop(CardDrop cardDrop)
	{
		if (ActiveCardDrop != cardDrop)
		{
			GD.Print($"Cannot deactivate card drop {cardDrop.Name}. Active card drop = {ActiveCardDrop?.Name}");
			return;
		}
		GD.Print($"Deactivated card drop: {cardDrop.Name}");
		ActiveCardDrop = null;
	}

	public void SetCardDrop(Card card, CardDrop cardDrop)
	{
		if (cardDrop != null && !cardDrop.CanDropCard())
		{
			GD.Print($"Can't drop card {card.Name} onto {cardDrop.Name}.");
			return;
		}

		Vector2 cardGlobalPosition = card.GlobalPosition;
		if (card.HomeCardDrop != null)
		{
			card.HomeCardDrop.TryRemoveCard(card);
		}

		card.HomeCardDrop = cardDrop;
		cardDrop?.TryAddCard(card, cardGlobalPosition);
	}

	public void SetDraggingCard(Card card)
	{
		if (DraggingCard != null)
		{
			GD.Print($"Can't start dragging card. {DraggingCard.Name} is already dragging.");
			return;
		}

		DraggingCard = card;
	}

	public void ClearDraggingCard(Card card)
	{
		if (DraggingCard != card)
		{
			GD.Print($"Can't stop dragging card {card.Name}. Dragging card = {DraggingCard?.Name}");
			return;
		}

		if (ActiveCardDrop != null)
		{
			SetCardDrop(card, ActiveCardDrop);
		}

		DraggingCard = null;
	}
}
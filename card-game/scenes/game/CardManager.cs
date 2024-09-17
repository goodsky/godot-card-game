using Godot;

public partial class CardManager : Node2D
{
	public static CardManager Instance { get; private set; }

	public Card DraggingCard { get; private set; } = null;

	public Card SelectedCard { get; private set; } = null;

	public CardDrop ActiveCardDrop { get; private set; } = null;

	public override void _Ready()
	{
		Instance = this;
	}

	public void ActivateCardDrop(CardDrop cardDrop)
	{
		// if (ActiveCardDrop != null)
		// {
		// 	GD.Print($"Can't activate card drop {cardDrop.Name}. {ActiveCardDrop.Name} is already active.");
		// 	return;
		// }

		ActiveCardDrop = cardDrop;
	}

	public void DeactivateCardDrop(CardDrop cardDrop)
	{
		if (ActiveCardDrop != cardDrop)
		{
			// GD.Print($"Skipping deactivate card drop {cardDrop.Name}. Active card drop = {ActiveCardDrop?.Name}");
			return;
		}

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

	public void SelectCard(Card card)
	{
		var oldSelectedCard = SelectedCard;
		SelectedCard = card;

		oldSelectedCard?.Unselect();
		card?.Select();
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
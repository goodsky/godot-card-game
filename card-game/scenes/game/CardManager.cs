using System.Collections.Generic;
using Godot;

public partial class CardManager : Node2D
{
	public static CardManager Instance { get; private set; }

	public Card DraggingCard { get; private set; } = null;

	public Card SelectedCard { get; private set; } = null;

	// Play a Card
	public Card StagedCard { get; private set; } = null;
	private CardDrop StagedCardOldHome = null;

	// Choose Sacrifices
	public List<Card> ProposedSacrifices { get; private set; } = new List<Card>();

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
		if (cardDrop != null && !cardDrop.CanDropCard(card))
		{
			GD.Print($"Can't drop card {card.Name} onto {cardDrop.Name}.");
			return;
		}

		Vector2 cardStartingGlobalPosition = card.GlobalPosition;
		card.HomeCardDrop?.TryRemoveCard(card);
		card.HomeCardDrop = null;

		if (cardDrop?.TryAddCard(card, cardStartingGlobalPosition) == false)
		{
			GD.PushError($"Failed to set card drop. {card.Name} could not be added to {cardDrop?.Name}");
		}
	}

	public void SelectCard(Card card)
	{
		var oldSelectedCard = SelectedCard;
		SelectedCard = card;

		oldSelectedCard?.Unselect();
		card?.Select();
	}

	public void StageCardPendingBloodCost(Card card, CardDrop oldHome)
	{
		StagedCard = card;
		StagedCardOldHome = oldHome;
	}

	public int AddSacrificeCard(Card card)
	{
		if (!ProposedSacrifices.Contains(card))
		{
			ProposedSacrifices.Add(card);
		}

		return ProposedSacrifices.Count;
	}

	public void RemoveSacrificeCard(Card card)
	{
		if (ProposedSacrifices.Contains(card))
		{
			ProposedSacrifices.Remove(card);
		}
	}

	public void ResolveStagedCard()
	{
		StagedCard = null;
		StagedCardOldHome = null;
		foreach (Card card in ProposedSacrifices)
		{
			RemoveSacrificeCard(card);
		}
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
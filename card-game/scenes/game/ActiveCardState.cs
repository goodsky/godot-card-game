using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class ActiveCardState : Node2D
{
	public static ActiveCardState Instance { get; private set; }

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

		if (cardDrop?.TryAddCard(card, cardStartingGlobalPosition) == false)
		{
			GD.PushError($"Failed to set card drop. {card.Name} could not be added to {cardDrop?.Name}");
		}

		if (cardDrop == null)
		{
			card.HomeCardDrop = null;
		}

		if (MainGame.Instance.CurrentState == GameState.IsaacMode)
		{
			MainGame.Instance.Isaac_CheckCards();
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
		Vector2 pendingPlayOffset = new Vector2(0, 75f);
		card.TargetPosition = card.HomeCardDrop.GlobalPosition + pendingPlayOffset;
		card.ZIndex = 10;

		StagedCard = card;
		StagedCardOldHome = oldHome;
	}

	public void CancelStagedCard()
	{
		if (StagedCard != null)
		{
			GD.Print($"Cancelled Staged Card. Reset {StagedCard.Name} to {StagedCardOldHome?.Name}");
			SetCardDrop(StagedCard, StagedCardOldHome);
			StagedCard = null;
			StagedCardOldHome = null;
		}
	}

	public int AddSacrificeCard(Card card)
	{
		if (!ProposedSacrifices.Contains(card))
		{
			ProposedSacrifices.Add(card);
			card.StopShaking();
			Task _ = card.RotateCard(Mathf.Pi / 2);

			MainGame.Instance.Board.UpdatePayThePrice(ProposedSacrifices.Count);

			if (ProposedSacrifices.Count >= (int)StagedCard.Info.BloodCost)
			{
				_resolveSacrificesCancellation?.Cancel();
				_resolveSacrificesCancellation = new CancellationTokenSource();
				Task __ = this.StartCoroutine(ResolveSacrificesThenPlayNewCard(), _resolveSacrificesCancellation.Token);
			}
		}

		return ProposedSacrifices.Count;
	}

	public void RemoveSacrificeCard(Card card)
	{
		if (ProposedSacrifices.Contains(card))
		{
			_resolveSacrificesCancellation?.Cancel();

			ProposedSacrifices.Remove(card);
			card.RotateCard(0).ContinueWith((t) => card.CallThreadSafe("StartShaking"));

			MainGame.Instance.Board.UpdatePayThePrice(ProposedSacrifices.Count);
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

	/** Coroutines */
	private CancellationTokenSource _resolveSacrificesCancellation = null;
	private IEnumerable ResolveSacrificesThenPlayNewCard()
	{
		yield return new CoroutineDelay(1.0);

		if (ProposedSacrifices.Count != (int)StagedCard.Info.BloodCost)
		{
			GD.PushError($"Sacrifices do not equal Blood Cost while resolving. {string.Join(";", ProposedSacrifices.Select(c => c.Name))} for {StagedCard.Info.BloodCost}");
		}

		foreach (Card card in ProposedSacrifices)
		{
			card.Kill();
		}
		ProposedSacrifices.Clear();
		_resolveSacrificesCancellation = null; // too late to stop now!

		yield return new CoroutineDelay(1.0);

		Card stagedCard = StagedCard;
		StagedCard = null;
		StagedCardOldHome = null;
		stagedCard.TargetPosition = stagedCard.HomeCardDrop.GlobalPosition;
		MainGame.Instance.CardCostPaid();

		yield return new CoroutineDelay(0.5);
		stagedCard.ZIndex = 0;
	}
}
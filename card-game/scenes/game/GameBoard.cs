using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GameBoard : Node2D
{
	private PlayArea[] _playerLanes => new[] { Lane0[0], Lane1[0], Lane2[0], Lane3[0] };
	private Dictionary<Card, SacrificeCardCallbacks> _sacrificeCallbacks = new Dictionary<Card, SacrificeCardCallbacks>();

	[Export]
	public PlayArea[] Lane0 { get; set; }

	[Export]
	public PlayArea[] Lane1 { get; set; }

	[Export]
	public PlayArea[] Lane2 { get; set; }

	[Export]
	public PlayArea[] Lane3 { get; set; }

	[Export]
	public BackgroundRenderer Background { get; set; }

	[Export]
	public CanvasItem PayCostPanel { get; set; }

	[Export]
	public CanvasItem[] PayBloodCostIcons { get; set; }

	public int PlayerCardCount => _playerLanes.Sum(lane => lane.CardCount > 0 ? 1 : 0);

	public override void _Input(InputEvent inputEvent)
	{
		bool clickedWithNoCardDrop = inputEvent.IsActionPressed(Constants.ClickEventName) &&
			ActiveCardState.Instance.SelectedCard != null && ActiveCardState.Instance.ActiveCardDrop == null;

		bool cancelClicked = inputEvent.IsActionPressed(Constants.RightClickEventName) || inputEvent.IsActionPressed(Constants.EscEventName);

		if (clickedWithNoCardDrop || cancelClicked)
		{
			// Clicks that don't have a card drop should clear the selected card.
			// Is there a better place for this responsibility to live?
			ActiveCardState.Instance.SelectCard(null);

			if (MainGame.Instance.CurrentState == GameState.PlayCard_PayPrice)
			{
				// We pretend the card cost was paid, even though it wasn't.
				// This works out because DisablePayThePrice cleans up the ActiveCardState for us.
				MainGame.Instance.CardCostPaid();
			}
		}
	}

	public void OnGameStateTransition(GameState nextState, GameState lastState)
	{
		switch (lastState)
		{
			case GameState.PlayCard:
				DisableLanes();
				break;

			case GameState.PlayCard_PayPrice:
				DisablePayThePrice();
				break;
		}

		switch (nextState)
		{
			case GameState.PlayCard:
				EnableLanes();
				break;

			case GameState.PlayCard_PayPrice:
				Card stagedCard = ActiveCardState.Instance.StagedCard;
				if (stagedCard == null)
				{
					GD.PushError($"[UnexpectedState] Transitioned to PayPrice without a StagedCard.");
					break;
				}
				InitializePayThePrice(stagedCard.CardInfo.BloodCost);
				break;

			default:
				DisableLanes();
				break;
		}
	}

	public bool CanPlayCardAtLocation(Card card, CardDrop cardDrop)
	{
		if (card == null || cardDrop == null) return false;
		if (MainGame.Instance.CurrentState == GameState.IsaacMode) return true;

		bool canAfford = PlayerCardCount >= (int)card.CardInfo.BloodCost;
		bool isEmptyPlayArea = cardDrop is PlayArea && cardDrop.CardCount == 0;
		bool isSacrificablePlayArea = cardDrop is PlayArea && (int)card.CardInfo.BloodCost > 0;

		return canAfford && (isEmptyPlayArea || isSacrificablePlayArea);
	}

	private void InitializePayThePrice(CardBloodCost cost)
	{
		PayCostPanel.Visible = true;
		for (int i = 0; i < PayBloodCostIcons.Length; i++)
		{
			PayBloodCostIcons[i].Visible = (i < (int)cost);
			PayBloodCostIcons[i].Modulate = new Color(1, 1, 1, 0.5f);
		}

		foreach (PlayArea lane in _playerLanes)
		{
			Card laneCard = lane.GetChildCards().FirstOrDefault();
			if (laneCard != null && laneCard != ActiveCardState.Instance.StagedCard)
			{
				var callbacks = new SacrificeCardCallbacks(laneCard);
				_sacrificeCallbacks[laneCard] = callbacks;
				callbacks.AddCallbacks();
			}
		}
	}

	public void UpdatePayThePrice(int cardsPaid)
	{
		for (int i = 0; i < PayBloodCostIcons.Length; i++)
		{
			PayBloodCostIcons[i].Modulate = (i < cardsPaid) ? new Color(1, 1, 1, 1.0f) : new Color(1, 1, 1, 0.5f);
		}
	}

	private void DisablePayThePrice()
	{
		PayCostPanel.Visible = false;

		ActiveCardState.Instance.CancelStagedCard();

		foreach (PlayArea lane in _playerLanes)
		{
			Card laneCard = lane.GetChildCards().FirstOrDefault();
			if (laneCard != null && _sacrificeCallbacks.TryGetValue(laneCard, out SacrificeCardCallbacks callbacks))
			{
				callbacks.RemoveCallbacks();
				ActiveCardState.Instance.RemoveSacrificeCard(callbacks.Card);
			}
		}

		_sacrificeCallbacks.Clear();
	}

	private void DisableLanes()
	{
		foreach (var lane in _playerLanes)
		{
			lane.SupportsDrop = false;
		}
	}

	private void EnableLanes()
	{
		foreach (var lane in _playerLanes)
		{
			lane.SupportsDrop = true;
		}
	}

	private class SacrificeCardCallbacks
	{
		private bool _isSelected = false;
		public Card Card;

		public SacrificeCardCallbacks(Card card)
		{
			Card = card;
		}

		public void AddCallbacks()
		{
			Card.Area.AreaClicked += Select;
			Card.Area.AreaMouseOver += StartHovering;
			Card.Area.AreaMouseOut += StopHovering;

			Card.StartShaking();
		}

		public void RemoveCallbacks()
		{
			if (Card.IsQueuedForDeletion())
			{
				return;
			}

			Card.Area.AreaClicked -= Select;
			Card.Area.AreaMouseOver -= StartHovering;
			Card.Area.AreaMouseOut -= StopHovering;

			Card.StopShaking();
			StopHovering();
		}

		public void Select()
		{
			int selectedSacrifices = ActiveCardState.Instance.ProposedSacrifices.Count;
			int cost = (int)ActiveCardState.Instance.StagedCard.CardInfo.BloodCost;

			bool isInSameLaneAsPlayingCard = ActiveCardState.Instance.StagedCard.HomeCardDrop == Card.HomeCardDrop;

			if (_isSelected && !isInSameLaneAsPlayingCard)
			{
				_isSelected = false;
				Card.Modulate = Colors.Red;
				ActiveCardState.Instance.RemoveSacrificeCard(Card);
			}
			else if (selectedSacrifices < cost)
			{
				_isSelected = true;
				Card.Modulate = Colors.White;
				ActiveCardState.Instance.AddSacrificeCard(Card);
			}
		}

		public void StartHovering()
		{
			if (!_isSelected)
			{
				Card.Modulate = Colors.Red;
			}
		}

		public void StopHovering()
		{
			Card.Modulate = Colors.White;
		}
	}
}

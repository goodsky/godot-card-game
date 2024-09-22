using System.Linq;
using Godot;

public partial class GameBoard : Node2D
{
	private PlayArea[] _playerLanes => new[] { Lane0[0], Lane1[0], Lane2[0], Lane3[0] };

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
			CardManager.Instance.SelectedCard != null && CardManager.Instance.ActiveCardDrop == null;

		bool cancelClicked = inputEvent.IsActionPressed(Constants.RightClickEventName) || inputEvent.IsActionPressed(Constants.EscEventName);

		if (clickedWithNoCardDrop || cancelClicked)
		{
			// Clicks that don't have a card drop should clear the selected card.
			// Is there a better place for this responsibility to live?
			CardManager.Instance.SelectCard(null);
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
				Card stagedCard = CardManager.Instance.StagedCard;
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
		}

		foreach (PlayArea lane in _playerLanes)
		{
			Card laneCard = lane.GetChildCards().FirstOrDefault();
			if (laneCard != null)
			{
				laneCard.StartShaking();
			}
		}
	}

	private void DisablePayThePrice()
	{
		PayCostPanel.Visible = false;

		foreach (PlayArea lane in _playerLanes)
		{
			Card laneCard = lane.GetChildCards().FirstOrDefault();
			if (laneCard != null)
			{
				laneCard.StopShaking();
			}		}
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
}

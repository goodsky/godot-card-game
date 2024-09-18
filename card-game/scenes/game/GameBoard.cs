using Godot;

public partial class GameBoard : Node2D
{
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

	public int PlayerCardCount =>
		Lane0[0].CardCount +
		Lane1[0].CardCount +
		Lane2[0].CardCount +
		Lane3[0].CardCount;

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
		switch (nextState)
		{
			case GameState.PlayCard_SelectCard:
			case GameState.PlayCard_SelectLocation:
				EnableLanes();
				break;

			default:
				DisableLanes();
				break;
		}
	}

	public bool CanPlayCardAtLocation(Card card, CardDrop cardDrop)
	{
		if (card == null || cardDrop == null) return false;

		bool canAfford = PlayerCardCount >= (int)card.CardInfo.BloodCost;
		bool isEmptyPlayArea = cardDrop is PlayArea && cardDrop.CardCount == 0;
		bool isSacrificablePlayArea = cardDrop is PlayArea && (int)card.CardInfo.BloodCost > 0;
		
		return canAfford && (isEmptyPlayArea || isSacrificablePlayArea);
	}

	private void DisableLanes()
	{
		Lane0[0].SupportsDrop = false;
		Lane1[0].SupportsDrop = false;
		Lane2[0].SupportsDrop = false;
		Lane3[0].SupportsDrop = false;
	}

	private void EnableLanes()
	{
		Lane0[0].SupportsDrop = true;
		Lane1[0].SupportsDrop = true;
		Lane2[0].SupportsDrop = true;
		Lane3[0].SupportsDrop = true;
	}
}

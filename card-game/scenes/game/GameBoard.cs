using Godot;
using System;

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

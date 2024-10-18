using Godot;
using System.Collections.Generic;

public partial class DeckPopUp : Control
{
	[Export]
	public CanvasItem BackgroundFade { get; set; }

	[Export]
	public Container CardContainer { get; set; }

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed(Constants.EscEventName))
		{
			Click_Cancel();
		}
	}

	public static void PopUp(Node root, IEnumerable<CardInfo> deckCards, bool fadeBackground = false)
	{
		GD.Print("Pop up Deck Dialog");
		var deckPopUp = Constants.DeckPopUp.Instantiate<DeckPopUp>();
		deckPopUp.BackgroundFade.Visible = fadeBackground;

		foreach (var cardInfo in deckCards)
		{
			var card = Constants.CardButtonScene.Instantiate<CardButton>();
			card.ShowCardBack = false;
			card.SetDisabled(true, fade: false);
			card.SetCard(cardInfo);
			deckPopUp.CardContainer.AddChild(card);
		}

		root.AddChild(deckPopUp);
	}

	private void Click_Cancel()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		QueueFree();
	}

	public void HoverOverButton()
	{
		AudioManager.Instance.Play(Constants.Audio.BalloonSnap, pitch: 1.0f, volume: 0.5f);
	}
}

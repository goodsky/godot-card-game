using Godot;
using System;
using System.Linq;

public partial class MainMenu : Control
{
	private CardPool[] _cards;
	private CardPool _selectedCards;

	[Export]
	public Control MainButtons { get; set; }

	[Export]
	public Control SelectCards { get; set; }

	public override void _Ready()
	{
		Button[] allButtons = FindChildren("*Button").Select(x => x as Button).Where(x => x != null).ToArray();
		foreach (Button button in allButtons)
		{
			button.MouseEntered += () => HoverOverButton(button);
		}

		if (OS.IsDebugBuild())
		{
			GameLoader.Debug_TestEndToEnd();
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("ui_cancel"))
		{
			Click_Cancel();
		}
	}

	public void Click_BuildCards()
	{

	}

	public void Click_PlayGame()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		OpenSelectDeckDialog();
	}

	public void Click_DeckListItem(int index)
	{
		GD.Print("Selected card pool ", index);
		_selectedCards = _cards[index];

		Button startButton = SelectCards
			.GetChildren()
			.Where(c => c is Button && c.Name == "StartGameButton")
			.Select(c => c as Button)
			.FirstOrDefault();

		startButton.Disabled = false;
	}

	public void Click_StartGame()
	{
		if (SceneLoader.Instance != null)
		{
			AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);

			// TODO: Card Pool vs. Deck
			var sacrificeCards = _selectedCards.Cards.Where(c => c.Rarity == CardRarity.Sacrifice);
			var creatureCards = _selectedCards.Cards.Where(c => c.Rarity != CardRarity.Sacrifice);
			var sacrificeDeck = new Deck(sacrificeCards, "Sacrifices");
			var creatureDeck = new Deck(creatureCards, "Creatures");
			SceneLoader.Instance.LoadMainGame(sacrificeDeck, creatureDeck);
		}
	}

	public void Click_Cancel()
	{
		if (SelectCards.Visible)
		{
			AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
			OpenMainDialog();
		}
	}

	public void Click_Quit()
	{
		GetTree().Quit();
	}

	public void HoverOverButton(Button button)
	{
		if (!button.Disabled)
		{
			AudioManager.Instance.Play(Constants.Audio.HoverSnap, pitch: 1.0f, volume: 0.5f);
		}
	}

	private void OpenMainDialog()
	{
		SelectCards.Visible = false;
		MainButtons.Visible = true;
	}

	private void OpenSelectDeckDialog()
	{
		ItemList cardsList = SelectCards
			.GetChildren()
			.Where(c => c is ItemList)
			.Select(c => c as ItemList)
			.FirstOrDefault();

		Button startButton = SelectCards
			.GetChildren()
			.Where(c => c is Button && c.Name == "StartGameButton")
			.Select(c => c as Button)
			.FirstOrDefault();

		_cards = GameLoader.GetAvailableCardPools().Select(t => t.cards).ToArray();
		cardsList.Clear();
		foreach (var cardPool in _cards)
		{
			cardsList.AddItem(cardPool.Name);
			cardsList.SetItemTooltipEnabled(cardsList.ItemCount - 1, false);
		}

		startButton.Disabled = true;

		MainButtons.Visible = false;
		SelectCards.Visible = true;
	}
}

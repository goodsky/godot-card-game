using Godot;
using System;
using System.Linq;

public partial class MainMenu : Control
{
	private Deck[] _decks;
	private Deck _selectedDeck;

	[Export]
	public Control MainButtons { get; set; }

	[Export]
	public Control SelectDeck { get; set; }

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("ui_cancel"))
		{
			Click_Cancel();
		}
	}

	public void Click_BuildADeck()
	{
		
	}

	public void Click_PlayGame()
	{
		OpenSelectDeckDialog();
	}

	public void Click_DeckListItem(int index)
	{
		GD.Print("Selected deck ", index);
		_selectedDeck = _decks[index];

		Button startButton = SelectDeck
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
			SceneLoader.Instance.LoadMainGame(_selectedDeck);
		}
	}

	public void Click_Cancel()
	{
		if (SelectDeck.Visible)
		{
			OpenMainDialog();
		}
	}

	public void Click_Quit()
	{
		GetTree().Quit();
	}

	private void OpenMainDialog()
	{
		SelectDeck.Visible = false;
		MainButtons.Visible = true;
	}

	private void OpenSelectDeckDialog()
	{
		ItemList deckList = SelectDeck
			.GetChildren()
			.Where(c => c is ItemList)
			.Select(c => c as ItemList)
			.FirstOrDefault();

		Button startButton = SelectDeck
			.GetChildren()
			.Where(c => c is Button && c.Name == "StartGameButton")
			.Select(c => c as Button)
			.FirstOrDefault();
		
		_decks = DeckLoader.GetAvailableDecks().Select(t => t.deck).ToArray();
		deckList.Clear();
		foreach (var deck in _decks)
		{
			deckList.AddItem(deck.Name);
			deckList.SetItemTooltipEnabled(deckList.ItemCount - 1, false);
		}

		startButton.Disabled = true;

		MainButtons.Visible = false;
		SelectDeck.Visible = true;
	}
}

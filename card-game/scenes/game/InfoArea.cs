using Godot;
using System;

public partial class InfoArea : Node2D
{
	public static InfoArea Instance { get; private set; }

	// who set the infobar content? Used when resetting the info to avoid unexpected resets.
	private object _infoOwner;

	/**
	In Isaac mode - there is not game state - so ignore everything here
	*/
	[Export]
	public bool IsaacMode { get; set; } = false;

	[Export]
	public RichTextLabel InfoLabel { get; set; }

	[Export]
	public Label TurnLabel { get; set; }

	[Export]
	public Label GameStateDescriptionLabel { get; set; }

	[Export]
	public Button DrawFromDeckButton { get; set; }

	[Export]
	public Button DrawFromSacrificeButton { get; set; }

	[Export]
	public Button EndTurnButton { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
	{
		Instance = this;
		DrawFromDeckButton.Text = $"Draw Creature ({MainGame.Instance.Creatures?.Count})";
		DrawFromSacrificeButton.Text = $"Draw Sacrifice ({MainGame.Instance.Sacrifices?.Count})";
	}

	public void OnGameStateTransition(GameState nextState, GameState lastState)
	{
		switch (lastState)
		{
			case GameState.DrawCard:
				DrawFromDeckButton.Visible = false;
				DrawFromSacrificeButton.Visible = false;
				break;

			case GameState.PlayCard:
				EndTurnButton.Visible = false;
				break;
		}

		switch (nextState)
		{
			case GameState.DrawCard:
				TurnLabel.Text = "Your Turn";
				GameStateDescriptionLabel.Text = "Choose a card";
				DrawFromDeckButton.Visible = true;
				DrawFromDeckButton.Disabled = MainGame.Instance.Creatures.Count == 0;
				DrawFromDeckButton.Text = $"Draw Creature ({MainGame.Instance.Creatures.Count})";
				DrawFromSacrificeButton.Visible = true;
				DrawFromSacrificeButton.Disabled = MainGame.Instance.Sacrifices.Count == 0;
				DrawFromSacrificeButton.Text = $"Draw Sacrifice ({MainGame.Instance.Sacrifices.Count})";
				break;

			case GameState.PlayCard:
				TurnLabel.Text = "Your Turn";
				GameStateDescriptionLabel.Text = "Play a Card\nor\nEnd your Turn";
				EndTurnButton.Visible = true;
				break;
		}
	}

	public void SetInfoBar(string message, object owner = null)
	{
		if (IsaacMode) return;
		InfoLabel.Text = message;
		_infoOwner = owner;
	}

	public void ResetInfoBar(object owner = null)
	{
		if (IsaacMode) return;
		if (owner != null && owner != _infoOwner) return;

		InfoLabel.Text = "\n\n\n\n\n\n\n\n\n\n[center][font_size=16][i]Hover over things to get helpful tips[/i][/font_size][/center]";
		_infoOwner = null;
	}

	public void SetGameState(string stateName, string stateDescription)
	{
		if (IsaacMode) return;
		TurnLabel.Text = stateName;
		GameStateDescriptionLabel.Text = stateDescription;
	}

	public void Click_MenuBar()
	{
		GD.Print("Menu Bar Clicked");
	}

	public void MouseEntered_DrawFromDeckButton()
	{
		SetInfoBar("\n\n\n\n\n\n\n\n[font_size=16]Draw from Deck[/font_size]\nDraw a random card from your deck.");
	}

	public void MouseEntered_DrawSacrificeButton()
	{
		SetInfoBar("\n\n\n\n\n\n\n\n\n[font_size=16]Draw a Sacrifice[/font_size]\nDraw a 0 cost card for sacrificing.");
	}

	public void MouseEntered_EndTurnButton()
	{
		SetInfoBar("\n\n\n\n\n\n\n\n\n[font_size=16]End your Turn[/font_size]\nEnd your turn and move to the combat phase.");
	}

	public void MouseExited_ResetInfo()
	{
		ResetInfoBar();
	}
}

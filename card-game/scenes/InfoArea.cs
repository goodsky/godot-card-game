using Godot;
using System;

public partial class InfoArea : Node2D
{
	public static InfoArea Instance { get; private set; }

    /**
	In Isaac mode - there is not game state - so ignore everything here
	*/
	[Export]
	public bool IsaacMode { get; set; } = false;

	[Export]
	public RichTextLabel InfoLabel { get; set; }

	[Export]
	public Label GameStateLabel { get; set; } 

	[Export]
	public Label GameStateDescriptionLabel { get; set; } 

	[Export]
	public Button DrawFromDeckButton { get; set; }

	[Export]
	public Button DrawFromSacrificeButton { get; set; }

	public override void _Ready()
	{
		Instance = this;
	}

	public void SetInfoBar(string message)
	{
		if (IsaacMode) return;
		InfoLabel.Text = message;
	}

	public void ResetInfoBar()
	{
		if (IsaacMode) return;
		InfoLabel.Text = "\n\n\n\n\n\n\n\n\n\n[center][font_size=16][i]Hover over things to get helpful tips[/i][/font_size][/center]";
	}

	public void SetGameState(string stateName, string stateDescription)
	{
		if (IsaacMode) return;
		GameStateLabel.Text = stateName;
		GameStateDescriptionLabel.Text = stateDescription;
	}

	public void Click_MenuBar()
	{
		GD.Print("Menu Bar Clicked");
	}

	public void MouseEntered_DrawFromDeckButton()
	{
		SetInfoBar("Draw a random card from your deck.");
	}

	public void MouseEntered_DrawSacrificeButton()
	{
		SetInfoBar("Draw a 0 cost card for sacrificing.");
	}

	public void MouseExited_ResetInfo()
	{
		ResetInfoBar();
	}
}

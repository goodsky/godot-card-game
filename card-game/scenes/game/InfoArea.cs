using Godot;
using System;
using System.Text;

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
	public Label LevelLabel { get; set; }

	[Export]
	public Label GameStateLabel { get; set; }

	[Export]
	public Label GameStateDescriptionLabel { get; set; }

	[Export]
	public Label DrawFromDeckLabel { get; set; }

	[Export]
	public CardButton DrawFromDeckButton { get; set; }

	[Export]
	public Label DrawFromSacrificeLabel { get; set; }

	[Export]
	public CardButton DrawFromSacrificeButton { get; set; }

	[Export]
	public Button EndTurnButton { get; set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		Instance = this;
		
		if (GameManager.Instance?.Progress != null)
		{
			LevelLabel.Text = $"Level {GameManager.Instance.Progress.Level}";
		}

		if (DrawFromDeckButton != null)
		{
			DrawFromDeckButton.SetCard(MainGame.Instance.Creatures.PeekTop());
			DrawFromDeckButton.Pressed += MainGame.Instance.DrawCardFromDeck;
			DrawFromDeckButton.MouseEntered += SetDrawFromDeckCardInfo;
			DrawFromDeckButton.MouseExited += ResetDrawFromDeckCardInfo;
			DrawFromDeckLabel.Text = $"Creatures ({MainGame.Instance.Creatures?.Count})";
		}

		if (DrawFromSacrificeButton != null)
		{
			DrawFromSacrificeButton.SetCard(MainGame.Instance.Sacrifices.PeekTop());
			DrawFromSacrificeButton.Pressed += MainGame.Instance.DrawCardFromSacrificeDeck;
			DrawFromSacrificeButton.MouseEntered += SetDrawFromSacrificesCardInfo;
			DrawFromSacrificeButton.MouseExited += ResetDrawFromSacrificesCardInfo;
			DrawFromSacrificeLabel.Text = $"Sacrifices ({MainGame.Instance.Sacrifices?.Count})";

			if (MainGame.Instance.Sacrifices?.Count == 0)
			{
				DrawFromSacrificeButton.SetCard(null);
			}
		}
	}

	public void OnGameStateTransition(GameState nextState, GameState lastState)
	{
		switch (lastState)
		{
			case GameState.DrawCard:
				if (MainGame.Instance.Creatures.Count == 0 &&
					MainGame.Instance.Sacrifices.Count == 0)
				{
					GD.Print("No cards to draw! Continue on to combat.");
					MainGame.Instance.SkipDrawingCard();
				}

				DrawFromDeckButton.SetDisabled(true);
				DrawFromDeckButton.SetCard(MainGame.Instance.Creatures.PeekTop());
				DrawFromDeckLabel.Text = $"Creatures ({MainGame.Instance.Creatures?.Count})";
				DrawFromSacrificeButton.SetDisabled(true);
				DrawFromSacrificeButton.SetCard(MainGame.Instance.Sacrifices.PeekTop());
				DrawFromSacrificeLabel.Text = $"Sacrifices ({MainGame.Instance.Sacrifices?.Count})";
				break;

			case GameState.PlayCard:
				EndTurnButton.Disabled = true;
				break;
		}

		switch (nextState)
		{
			case GameState.DrawCard:
				GameStateLabel.Text = "Your Turn";
				GameStateDescriptionLabel.Text = "Choose a card";
				DrawFromDeckButton.SetDisabled(false);
				DrawFromDeckButton.SetCard(MainGame.Instance.Creatures.PeekTop());
				DrawFromDeckLabel.Text = $"Creatures ({MainGame.Instance.Creatures?.Count})";
				DrawFromSacrificeButton.SetDisabled(false);
				DrawFromSacrificeButton.SetCard(MainGame.Instance.Sacrifices.PeekTop());
				DrawFromSacrificeLabel.Text = $"Sacrifices ({MainGame.Instance.Sacrifices?.Count})";
				break;

			case GameState.PlayCard:
				GameStateLabel.Text = "Your Turn";
				GameStateDescriptionLabel.Text = "Play cards then end turn";
				EndTurnButton.Disabled = false;
				break;

			case GameState.PlayerCombat:
				GameStateLabel.Text = "Your Turn";
				GameStateDescriptionLabel.Text = "Your cards attack";
				break;

			case GameState.EnemyPlayCard:
				GameStateLabel.Text = "Enemy Turn";
				GameStateDescriptionLabel.Text = "Playing new cards";
				break;

			case GameState.EnemyCombat:
				GameStateLabel.Text = "Enemy Turn";
				GameStateDescriptionLabel.Text = "Opponent cards attack";
				break;

			case GameState.EnemyStageCard:
				GameStateLabel.Text = "Get Ready";
				GameStateDescriptionLabel.Text = "Your turn is next";
				break;
		}
	}

	public void SetInfoBar(string message, object owner = null)
	{
		if (IsaacMode) return;
		InfoLabel.Text = message;
		_infoOwner = owner;
	}

	public void SetCardInfo(CardInfo cardInfo, object owner = null)
	{
		var infoStr = new StringBuilder();
		infoStr.AppendLine($"[center][font_size=16]{cardInfo.Name}[/font_size][/center]");
		infoStr.AppendLine("");
		infoStr.AppendLine($"Attack: {cardInfo.Attack}");
		infoStr.AppendLine($"Defense: {cardInfo.Health}");
		infoStr.AppendLine($"Cost: {cardInfo.BloodCost}");
		infoStr.AppendLine($"Rarity: {cardInfo.Rarity}");
		SetInfoBar(infoStr.ToString(), owner);
	}

	public void ResetInfoBar(object owner = null)
	{
		if (IsaacMode) return;
		if (owner != null && owner != _infoOwner) return;

		InfoLabel.Text = "\n\n\n[center][font_size=16][i]Hover over things to get helpful tips[/i][/font_size][/center]";
		_infoOwner = null;
	}

	public void SetGameState(string stateName, string stateDescription)
	{
		if (IsaacMode) return;
		GameStateLabel.Text = stateName;
		GameStateDescriptionLabel.Text = stateDescription;
	}

	public void Click_MenuBar()
	{
		SettingsPopUp.PopUp(MainGame.Instance.PopUpParent, fadeBackground: false, showMainMenuButton: true);
	}

	public void MouseEntered_DrawFromDeckButton()
	{
		SetInfoBar("\n\n\n[font_size=16]Draw from Deck[/font_size]\nDraw a random card from your deck.");
	}

	public void MouseEntered_DrawSacrificeButton()
	{
		SetInfoBar("\n\n\n[font_size=16]Draw a Sacrifice[/font_size]\nDraw a 0 cost card for sacrificing.");
	}

	public void MouseEntered_EndTurnButton()
	{
		SetInfoBar("\n\n\n[font_size=16]End your Turn[/font_size]\nEnd your turn and move to the combat phase.");
	}

	public void MouseExited_ResetInfo()
	{
		ResetInfoBar();
	}

	private void SetDrawFromDeckCardInfo()
	{
		if (DrawFromDeckButton.Disabled || DrawFromDeckButton.ShowCardBack || !DrawFromDeckButton.Info.HasValue) return;

		SetCardInfo(DrawFromDeckButton.Info.Value, DrawFromDeckButton);
	}

	private void ResetDrawFromDeckCardInfo()
	{
		ResetInfoBar(DrawFromDeckButton);
	}

	private void SetDrawFromSacrificesCardInfo()
	{
		if (DrawFromSacrificeButton.Disabled || DrawFromSacrificeButton.ShowCardBack || !DrawFromSacrificeButton.Info.HasValue) return;

		SetCardInfo(DrawFromSacrificeButton.Info.Value, DrawFromSacrificeButton);
	}

	private void ResetDrawFromSacrificesCardInfo()
	{
		ResetInfoBar(DrawFromSacrificeButton);
	}
}

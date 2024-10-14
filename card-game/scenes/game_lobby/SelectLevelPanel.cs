using System;
using System.Collections.Generic;
using Godot;

public partial class SelectLevelPanel : PanelContainer
{
	private LevelDifficulty _difficulty;
	private LevelReward _reward;
	private Action _onSelected;

	public override void _Ready()
	{
		var difficultyBubbles = new Dictionary<LevelDifficulty, CanvasItem>
		{
			{ LevelDifficulty.Easy, FindChild("Easy") as CanvasItem },
			{ LevelDifficulty.Medium, FindChild("Medium") as CanvasItem },
			{ LevelDifficulty.Hard, FindChild("Hard") as CanvasItem },
		};

		var rewardBubbles = new Dictionary<LevelReward, CanvasItem>
		{
			{ LevelReward.AddResource, FindChild("AddResource") as CanvasItem },
			{ LevelReward.AddCreature, FindChild("NewCard") as CanvasItem },
			{ LevelReward.AddUncommonCreature, FindChild("NewCardUncommon") as CanvasItem },
			{ LevelReward.AddRareCreature, FindChild("NewCardRare") as CanvasItem },
			{ LevelReward.RemoveCard, FindChild("RemoveCard") as CanvasItem },
			{ LevelReward.IncreaseHandSize, FindChild("HandSize") as CanvasItem },
		};

		var button = FindChild("Button") as BaseButton;

		foreach (var item in difficultyBubbles.Values)
		{
			item.Visible = false;
		}

		foreach (var item in rewardBubbles.Values)
		{
			item.Visible = false;
		}

		difficultyBubbles[_difficulty].Visible = true;
		rewardBubbles[_reward].Visible = true;

		button.Pressed += _onSelected;
		button.Pressed += () => AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		button.MouseEntered += () => HoverOverButton();
	}

	// Must call this before adding to the scene tree!
	public void Configure(LevelDifficulty difficulty, LevelReward reward, Action onSelected)
	{
		_difficulty = difficulty;
		_reward = reward;
		_onSelected = onSelected;
	}

	public void HoverOverButton()
	{
		AudioManager.Instance.Play(Constants.Audio.BalloonSnap, pitch: 1.0f, volume: 0.5f);
	}
}

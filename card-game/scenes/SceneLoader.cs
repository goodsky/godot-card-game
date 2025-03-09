using System;
using Godot;

public partial class SceneLoader : Node2D
{
	public static SceneLoader Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		LoadMainMenu();
	}

	public void LoadMainMenu()
	{
		RemoveAllChildren();
		var mainMenu = Constants.MainMenuScene.Instantiate<MainMenu>();
		AddChild(mainMenu);

		AudioManager.Instance.PlayMusic(Constants.Audio.Music_Lobby);
	}

	public void LoadGameLobby(bool startNewGame = false)
	{
		// Reset the random seed to the saved version
		GameManager.Instance.RefreshSavedGame();

		RemoveAllChildren();
		var gameLobby = Constants.GameLobbyScene.Instantiate<GameLobby>();
		gameLobby.IsNewGame = startNewGame;
		AddChild(gameLobby);

		AudioManager.Instance.StopMusic();
	}

	public void LoadMainGame(Deck sacrifices, Deck creatures, GameLevel level)
	{
		RemoveAllChildren();

		MainGame mainGame = Constants.MainGameScene.Instantiate<MainGame>();
		mainGame.Sacrifices = sacrifices;
		mainGame.Creatures = creatures;
		mainGame.GameLevel = level;
		AddChild(mainGame);

		var musicOptions = new[] { Constants.Audio.Music_Game1, Constants.Audio.Music_Game2 };
		var music = musicOptions[Random.Shared.Next(0, musicOptions.Length)];
		AudioManager.Instance.PlayMusic(music);
	}

	public void LoadTestBench()
	{
		RemoveAllChildren();
		TestBench testBench = Constants.TestBenchScene.Instantiate<TestBench>();
		AddChild(testBench);

		AudioManager.Instance.StopMusic();
	}

	public void LoadIsaacMode()
	{
		RemoveAllChildren();
		MainGame mainGame = Constants.IsaaacModeScene.Instantiate<MainGame>();
		AddChild(mainGame);
		
		AudioManager.Instance.StopMusic();
	}

	private void RemoveAllChildren()
	{
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
	}
}

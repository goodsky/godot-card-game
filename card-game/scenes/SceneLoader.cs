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
	}

	public void LoadGameLobby(bool startNewGame = false)
	{
		// Reset the random seed to the saved version
		GameManager.Instance.RefreshSavedGame();

		RemoveAllChildren();
		var gameLobby = Constants.GameLobbyScene.Instantiate<GameLobby>();
		gameLobby.IsNewGame = startNewGame;
		AddChild(gameLobby);
	}

	public void LoadMainGame(Deck sacrifices, Deck creatures, GameLevel level)
	{
		RemoveAllChildren();
		MainGame mainGame = Constants.MainGameScene.Instantiate<MainGame>();
		mainGame.Sacrifices = sacrifices;
		mainGame.Creatures = creatures;
		mainGame.GameLevel = level;
		AddChild(mainGame);
	}

	private void RemoveAllChildren()
	{
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
	}
}

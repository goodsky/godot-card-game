using Godot;

public enum GameScenes
{
	Menu,
	Game,
}

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

		var mainMenu = Constants.MainMenuScene.Instantiate() as MainMenu;

		AddChild(mainMenu);
		mainMenu.Position = Vector2.Zero;
	}

	public void LoadMainGame(Deck deck)
	{
		RemoveAllChildren();

		MainGame mainGame = Constants.MainGameScene.Instantiate<MainGame>();
		mainGame.Deck = deck;
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

using Godot;
using System;

public partial class MainMenu : Control
{
	public void Click_BuildADeck()
	{

	}

	public void Click_PlayGame()
	{
		if (SceneLoader.Instance != null)
		{
			SceneLoader.Instance.LoadMainGame();
		}
	}

	public void Click_Quit()
	{
		GetTree().Quit();
	}
}

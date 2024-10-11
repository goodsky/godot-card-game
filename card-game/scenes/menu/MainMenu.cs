using Godot;
using System.Linq;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		Button continueGameButton = FindChild("ContinueGameButton") as Button;
		continueGameButton.Disabled = GameManager.Instance.Progress == null;

		Button[] allButtons = FindChildren("*Button").Select(x => x as Button).Where(x => x != null).ToArray();
		foreach (Button button in allButtons)
		{
			button.MouseEntered += () => HoverOverButton(button);
		}
	}

	public void Click_ContinueGame()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		SceneLoader.Instance.LoadGameLobby();
	}

	public void Click_StartNewGame()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		if (GameManager.Instance.Progress != null)
		{
			ConfirmPopUp.PopUp(this, "Are you sure you want to overwrite your previous save?", confirm: () => SceneLoader.Instance.LoadGameLobby(startNewGame: true));
		}
		else
		{
			SceneLoader.Instance.LoadGameLobby(startNewGame: true);
		}
	}

	public void Click_Settings()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		SettingsPopUp.PopUp(this, fadeBackground: false, showMainMenuButton: false);
	}

	public void Click_Quit()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		GetTree().Quit();
	}

	public void HoverOverButton(Button button)
	{
		if (!button.Disabled)
		{
			AudioManager.Instance.Play(Constants.Audio.BalloonSnap, pitch: 1.0f, volume: 0.5f);
		}
	}
}

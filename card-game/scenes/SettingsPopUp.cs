using Godot;

public partial class SettingsPopUp : Control
{
	[Export]
	public CanvasItem BackgroundFade { get; set; }

	[Export]
	public Slider EffectsVolumeSlider { get; set; }

	[Export]
	public Slider MusicVolumeSlider { get; set; }

	[Export]
	public Button MainMenuButton { get; set; }

    public override void _Ready()
    {
		var settings = SettingsLoader.LoadSettings();

		EffectsVolumeSlider.Value = settings.EffectsVolume;
		MusicVolumeSlider.Value = settings.MusicVolume;
    }

    public override void _EnterTree()
    {
		GD.Print("Settings pop up added");
        GetTree().Paused = true;
    }

    public override void _ExitTree()
    {
		GD.Print("Settings pop up removed");
		SettingsLoader.SaveSettings((float)EffectsVolumeSlider.Value, (float)MusicVolumeSlider.Value);
        GetTree().Paused = false;
    }

    public static void PopUp(Node root, bool fadeBackground = false, bool showMainMenuButton = false)
	{
		GD.Print("Pop up Settings Dialog");
		var settingsPopUp = Constants.SettingsPopUp.Instantiate<SettingsPopUp>();
		settingsPopUp.BackgroundFade.Visible = fadeBackground;
		settingsPopUp.MainMenuButton.Visible = showMainMenuButton;
		root.AddChild(settingsPopUp);
	}

	private void UpdateEffectsVolume(bool valueChanged)
	{
		AudioManager.Instance.UpdateEffectsVolume((float)EffectsVolumeSlider.Value);
	}

	private void UpdateMusicVolume(bool valueChanged)
	{
		// TODO: Add music
	}

	private void Click_MainMenu()
	{
		ConfirmPopUp.PopUp(
			MainGame.Instance.PopUpParent,
			"Are you sure you want to abandon this game?",
			() => SceneLoader.Instance.LoadMainMenu(),
			confirmText: "Quit",
			fadeBackground: true);
	}

	private void Click_Cancel()
	{
		QueueFree();
	}
}

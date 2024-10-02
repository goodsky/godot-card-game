using Godot;

public partial class SettingsPopUp : Control
{
	[Export]
	public Slider EffectsVolumeSlider { get; set; }

	[Export]
	public Slider MusicVolumeSlider { get; set; }

    public override void _Ready()
    {
        EffectsVolumeSlider.ValueChanged += SetEffectsVolume;
		MusicVolumeSlider.ValueChanged += SetMusicVolume;
    }

	private void SetEffectsVolume(double value)
	{

	}

	private void SetMusicVolume(double value)
	{

	}

	private void Click_Cancel()
	{

	}

	private void Click_MainMenu()
	{
		
	}
}

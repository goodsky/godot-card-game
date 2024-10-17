using Godot;
using System.Collections;

public partial class TestBench : Node2D
{
	[Export]
	public LineEdit PitchScale { get; set; }

	[Export]
	public Slider VolumeSlider { get; set; }

	public override void _Ready()
	{
		this.StartCoroutine(Debug_TestCoroutine());
	}

	public void Click_PlaySound()
	{
		float pitchScale = 1.0f;
		if (!string.IsNullOrEmpty(PitchScale.Text))
		{
			float.TryParse(PitchScale.Text, out pitchScale);
		}

		float volume = (float)VolumeSlider.Value;
		GD.Print("Volume = ", volume);

		AudioManager.Instance.Play(Constants.Audio.CardsShuffle, pitch: pitchScale, volume: volume);
	}

	public void Click_PlayManySounds()
	{
		this.StartCoroutine(Debug_PlaySoundCoroutine());
	}

	public void Click_GenerateDeck()
	{
		GameLoader.Debug_TestEndToEnd();
	}

	private IEnumerable Debug_PlaySoundCoroutine()
	{
		yield return AudioManager.Instance.Play(Constants.Audio.CardsShuffle);
		yield return new CoroutineDelay(1.0);
		yield return AudioManager.Instance.Play(Constants.Audio.CardsShuffle, pitch: 0.5f);
		yield return new CoroutineDelay(1.0);
		yield return AudioManager.Instance.Play(Constants.Audio.CardsShuffle, pitch: 1.25f);
	}

	private IEnumerable Debug_TestCoroutine()
	{
		GD.Print("Testing the coroutine!");
		yield return new CoroutineDelay(2.0);
		GD.Print("I waited 2 seconds!");
		yield return null;
		GD.Print("And that time I didn't wait at all!");
		for (int i = 10; i > 0; i--)
		{
			GD.Print($"{i}...");
			yield return new CoroutineDelay(0.2);
		}

		GD.Print("Blastoff!");
		yield return new CoroutineDelay(5);
		GD.Print("Get ready for a big one...");
		yield return new CoroutineDelay(1);
		for (int i = 100; i > 0; i--)
		{
			GD.Print($"{i}...!");
			yield return null;
		}

		GD.Print("Okay I'm done! Bye!");
	}
}

using Godot;
using System;

public partial class ConfirmPopUp : Control
{
	[Export]
	public CanvasItem BackgroundFade { get; set; }

	[Export]
	public Label MessageLabel { get; set; }

	[Export]
	public Button ConfirmButton { get; set; }

	[Export]
	public Button CancelButton { get; set; }

    public override void _Ready()
    {
        ConfirmButton.MouseEntered += HoverOverButton;
        CancelButton.MouseEntered += HoverOverButton;
    }

    public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed(Constants.EscEventName))
		{
			QueueFree();
		}
	}

	public static void PopUp(Node root, string message, Action confirm, Action cancel = null, string confirmText = "Confirm", bool fadeBackground = false)
	{
		var confirmPopUp = Constants.ConfirmPopUp.Instantiate<ConfirmPopUp>();
		confirmPopUp.BackgroundFade.Visible = fadeBackground;
		confirmPopUp.MessageLabel.Text = message;
		confirmPopUp.ConfirmButton.Text = confirmText;
		confirmPopUp.ConfirmButton.Pressed += confirm;
		confirmPopUp.ConfirmButton.Pressed += () => AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);

		if (cancel != null)
		{
			confirmPopUp.CancelButton.Pressed += cancel;
		}
		else
		{
			confirmPopUp.CancelButton.Pressed += () => confirmPopUp.QueueFree();
		}
		confirmPopUp.CancelButton.Pressed += () => AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);

		root.AddChild(confirmPopUp);
	}

	public void HoverOverButton()
	{
		AudioManager.Instance.Play(Constants.Audio.BalloonSnap, pitch: 1.0f, volume: 0.5f);
	}
}

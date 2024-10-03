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

	public static void PopUp(Node root, string message, Action confirm, Action cancel = null, string confirmText = "Confirm", bool fadeBackground = false)
	{
		var confirmPopUp = Constants.ConfirmPopUp.Instantiate<ConfirmPopUp>();
		confirmPopUp.BackgroundFade.Visible = fadeBackground;
		confirmPopUp.MessageLabel.Text = message;
		confirmPopUp.ConfirmButton.Text = confirmText;
		confirmPopUp.ConfirmButton.Pressed += confirm;

		if (cancel != null)
		{
			confirmPopUp.CancelButton.Pressed += cancel;
		}
		else
		{
			confirmPopUp.CancelButton.Pressed += () => confirmPopUp.QueueFree();
		}

		root.AddChild(confirmPopUp);
	}
}

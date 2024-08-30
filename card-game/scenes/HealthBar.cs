using Godot;

public partial class HealthBar : Sprite2D
{
	private Vector2 _size;

	[Export]
	public Sprite2D Marker { get; set; }

	[Export]
	public Texture2D PipTexture { get; set; }

	public int HealthPerPlayer { get; set; } = 5;

	public override void _Ready()
	{
		_size = Texture.GetSize();

		float buffer = 5f;
		float heightMinusBuffer = _size.Y - (buffer * 2);
		float pipStep = heightMinusBuffer / (HealthPerPlayer * 2);
		for (int i = 0; i <= HealthPerPlayer * 2; i++)
		{
			var pip = new Sprite2D
			{
				Texture = PipTexture,
				Position = new Vector2(_size.X / 2, buffer + (pipStep * i))
			};

			AddChild(pip);
		}

		Marker.Position = new Vector2(_size.X / 2, _size.Y / 2);
		MoveChild(Marker, -1);
	}
}

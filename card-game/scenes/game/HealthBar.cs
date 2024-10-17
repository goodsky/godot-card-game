using System;
using System.Collections;
using System.Threading.Tasks;
using Godot;

public partial class HealthBar : Sprite2D
{
	private Vector2 _size;

	[Export]
	public Sprite2D Marker { get; set; }

	[Export]
	public Texture2D PipTexture { get; set; }

	public int HealthPerPlayer { get; set; } = 5;

	// Player Points is a value in the range [-HealthPerPlayer, HealthPerPlayer]
	// -HealthPerPlayer == the player loses
	// +HealthPerPlayer == the player wins
	public int PlayerPoints { get; private set; }

	public override void _Ready()
	{
		_size = Texture.GetSize();

		for (int pipIndex = -HealthPerPlayer; pipIndex <= HealthPerPlayer; pipIndex++)
		{
			var pip = new Sprite2D
			{
				Texture = PipTexture,
				Position = new Vector2(_size.X / 2, GetPositionForPip(pipIndex))
			};

			AddChild(pip);
		}

		Marker.Position = new Vector2(_size.X / 2, _size.Y / 2);
		MoveChild(Marker, -1);
	}

	public Task PlayerTakeDamage(int damage)
	{
		return OpponentTakeDamage(-damage); // player damage is just negative points
	}

	public Task OpponentTakeDamage(int damage)
	{
		int startingPoints = PlayerPoints;
		PlayerPoints += damage;
		PlayerPoints = Math.Clamp(PlayerPoints, -HealthPerPlayer, HealthPerPlayer);
		return this.StartCoroutine(MovePipCoroutine(startingPoints, PlayerPoints));
	}

	private float GetPositionForPip(int pipIndex)
	{
		float buffer = 5f;
		float heightMinusBuffer = _size.Y - (buffer * 2);
		float pipStep = heightMinusBuffer / (HealthPerPlayer * 2);
		float midPoint = _size.Y / 2;

		return midPoint + (pipStep * -pipIndex);
	}

	private IEnumerable MovePipCoroutine(int startPipIndex, int endPipIndex)
	{
		int pipIndex = startPipIndex;
		int pipDelta = startPipIndex < endPipIndex ? 1 : -1;
		while (pipIndex != endPipIndex)
		{
			pipIndex += pipDelta;
			Vector2 pipPosition = new Vector2(Marker.GlobalPosition.X, GlobalPosition.Y + GetPositionForPip(pipIndex));
			yield return Marker.LerpGlobalPositionCoroutine(pipPosition, 0.05f);
			yield return new CoroutineDelay(0.2);
		}

		if (endPipIndex <= -HealthPerPlayer || endPipIndex >= HealthPerPlayer)
		{
			Log.Info($"The pip has hit the limit. This game is over! {startPipIndex} -> {endPipIndex}.");
			MainGame.Instance.GameOver();
		}
	}
}

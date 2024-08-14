using Godot;

public partial class Card : Node2D
{
	private static readonly RandomNumberGenerator Rand = new RandomNumberGenerator();
	
	private bool _isDragging = false;

	protected CardManager CardManager;

	// When not dragging, the home drop node for this card to live at.
	public CardDrop HomeCardDrop { get; set; }

	// When not dragging, the target location for this card to live at.
	public Vector2? TargetPosition { get; set; }

	[Export]
	public Area2D Area { get; set; }

	public override void _Ready()
	{
		CardManager = this.GetCardManager();

		SetProcessInput(false);

		Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Modulate = new Color(Rand.Randf(), Rand.Randf(), Rand.Randf());
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDragging)
		{
			GlobalPosition = GlobalPosition.Lerp(GetGlobalMousePosition(), 15f * (float)delta);
		}
		else if (TargetPosition.HasValue)
		{
			GlobalPosition = GlobalPosition.Lerp(TargetPosition.Value, 10f * (float)delta);
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (_isDragging && inputEvent is InputEventMouseButton mouseButtonEvent &&
			!mouseButtonEvent.Pressed)
		{
			CardManager.StopDragging(this);
		}
	}

	public void StartDragging()
	{
		_isDragging = true;
		SetProcessInput(true);
	}

	public void StopDragging()
	{
		_isDragging = false;
		SetProcess(false);
	}
}

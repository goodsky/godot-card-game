using Godot;
using System.Linq;

public partial class Card : Node2D
{
	private bool _isDragging = false;

	// While dragging, this tracks if there is an active card node available for a drop.
	private CardDrop _activeCardDrop = null;

	// When not dragging, the home drop node for this card to live at. This node is responsible for setting the target position.
	private CardDrop _homeCardDrop { get; set; }

	// When not dragging, the target location for this card to live at.
	public Vector2? TargetPosition { get; set; }

	[Export]
	public Area2D Area { get; set; }

	public override void _Ready()
	{
		SetProcessInput(false);
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
			StopDragging();
		}

		if (_isDragging && inputEvent is InputEventMouseMotion mouseMotionEvent)
		{
			var activeCardDrops = GetTree()
				.GetNodesInGroup(Constants.ActiveCardDropGroup)
				.Where(node => node is CardDrop)
				.Select(node => node as CardDrop)
				.ToArray();

			if (activeCardDrops.Length == 0)
			{
				_activeCardDrop = null;
			}
			else if (activeCardDrops.Length > 0)
			{
				if (activeCardDrops.Length > 1)
				{
					GD.Print("Multiple Active Card Drops: ", activeCardDrops.Select(node => node.Name)); // Will this ever happen?
				}
				_activeCardDrop = activeCardDrops[0];
			}
		}
	}

	public void SetCardDrop(CardDrop cardDrop)
	{
		_homeCardDrop = cardDrop;
		cardDrop.TryAddCard(this);
	}

	public void StartDragging()
	{
		_isDragging = true;
		AddToGroup(Constants.DraggingCardGroup);
		SetProcessInput(true);
	}

	public void StopDragging()
	{
		if (_activeCardDrop != null)
		{
			if (_homeCardDrop != null)
			{
				_homeCardDrop.TryRemoveCard(this);
			}

			SetCardDrop(_activeCardDrop);
			_activeCardDrop = null;
		}

		_isDragging = false;
		RemoveFromGroup(Constants.DraggingCardGroup);
		SetProcess(false);
	}
}

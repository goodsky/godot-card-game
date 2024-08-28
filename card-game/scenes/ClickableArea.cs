using System;
using System.Linq;
using Godot;

public partial class ClickableArea : Node2D
{
	private static readonly float DragDistanceThreshold = 5f;

	private Area2D _area;

	private bool _isMouseOver = false;
	private bool _isMouseDown = false;
	private bool _isDragging = false;

	private Vector2? _clickGlobalPosition;

	[Signal]
	public delegate void AreaClickedEventHandler();

	[Signal]
	public delegate void AreaStartDraggingEventHandler();

	[Signal]
	public delegate void AreaStopDraggingEventHandler();

	[Signal]
	public delegate void AreaMouseOverEventHandler();

	[Signal]
	public delegate void AreaMouseOutEventHandler();

	public CollisionShape2D GetCollisionShape()
	{
		return _area.GetNode<CollisionShape2D>("CollisionShape2D");
	}

	public RectangleShape2D GetRectangleShape()
	{
		CollisionShape2D collisionShape = GetCollisionShape();
		return collisionShape.Shape as RectangleShape2D;
	}

	public override void _Ready()
	{
		SetProcessInput(false);

		_area = GetChildren()
			.Where(node => node is Area2D)
			.Select(node => node as Area2D)
			.FirstOrDefault();
		
		if (_area == null)
		{
			throw new InvalidOperationException("ClickableArea does not have a child Area2D!");
		}

		_area.InputEvent += OnArea2DInputEvent;
		_area.MouseEntered += OnMouseEntered;
		_area.MouseExited += OnMouseExited;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionReleased(Constants.ClickEventName))
		{
			GD.Print($"Unclick: mouseDown = {_isMouseDown}; dragging = {_isDragging};");
			if (_isDragging)
			{
				EmitSignal(SignalName.AreaStopDragging);
			}
			else if (_isMouseDown)
			{
				EmitSignal(SignalName.AreaClicked);
			}

			SetProcessInput(false);
			_isMouseDown = false;
			_isDragging = false;
			_clickGlobalPosition = null;
		}
	}

	protected void OnArea2DInputEvent(Node viewport, InputEvent inputEvent, long shape_idx)
	{
		if (_clickGlobalPosition != null && !_isDragging && inputEvent is InputEventMouseMotion inputMouseMotionEvent)
		{
			Vector2 clickDelta = inputMouseMotionEvent.GlobalPosition - _clickGlobalPosition.Value;
			if (clickDelta.Length() > DragDistanceThreshold)
			{
				GD.Print($"Dragging on {this.GetParent().Name}. delta = {clickDelta}; length = {clickDelta.Length()}");
				_isDragging = true;
				EmitSignal(SignalName.AreaStartDragging);
			}
		}

		if (inputEvent.IsActionPressed(Constants.ClickEventName))
		{
			GD.Print($"Click on {this.GetParent().Name}");
			SetProcessInput(true);
			_isMouseDown = true;
			_clickGlobalPosition = GetGlobalMousePosition();
		}
	}

	protected void OnMouseEntered()
	{
		_isMouseOver = true;
		EmitSignal(SignalName.AreaMouseOver);
	}

	protected void OnMouseExited()
	{
		_isMouseOver = false;
		EmitSignal(SignalName.AreaMouseOut);
	}
}
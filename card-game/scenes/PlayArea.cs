using System;
using System.Linq;
using Godot;

public partial class PlayArea : CardDrop
{
	private Vector2 _area;
	private bool _isHoverOver = false;

	[Export]
	public bool SupportsPickUp { get; set; }

	[Export]
	public Area2D Area { get; set; }

    protected override int MaxCards => 1;

    public override void _Ready()
	{
		base._Ready();

		var rect = Area.GetNode<CollisionShape2D>("CollisionShape2D").Shape as RectangleShape2D;
		_area = rect.Size;
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(-_area.X / 2, -_area.Y / 2, _area.X, _area.Y), _isHoverOver ? Colors.Yellow : Colors.Gray, false, 2.0f);
	}

	public override bool TryAddCard(Card card, Vector2? globalPosition)
	{
		if (base.TryAddCard(card, globalPosition))
		{
			card.TargetPosition = GlobalPosition;
			return true;
		}
		return false;
	}

	public override bool TryRemoveCard(Card card)
	{
		return base.TryRemoveCard(card);
	}

	public void OnArea2DInputEvent(Node viewport, InputEvent inputEvent, int shape_idx)
	{
		if (SupportsPickUp && inputEvent.IsActionPressed("click"))
		{
			Card card = GetChildCard();
			if (card != null)
			{
				card.StartDragging();
			}
		}
	}

	public void HoverOver()
	{
		_isHoverOver = true;
		AddToGroup(Constants.ActiveCardDropGroup);
		QueueRedraw();
	}

	public void HoverOut()
	{
		_isHoverOver = false;
		RemoveFromGroup(Constants.ActiveCardDropGroup);
		QueueRedraw();
	}

	private Card GetChildCard()
	{
		return GetChildCards().FirstOrDefault();
	}
}

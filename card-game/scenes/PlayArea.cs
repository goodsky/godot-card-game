using System;
using System.Linq;
using Godot;

public partial class PlayArea : CardDrop
{
	private Vector2 _size;
	private bool _isHoverOver = false;

	[Export]
	public bool SupportsPickUp { get; set; }

	[Export]
	public ClickableArea Area { get; set; }

    protected override int MaxCards => 1;

    public override void _Ready()
	{
		base._Ready();

		_size = Area.GetRectangleShape().Size;
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(-_size.X / 2, -_size.Y / 2, _size.X, _size.Y), _isHoverOver ? Colors.Yellow : Colors.Gray, false, 2.0f);
	}

	public override bool TryAddCard(Card card, Vector2? globalPosition)
	{
		if (base.TryAddCard(card, globalPosition))
		{
			card.TargetPosition = GlobalPosition;

			if (SupportsPickUp)
			{
				card.Area.AreaStartDragging += card.StartDragging;
				card.Area.AreaStopDragging += card.StopDragging;
			}
			
			return true;
		}
		return false;
	}

	public override bool TryRemoveCard(Card card)
	{
		if  (base.TryRemoveCard(card))
		{
			card.Area.AreaStartDragging -= card.StartDragging;
			card.Area.AreaStopDragging -= card.StopDragging;
			return true;
		}
		return false;
	}

	public void HoverOver()
	{
		_isHoverOver = true;
		CardManager.ActivateCardDrop(this);
		QueueRedraw();
	}

	public void HoverOut()
	{
		_isHoverOver = false;
		CardManager.DeactivateCardDrop(this);
		QueueRedraw();
	}
}

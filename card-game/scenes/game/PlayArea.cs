using Godot;

public partial class PlayArea : CardDrop
{
	private Vector2 _size;
	private bool _isHoverOver = false;

	[Export]
	public bool SupportsPickUp { get; set; }

	[Export]
	public bool SupportsDrop { get; set; } = true;

	[Export]
	public ClickableArea Area { get; set; }

	protected override int MaxCards => 1;

	public override void _Ready()
	{
		base._Ready();

		_size = Area.GetRectangleShape().Size;
		Area.AreaClicked += Clicked;
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(-_size.X / 2, -_size.Y / 2, _size.X, _size.Y), _isHoverOver ? Colors.WhiteSmoke : Colors.Gray, false, 2.0f);
	}

    public override bool CanDropCard(Card card)
    {
        return MainGame.Instance.Board.CanPlayCardAtLocation(card, this);
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
		if (base.TryRemoveCard(card))
		{
			card.Area.AreaStartDragging -= card.StartDragging;
			card.Area.AreaStopDragging -= card.StopDragging;
			return true;
		}
		return false;
	}

	public void Clicked()
	{
		if (SupportsDrop && CardManager.Instance.SelectedCard != null)
		{
			Card selectedCard = CardManager.Instance.SelectedCard;
			CardManager.Instance.SelectCard(null);
			CardManager.Instance.SetCardDrop(selectedCard, this);
		}
	}

	public void HoverOver()
	{
		if (!SupportsDrop) return;

		_isHoverOver = true;
		CardManager.Instance.ActivateCardDrop(this);
		QueueRedraw();
	}

	public void HoverOut()
	{
		if (!SupportsDrop) return;

		_isHoverOver = false;
		CardManager.Instance.DeactivateCardDrop(this);
		QueueRedraw();
	}
}

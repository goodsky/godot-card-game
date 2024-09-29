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

	protected override int MaxCards => MainGame.Instance.CurrentState == GameState.IsaacMode ? 1 : 2;

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
		if (MainGame.Instance.CurrentState == GameState.PlayCard ||
			MainGame.Instance.CurrentState == GameState.PlayCard_PayPrice)
		{
			return MainGame.Instance.Board.CanPlayCardAtLocation(card, this);
		}

		return base.CanDropCard(card);
	}

	public override bool TryAddCard(Card card, Vector2? globalPosition)
	{
		CardDrop oldCardDrop = card.HomeCardDrop;
		if (base.TryAddCard(card, globalPosition))
		{
			switch (MainGame.Instance.CurrentState)
			{
				case GameState.PlayCard:
					MainGame.Instance.PlayedCard(card, this, oldCardDrop);
					break;

				case GameState.IsaacMode:
					card.TargetPosition = GlobalPosition;

					if (SupportsPickUp)
					{
						card.Area.AreaStartDragging += card.StartDragging;
						card.Area.AreaStopDragging += card.StopDragging;
					}
					break;

				case GameState.EnemyStageCard:
					card.TargetPosition = GlobalPosition;
					break;

				case GameState.EnemyPlayCard:
					card.TargetPosition = GlobalPosition;
					AudioManager.Instance.Play(Constants.Audio.PlayCardClick);
					break;

				default:
					card.TargetPosition = GlobalPosition;
					GD.PushError($"[Unexpected State Action] Card added to PlayArea during {MainGame.Instance.CurrentState}.");
					break;
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
		if (SupportsDrop && ActiveCardState.Instance.SelectedCard != null)
		{
			Card selectedCard = ActiveCardState.Instance.SelectedCard;
			ActiveCardState.Instance.SelectCard(null);
			ActiveCardState.Instance.SetCardDrop(selectedCard, this);
		}
	}

	public void HoverOver()
	{
		if (!SupportsDrop) return;

		_isHoverOver = true;
		ActiveCardState.Instance.ActivateCardDrop(this);
		QueueRedraw();
	}

	public void HoverOut()
	{
		if (!SupportsDrop) return;

		_isHoverOver = false;
		ActiveCardState.Instance.DeactivateCardDrop(this);
		QueueRedraw();
	}
}

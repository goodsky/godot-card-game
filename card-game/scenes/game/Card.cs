using System;
using System.Text;
using System.Text.Json.Serialization;
using Godot;

public enum CardBloodCost
{
	Zero = 0,
	One = 1,
	Two = 2,
	Three = 3,
}

public enum CardRarity
{
	Sacrifice = 0,
	Common = 1,
	Uncommon = 2,
	Rare = 3,
}

public struct CardInfo
{
	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("avatar")]
	public string AvatarResource { get; set; }

	[JsonPropertyName("attack")]
	public int Attack { get; set; }

	[JsonPropertyName("health")]
	public int Health { get; set; }

	[JsonPropertyName("cost")]
	public CardBloodCost BloodCost { get; set; }

	[JsonPropertyName("rarity")]
	public CardRarity Rarity { get; set; }
}

public partial class Card : Node2D
{
	private static readonly RandomNumberGenerator Rand = new RandomNumberGenerator();

	private bool _isSelected = false;
	private bool _isDragging = false;
	private bool _freeDragging = false; // This is only for Isaac mode right now

	// When not dragging, the home drop node for this card to live at.
	public CardDrop HomeCardDrop { get; set; }

	// When not dragging, the target location for this card to live at.
	public Vector2? TargetPosition { get; set; }

	// Optional offset for things like hovering over or wiggling.
	public Vector2? TargetPositionOffset { get; set; }

	public CardInfo CardInfo { get; private set; }

	[Export]
	public Sprite2D Background { get; set; }

	[Export]
	public Sprite2D Avatar { get; set; }

	[Export]
	public Sprite2D[] BloodCostIcons { get; set; }

	[Export]
	public CanvasText NameLabel { get; set; }

	[Export]
	public CanvasText AttackLabel { get; set; }

	[Export]
	public CanvasText DefenseLabel { get; set; }

	[Export]
	public ClickableArea Area { get; set; }

	[Signal]
	public delegate void CardUnselectedEventHandler();

	public override void _Ready()
	{
		switch (CardInfo.Rarity)
		{
			case CardRarity.Sacrifice:
				Background.SelfModulate = new Color("88615f");
				break;

			case CardRarity.Common:
				Background.SelfModulate = new Color("777168");
				break;

			case CardRarity.Uncommon:
				Background.SelfModulate = new Color("5659ae");
				break;

			case CardRarity.Rare:
				Background.SelfModulate = new Color(Rand.Randf() * .75f, Rand.Randf(), Rand.Randf() * .75f);
				break;
		}

		UpdateVisuals(CardInfo);

		Area.AreaMouseOver += HoverOver;
		Area.AreaMouseOut += HoverOut;
	}

	public override void _Draw()
	{
		if (MainGame.Instance.CurrentState == GameState.PlayCard_SelectCard && (_isDragging || _isSelected))
		{
			Vector2 mousePosition = GetGlobalMousePosition();
			if (mousePosition.X < MainGame.Instance.Board.Background.Size.X &&
				mousePosition.Y < MainGame.Instance.Board.Background.Size.Y)
			{
				CardDrop activeCardDrop = CardManager.Instance.ActiveCardDrop;
				if (activeCardDrop is PlayArea)
				{
					Vector2 topOfPlayArea = CardManager.Instance.ActiveCardDrop.GlobalPosition - new Vector2(0, 40f);
					Color arrowColor = activeCardDrop.CanDropCard(this) ? Colors.LawnGreen : Colors.IndianRed;
					DrawArrowToPosition(topOfPlayArea, arrowColor);
				}
				else
				{
					DrawArrowToPosition(mousePosition, Colors.IndianRed);
				}
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDragging)
		{
			if (_freeDragging)
			{
				GlobalPosition = GlobalPosition.Lerp(GetGlobalMousePosition(), 15f * (float)delta);
			}
			else
			{
				float xMargin = 50f;
				float yMargin = 65f;
				float minDragX = xMargin;
				float maxDragX = MainGame.Instance.Board.Background.Size.X - xMargin;
				float minDragY = MainGame.Instance.Board.Background.Size.Y;
				float maxDragY = GetViewportRect().Size.Y - yMargin;

				Vector2 dragTarget = new Vector2(
					Math.Clamp(GetGlobalMousePosition().X, minDragX, maxDragX),
					Math.Clamp(GetGlobalMousePosition().Y, minDragY, maxDragY));

				GlobalPosition = GlobalPosition.Lerp(dragTarget, 15f * (float)delta);
				QueueRedraw();
			}
		}
		else if (TargetPosition.HasValue)
		{
			if (_isSelected)
			{
				QueueRedraw();
			}

			Vector2 targetPosition = TargetPosition.Value;
			if (TargetPositionOffset.HasValue)
			{
				targetPosition += TargetPositionOffset.Value;
			}

			GlobalPosition = GlobalPosition.Lerp(targetPosition, 10f * (float)delta);
		}
	}

	public void SetCardInfo(CardInfo cardInfo)
	{
		CardInfo = cardInfo;
		Avatar.Texture = ResourceLoader.Load<CompressedTexture2D>(cardInfo.AvatarResource);
	}

	public void Select()
	{
		_isSelected = true;
	}

	public void Unselect()
	{
		_isSelected = false;
		QueueRedraw();
		EmitSignal(SignalName.CardUnselected);
	}

	public void StartDragging()
	{
		_isDragging = true;
		_freeDragging = MainGame.Instance.IsaacMode;
		CardManager.Instance.SetDraggingCard(this);

		// When using touch screens - sometimes the global mouse position does not match card position
		float mouseToCardDelta = GlobalPosition.DistanceTo(GetGlobalMousePosition());
		// GD.Print($"Start dragging {Name}; My Position: {GlobalPosition}; Mouse Position: {GetGlobalMousePosition()}; DistanceTo: {mouseToCardDelta};");
		if (mouseToCardDelta > 75f)
		{
			GetViewport().WarpMouse(GlobalPosition);
			GD.Print("Moving mouse inside of new dragging card", Name);
		}

		ZIndex = 10;
	}

	public void StopDragging()
	{
		_isDragging = false;
		_isSelected = false;
		CardManager.Instance.ClearDraggingCard(this);

		QueueRedraw();
		ZIndex = 0;
	}

	private void HoverOver()
	{
		var cardInfo = new StringBuilder();
		cardInfo.Append("\n\n\n\n\n");
		cardInfo.AppendLine($"[center][font_size=16]{CardInfo.Name}[/font_size][/center]");
		cardInfo.AppendLine("");
		cardInfo.AppendLine($"Attack: {CardInfo.Attack}");
		cardInfo.AppendLine($"Defense: {CardInfo.Health}");
		cardInfo.AppendLine($"Cost: {CardInfo.BloodCost}");
		cardInfo.AppendLine($"Rarity: {CardInfo.Rarity}");

		InfoArea.Instance.SetInfoBar(cardInfo.ToString(), this);
	}

	private void HoverOut()
	{
		InfoArea.Instance.ResetInfoBar(this);
	}

	private void DrawArrowToPosition(Vector2 targetGlobalPosition, Color color)
	{
		Vector2 delta = targetGlobalPosition - GlobalPosition;
		DrawLine(Vector2.Zero, delta, color, width: 10f);

		float angle = delta.Angle();
		Vector2[] arrowHead = new Vector2[] {
			delta + new Vector2(0, -5).Rotated(angle + (Mathf.Pi / 2)),
			delta + new Vector2(0, -35).Rotated(angle + (Mathf.Pi / 2) + (4 * Mathf.Pi / 5)),
			delta + new Vector2(0, -35).Rotated(angle + (Mathf.Pi / 2) + (6 * Mathf.Pi / 5)),
		};
		DrawColoredPolygon(arrowHead, color);
	}

	private void UpdateVisuals(CardInfo info)
	{
		NameLabel.Text = info.Name;
		AttackLabel.Text = info.Attack.ToString();
		DefenseLabel.Text = info.Health.ToString();

		for (int i = 0; i < BloodCostIcons.Length; i++)
		{
			BloodCostIcons[i].Visible = i < ((int)info.BloodCost);
		}
	}
}

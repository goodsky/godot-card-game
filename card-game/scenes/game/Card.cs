using System.Text;
using Godot;

public struct CardInfo
{
	public string Name { get; set; }

	public int Attack { get; set; }

	public int Defense { get; set; }

	public int BloodCost { get; set; }
}

public partial class Card : Node2D
{
	private static readonly RandomNumberGenerator Rand = new RandomNumberGenerator();
	
	private bool _isDragging = false;

	// When not dragging, the home drop node for this card to live at.
	public CardDrop HomeCardDrop { get; set; }

	// When not dragging, the target location for this card to live at.
	public Vector2? TargetPosition { get; set; }

	// Optional offset for things like hovering over or wiggling.
	public Vector2? TargetPositionOffset { get; set; }

	public CardInfo CardInfo { get; set; }

	[Export]
	public Sprite2D Background { get; set; }

	[Export]
	public Sprite2D Avatar { get; set; }

	[Export]
	public Sprite2D[] BloodCostIcons { get; set; }

	[Export]
	public CanvasText NameLabel { get; set;}

	[Export]
	public CanvasText AttackLabel { get; set; }

	[Export]
	public CanvasText DefenseLabel { get; set; }

	[Export]
	public ClickableArea Area { get; set; }

	public override void _Ready()
	{
		// DEBUG: just for now
		Background.SelfModulate = new Color(Rand.Randf() * .75f, Rand.Randf(), Rand.Randf() * .75f);
		UpdateVisuals(CardInfo);

		Area.AreaMouseOver += HoverOver;
		Area.AreaMouseOut += HoverOut;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDragging)
		{
			GlobalPosition = GlobalPosition.Lerp(GetGlobalMousePosition(), 15f * (float)delta);
		}
		else if (TargetPosition.HasValue)
		{
			Vector2 targetPosition = TargetPosition.Value;
			if (TargetPositionOffset.HasValue) {
				targetPosition += TargetPositionOffset.Value;
			}

			GlobalPosition = GlobalPosition.Lerp(targetPosition, 10f * (float)delta);
		}
	}

	public void StartDragging()
	{
		_isDragging = true;
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
		CardManager.Instance.ClearDraggingCard(this);

		ZIndex = 0;
	}

	private void HoverOver()
	{
		var cardInfo = new StringBuilder();
		cardInfo.Append("\n\n\n\n\n");
		cardInfo.AppendLine($"[center][font_size=16]{CardInfo.Name}[/font_size][/center]");
		cardInfo.AppendLine("");
		cardInfo.AppendLine($"Attack: {CardInfo.Attack}");
		cardInfo.AppendLine($"Defense: {CardInfo.Defense}");

		InfoArea.Instance.SetInfoBar(cardInfo.ToString(), this);
	}

	private void HoverOut()
	{
		InfoArea.Instance.ResetInfoBar(this);
	}

	private void UpdateVisuals(CardInfo info)
	{
		NameLabel.Text = info.Name;
		AttackLabel.Text = info.Attack.ToString();
		DefenseLabel.Text = info.Defense.ToString();

		for (int i = 0; i < BloodCostIcons.Length; i++)
		{
			BloodCostIcons[i].Visible = (i < info.BloodCost);
		}
	}
}

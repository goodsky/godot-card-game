using System;
using System.Collections;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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
	[JsonPropertyName("id")]
	public int Id { get; set; }

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

public class CardCombatInfo
{
	public int DamageReceived { get; set; }
}

public partial class Card : Node2D
{
	private static readonly RandomNumberGenerator Rand = new RandomNumberGenerator();

	private bool _isAnimating = false; // when this Card's position is being controlled by an animation
	private bool _isDragging = false;
	private bool _isSelected = false;
	private bool _freeDragging = false; // This is only for Isaac mode right now

	private CardCombatInfo _combatInfo;

	// When not dragging, the home drop node for this card to live at.
	public CardDrop HomeCardDrop { get; set; }

	// When not dragging, the target location for this card to live at.
	public Vector2? TargetPosition { get; set; }

	// Optional offset for things like hovering over or wiggling.
	public Vector2? TargetPositionOffset { get; set; }

	public CardInfo Info { get; set; }

	[Export]
	public CardVisual Visual { get; set; }

	[Export]
	public ClickableArea Area { get; set; }

	[Signal]
	public delegate void CardUnselectedEventHandler();

	public override void _Ready()
	{
		// Note: CardInfo must be set before adding Card to scene tree!
		Visual.Update(Info, firstUpdate: true);
		_combatInfo = new CardCombatInfo
		{
			DamageReceived = 0,
		};

		Area.AreaMouseOver += UpdateInfoArea;
		Area.AreaMouseOut += ResetInfoArea;
	}

	public override void _Draw()
	{
		if (MainGame.Instance.CurrentState == GameState.PlayCard && (_isDragging || _isSelected))
		{
			Vector2 mousePosition = GetGlobalMousePosition();
			if (mousePosition.X < MainGame.Instance.Board.Background.Size.X &&
				mousePosition.Y < MainGame.Instance.Board.Background.Size.Y)
			{
				CardDrop activeCardDrop = ActiveCardState.Instance.ActiveCardDrop;
				if (activeCardDrop is PlayArea)
				{
					Vector2 topOfPlayArea = ActiveCardState.Instance.ActiveCardDrop.GlobalPosition - new Vector2(0, 40f);
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
		else if (!_isAnimating && TargetPosition.HasValue)
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

	public void SetAnimationControl(bool isAnimating)
	{
		_isAnimating = isAnimating;
	}

	

	public void DealDamage(int damage)
	{
		_combatInfo.DamageReceived += damage;
		Visual.Update(Info, _combatInfo);

		if (Info.Health - _combatInfo.DamageReceived <= 0)
		{
			Kill();

			AudioManager.Instance.Play(Constants.Audio.KillCard);
		}
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
		_freeDragging = MainGame.Instance.CurrentState == GameState.IsaacMode;
		ActiveCardState.Instance.SetDraggingCard(this);

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
		ZIndex = 0;
		_isDragging = false;
		_isSelected = false;
		ActiveCardState.Instance.ClearDraggingCard(this);

		QueueRedraw();
	}

	private bool _isKilled = false;
	public async void Kill()
	{
		if (_isKilled) return;
		_isKilled = true;
		await this.StartCoroutine(KillCoroutine());
	}

	private IEnumerable KillCoroutine()
	{
		yield return this.FadeTo(0.0f, 0.05f);

		GD.Print($"Card Killed : {Name}");
		Node parent = GetParent()?.GetParent();
		if (parent is CardDrop cardDrop)
		{
			GD.Print($"Removing from CardDrop {cardDrop.Name}");
			cardDrop.TryRemoveCard(this);
		}
		QueueFree();
	}

	private CancellationTokenSource rotationCancellation = null;
	public async Task RotateCard(float radians)
	{
		rotationCancellation?.Cancel();

		rotationCancellation = new CancellationTokenSource();
		await this.StartCoroutine(RotateCardCoroutine(radians), rotationCancellation.Token);
	}

	private IEnumerable RotateCardCoroutine(float radians)
	{
		float rotateDelta = 0.1f;
		if (Rotation > radians)
		{
			while (Rotation > radians)
			{
				Rotation -= rotateDelta;
				yield return null;
			}
		}
		else
		{
			while (Rotation < radians)
			{
				Rotation += rotateDelta;
				yield return null;
			}
		}
		Rotation = radians;
		rotationCancellation = null;
	}

	private bool _isShaking = false;
	public void StartShaking()
	{
		if (_isShaking) return;
		_isShaking = true;
		Task _ = this.StartCoroutine(ShakingCoroutine());
	}

	public void StopShaking()
	{
		_isShaking = false;
	}

	public void UpdateInfoArea()
	{
		if (Visual.CardBack.Visible) return; // Don't spoil the surprise!
		InfoArea.Instance.SetCardInfo(Info, this);
	}

	public void ResetInfoArea()
	{
		InfoArea.Instance.ResetInfoBar(this);
	}

	private IEnumerable ShakingCoroutine()
	{
		float shakeDelta = 0.015f;
		float maxAngle = Mathf.Pi / 24;
		while (_isShaking)
		{
			Rotation += shakeDelta;
			if (Rotation > maxAngle || Rotation < -maxAngle)
			{
				Rotation = Mathf.Clamp(Rotation, -maxAngle, maxAngle);
				shakeDelta *= -1;
			}

			yield return null;
		}

		Rotation = 0f;
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
}

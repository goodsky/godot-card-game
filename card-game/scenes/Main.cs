using Godot;
using System;

/**
 * Main Game Manager: Handles State Machine + Transitions
 */
public enum GameState
{
	DrawCard,
	PlayCard_SelectCard,
	PlayCard_SelectLocation,
	PlayCard_PayPrice,
	PlayerCombatStart,
	PlayerCombatEnd,
	EnemyPlayCard,
	EnemyCombatStart,
	EnemyCombatEnd,
	EnemyStageCard,
}

public partial class Main : Node2D
{
	public static Main Instance { get; private set; }

	public GameState CurrentState { get; private set; }

	[Signal]
	public delegate void OnStateTransitionEventHandler(GameState nextState, GameState prevState);

	[Export]
	public Hand Hand { get; set; }

	[Export]
	public HealthBar HealthBar { get; set; }

	public override void _Ready()
	{
		Instance = this;
	}

	public void DrawCardFromDeck()
	{
		switch (CurrentState)
		{
			case GameState.DrawCard:
				TransitionToState(GameState.PlayCard_SelectCard);
				break;

			default:
				throw new StateMachineException(nameof(DrawCardFromDeck), CurrentState);
		}
	}

	public void DrawCardFromSacrificeDeck()
	{
		switch (CurrentState)
		{
			case GameState.DrawCard:
				TransitionToState(GameState.PlayCard_SelectCard);
				break;

			default:
				throw new StateMachineException(nameof(DrawCardFromSacrificeDeck), CurrentState);
		}
	}

	public void EndTurn()
	{
		switch (CurrentState)
		{
			case GameState.PlayCard_SelectCard:
				TransitionToState(GameState.PlayerCombatStart);
				break;

			default:
				throw new StateMachineException(nameof(EndTurn), CurrentState);
		}
	}

	/** Isaac Mode! */
	private static int DrawnCardCount = 0;
	public void Debug_DrawCard()
	{
		var card = Constants.CardScene.Instantiate<Card>();
		card.AddToGroup("DebugCard");
		card.Name = $"Card_{DrawnCardCount++}";
		card.GlobalPosition = Hand.GlobalPosition + new Vector2(300, 0);
		card.CardInfo = new CardInfo()
		{
			Name = $"Blue Monster #{DrawnCardCount}",
			Attack = Random.Shared.Next(1, 6),
			Defense = Random.Shared.Next(1, 11),
			BloodCost = Random.Shared.Next(1, 4),
		};

		Texture2D avatar = Constants.CardAvatars[Random.Shared.Next(Constants.CardAvatars.Length)];
		card.Avatar.Texture = avatar;

		GD.Print($"Drawing card {card.Name}");
		CardManager.Instance.SetCardDrop(card, Hand);
	}

	public void Debug_ClearCards()
	{
		var debugCards = GetTree().GetNodesInGroup("DebugCard");
		foreach (var card in debugCards)
		{
			CardManager.Instance.SetCardDrop(card as Card, null);
			card.QueueFree();
		}
	}
	/** END Isaac Mode! */

	private void TransitionToState(GameState nextState)
	{
		GD.Print($"State Transition: {CurrentState.ToString()} -> {nextState.ToString()}");

		// Godot requires enums be converted to int - so they are valid variants.
		EmitSignal(SignalName.OnStateTransition, (int)nextState, (int)CurrentState);
	}
}

public class StateMachineException : Exception
{
	public StateMachineException(string action, GameState currentState) :
		base($"Invalid State Transition [{currentState.ToString()}] Cannot process action \"{action}\"") { }
}
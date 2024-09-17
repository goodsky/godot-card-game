using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

/**
 * Main Game Manager: Handles State Machine + Transitions
 */
public enum GameState
{
	Initializing,
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

public partial class MainGame : Node2D
{
	public static MainGame Instance { get; private set; }

	public GameState CurrentState { get; private set; } = GameState.Initializing;

	[Signal]
	public delegate void OnStateTransitionEventHandler(GameState nextState, GameState prevState);

	[Export]
	public bool IsaacMode { get; set; } = false;

	[Export]
	public GameBoard Board { get; set; }

	[Export]
	public Hand Hand { get; set; }

	[Export]
	public HealthBar HealthBar { get; set; }

	public Deck Creatures { get; set; }

	public Deck Sacrifices { get; set; }

	public override void _Ready()
	{
		Instance = this;
		InitializeGame();
	}

	public void DrawCardFromDeck()
	{
		GD.Print("Draw card from deck...");
		switch (CurrentState)
		{
			case GameState.DrawCard:
				var drawnCardInfo = Creatures.DrawFromTop();
				InstantiateCardInHand(drawnCardInfo);
				TransitionToState(GameState.PlayCard_SelectCard);
				break;

			default:
				throw new StateMachineException(nameof(DrawCardFromDeck), CurrentState);
		}
	}

	public void DrawCardFromSacrificeDeck()
	{
		GD.Print("Draw sacrifice card from deck...");

		switch (CurrentState)
		{
			case GameState.DrawCard:
				var drawnCardInfo = Sacrifices.DrawFromTop();
				InstantiateCardInHand(drawnCardInfo);
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

	private async void InitializeGame()
	{
		if (IsaacMode)
		{
			for (int i = 0; i < 3; i++)
			{
				Debug_DrawCard();
			}

			return;
		}

		if (Creatures == null)
		{
			GD.PushError("No Deck set! Initializing to the Starter Deck.");

			// TODO: Load a deck instead of the whole card pool
			var cardPool = GameLoader.LoadCardPool(Constants.StarterDeckResourcePath);
			var sacrificeCards = cardPool.Cards.Where(c => c.Rarity == CardRarity.Sacrifice);
			var creatureCards = cardPool.Cards.Where(c => c.Rarity != CardRarity.Sacrifice);
			Sacrifices = new Deck(sacrificeCards, "Sacrifices");
			Creatures = new Deck(creatureCards, "Creatures");
		}

		const int StartingHandSize = 3;
		await ToSignal(GetTree().CreateTimer(0.20f), "timeout");

		for (int i = 0; i < StartingHandSize; i++)
		{
			var drawnCardInfo = Creatures.DrawFromTop();
			InstantiateCardInHand(drawnCardInfo);

			await ToSignal(GetTree().CreateTimer(0.234f), "timeout");
		}

		TransitionToState(GameState.DrawCard);
	}

	private void TransitionToState(GameState nextState)
	{
		GD.Print($"State Transition: {CurrentState} -> {nextState}");

		// Godot requires enums be converted to int - so they are valid variants.
		EmitSignal(SignalName.OnStateTransition, (int)nextState, (int)CurrentState);
		CurrentState = nextState;
	}

	private static int DrawnCardCount = 0;
	private Card InstantiateCardInHand(CardInfo cardInfo)
	{
		var card = Constants.CardScene.Instantiate<Card>();
		card.Name = $"Card_{DrawnCardCount++}";
		card.GlobalPosition = Hand.GlobalPosition + new Vector2(300, 0);

		card.SetCardInfo(cardInfo);
		CardManager.Instance.SetCardDrop(card, Hand);

		return card;
	}

	/** Isaac Mode! */
	public void Debug_DrawCard()
	{
		var blueMonsterAvatars = new[] {
			"res://assets/sprites/avatars/avatar_blue_monster_00.jpeg",
			"res://assets/sprites/avatars/avatar_blue_monster_01.jpeg",
			"res://assets/sprites/avatars/avatar_blue_monster_02.jpeg",
		};

		var blueMonsterCard = new CardInfo()
		{
			Name = $"Blue Monster #{DrawnCardCount}",
			AvatarResource = blueMonsterAvatars[Random.Shared.Next(blueMonsterAvatars.Length)],
			Attack = Random.Shared.Next(1, 6),
			Health = Random.Shared.Next(1, 11),
			BloodCost = (CardBloodCost)Random.Shared.Next(1, 4),
			Rarity = CardRarity.Rare,
		};

		var card = InstantiateCardInHand(blueMonsterCard);
		card.AddToGroup("DebugCard");

		GD.Print($"Drawing card {blueMonsterCard.Name}");
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
}

public class StateMachineException : Exception
{
	public StateMachineException(string action, GameState currentState) :
		base($"Invalid State Transition [{currentState}] Cannot process action \"{action}\"")
	{ }
}
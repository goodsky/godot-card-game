using Godot;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/**
 * Main Game Manager: Handles State Machine + Transitions
 */
public enum GameState
{
	Initializing,
	DrawCard,
	PlayCard,
	PlayCard_PayPrice,
	PlayerCombat,
	EnemyPlayCard,
	EnemyCombat,
	EnemyStageCard,
	IsaacMode,
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

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
	{
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
				TransitionToState(GameState.PlayCard);
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
				TransitionToState(GameState.PlayCard);
				break;

			default:
				throw new StateMachineException(nameof(DrawCardFromSacrificeDeck), CurrentState);
		}
	}


	public void PlayedCard(Card card, PlayArea playArea, CardDrop oldHome)
	{
		bool playAreaAlreadyHasCard = (playArea.CardCount == 2); // card cound is 2 because existing card + staged card
		if (card.CardInfo.BloodCost == CardBloodCost.Zero)
		{
			card.TargetPosition = playArea.GlobalPosition;
			TransitionToState(GameState.PlayCard);
		}
		else if (playAreaAlreadyHasCard)
		{
			ActiveCardState.Instance.StageCardPendingBloodCost(card, oldHome);
			TransitionToState(GameState.PlayCard_PayPrice);
			ActiveCardState.Instance.AddSacrificeCard(playArea.GetChildCards()[0]);
		}
		else
		{
			ActiveCardState.Instance.StageCardPendingBloodCost(card, oldHome);
			TransitionToState(GameState.PlayCard_PayPrice);
		}
	}

	public void CardCostPaid()
	{
		switch (CurrentState)
		{
			case GameState.PlayCard_PayPrice:
				TransitionToState(GameState.PlayCard);
				break;

			default:
				throw new StateMachineException(nameof(CardCostPaid), CurrentState);
		}
	}

	public void EndTurn()
	{
		switch (CurrentState)
		{
			case GameState.PlayCard:
				TransitionToState(GameState.PlayerCombat);
				break;

			default:
				throw new StateMachineException(nameof(EndTurn), CurrentState);
		}
	}

	public void EndPlayerCombat()
	{
		switch (CurrentState)
		{
			case GameState.PlayerCombat:
				TransitionToState(GameState.EnemyPlayCard);
				break;
			
			default:
				throw new StateMachineException(nameof(EndPlayerCombat), CurrentState);
		}
	}

	private async void InitializeGame()
	{
		if (IsaacMode)
		{
			TransitionToState(GameState.IsaacMode);
			Task t = this.StartCoroutine(Debug_TestCoroutine());

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
		await this.Delay(0.200);

		for (int i = 0; i < StartingHandSize; i++)
		{
			var drawnCardInfo = Creatures.DrawFromTop();
			InstantiateCardInHand(drawnCardInfo);

			await this.Delay(0.234);
		}

		TransitionToState(GameState.DrawCard);
	}

	private void TransitionToState(GameState nextState)
	{
		if (CurrentState == GameState.IsaacMode)
		{
			GD.Print($"Embrace the Isaac mode! Cannot transition to {nextState}");
		}

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
		ActiveCardState.Instance.SetCardDrop(card, Hand);

		return card;
	}

	/** Isaac Mode! (and other miscellaneous manual test routines) */
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
			ActiveCardState.Instance.SetCardDrop(card as Card, null);
			card.QueueFree();
		}
	}

	private IEnumerable Debug_TestCoroutine()
	{
		GD.Print("Testing the coroutine!");
		yield return new CoroutineDelay(2.0);
		GD.Print("I waited 2 seconds!");
		yield return null;
		GD.Print("And that time I didn't wait at all!");
		for (int i = 10; i > 0; i--)
		{
			GD.Print($"{i}...");
			yield return new CoroutineDelay(0.2);
		}

		GD.Print("Blastoff!");
		yield return new CoroutineDelay(5);
		GD.Print("Get ready for a big one...");
		yield return new CoroutineDelay(1);
		for (int i = 100; i > 0; i--)
		{
			GD.Print($"{i}...!");
			yield return null;
		}

		GD.Print("Okay I'm done! Bye!");
	}
	/** END Isaac Mode! */
}

public class StateMachineException : Exception
{
	public StateMachineException(string action, GameState currentState) :
		base($"Invalid State Transition [{currentState}] Cannot process action \"{action}\"")
	{ }
}
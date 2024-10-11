using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
	GameOver,
	IsaacMode,
}

public partial class MainGame : Node2D
{
	public static MainGame Instance { get; private set; }

	public int CurrentTurn { get; private set; }
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

	[Export]
	public Control PopUpParent { get; set; }

	public Deck Creatures { get; set; }

	public Deck Sacrifices { get; set; }

	public EnemyAI Opponent { get; set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		this.StartCoroutine(InitializeGameCoroutine());
	}

	public void Click_MainMenu()
	{
		SceneLoader.Instance.LoadMainMenu();
	}

	public void Click_Continue()
	{
		GameProgress progress = GameManager.Instance.Progress;
		GameManager.Instance.UpdateProgress(LobbyState.DraftCards, level: progress.Level + 1);
		SceneLoader.Instance.LoadGameLobby();
	}

	public void DrawCardFromDeck()
	{
		GD.Print("Draw card from deck...");
		switch (CurrentState)
		{
			case GameState.DrawCard:
				if (Creatures.Count == 0)
				{
					GD.Print("Deck empty!");
					return;
				}

				var drawnCardInfo = Creatures.DrawFromTop();
				InstantiateCardInHand(drawnCardInfo, Hand.GlobalPosition + new Vector2(300, 0));
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
				if (Sacrifices.Count == 0)
				{
					GD.Print("Deck empty!");
					return;
				}

				var drawnCardInfo = Sacrifices.DrawFromTop();
				InstantiateCardInHand(drawnCardInfo, Hand.GlobalPosition + new Vector2(300, 0));
				TransitionToState(GameState.PlayCard);
				break;

			default:
				throw new StateMachineException(nameof(DrawCardFromSacrificeDeck), CurrentState);
		}
	}

	public void SkipDrawingCard()
	{
		if (Creatures.Count != 0 || Sacrifices.Count != 0)
		{
			GD.PushError("Skipped draw card state but cards are still available!");
		}
		TransitionToState(GameState.PlayCard);
	}

	public void PlayedCard(Card card, PlayArea playArea, CardDrop oldHome)
	{
		bool playAreaAlreadyHasCard = (playArea.CardCount == 2); // card cound is 2 because existing card + staged card
		if (card.Info.BloodCost == CardBloodCost.Zero)
		{
			card.TargetPosition = playArea.GlobalPosition;
			TransitionToState(GameState.PlayCard);

			AudioManager.Instance.Play(Constants.Audio.PlayCardClick, tweak: true);
		}
		else if (playAreaAlreadyHasCard)
		{
			ActiveCardState.Instance.StageCardPendingBloodCost(card, oldHome);
			TransitionToState(GameState.PlayCard_PayPrice);
			ActiveCardState.Instance.AddSacrificeCard(playArea.GetChildCards()[0]);

			AudioManager.Instance.Play(Constants.Audio.ProposeCardClick, tweak: true);
		}
		else
		{
			ActiveCardState.Instance.StageCardPendingBloodCost(card, oldHome);
			TransitionToState(GameState.PlayCard_PayPrice);

			AudioManager.Instance.Play(Constants.Audio.ProposeCardClick, tweak: true);
		}
	}

	public void CardCostPaid()
	{
		switch (CurrentState)
		{
			case GameState.PlayCard_PayPrice:
				TransitionToState(GameState.PlayCard);

				AudioManager.Instance.Play(Constants.Audio.PlayCardClick, tweak: true);
				break;

			case GameState.GameOver:
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

				AudioManager.Instance.Play(Constants.Audio.TurnEnd);
				break;

			case GameState.GameOver:
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

			case GameState.GameOver:
				break;

			default:
				throw new StateMachineException(nameof(EndPlayerCombat), CurrentState);
		}
	}

	public void OpponentDoneStagingCards()
	{
		switch (CurrentState)
		{
			case GameState.EnemyStageCard:
				CurrentTurn++;

				if (Creatures.Count > 0 || Sacrifices.Count > 0)
				{
					TransitionToState(GameState.DrawCard);
				}
				else
				{
					TransitionToState(GameState.PlayCard);
				}

				AudioManager.Instance.Play(Constants.Audio.TurnEnd);
				break;

			case GameState.GameOver:
				break;

			default:
				throw new StateMachineException(nameof(OpponentDoneStagingCards), CurrentState);
		}
	}

	public void OpponentDonePlayingCards()
	{
		switch (CurrentState)
		{
			case GameState.EnemyPlayCard:
				TransitionToState(GameState.EnemyCombat);
				break;

			case GameState.GameOver:
				break;

			default:
				throw new StateMachineException(nameof(OpponentDonePlayingCards), CurrentState);
		}
	}

	public void EndOpponentCombat()
	{
		switch (CurrentState)
		{
			case GameState.EnemyCombat:
				TransitionToState(GameState.EnemyStageCard);
				break;

			case GameState.GameOver:
				break;

			default:
				throw new StateMachineException(nameof(EndOpponentCombat), CurrentState);
		}
	}

	public void GameOver()
	{
		bool playerWon = Instance.HealthBar.PlayerPoints > 0;
		if (!playerWon)
		{
			GameManager.Instance.ClearGame();
		}

		TransitionToState(GameState.GameOver);
	}

	private void TransitionToState(GameState nextState)
	{
		if (CurrentState == GameState.IsaacMode)
		{
			GD.Print($"Embrace the Isaac mode! Cannot transition to {nextState}.");
			return;
		}

		if (CurrentState == GameState.GameOver)
		{
			GD.Print($"Game is over. Skipping transition to {nextState}.");
			return;
		}

		GD.Print($"State Transition: {CurrentState} -> {nextState}");

		GameState lastState = CurrentState;
		CurrentState = nextState;
		EmitSignal(SignalName.OnStateTransition, (int)nextState, (int)lastState); // Godot requires enums be converted to int - so they are valid variants.
	}

	private static int DrawnCardCount = 0;
	private Card InstantiateCardInHand(CardInfo cardInfo, Vector2 deckGlobalPosition)
	{
		var card = Constants.CardScene.Instantiate<Card>();
		string nodeName = cardInfo.Name.Replace(" ", "_");
		card.Name = $"{nodeName}_{DrawnCardCount++}";
		card.Info = cardInfo;
		card.GlobalPosition = deckGlobalPosition;
		
		ActiveCardState.Instance.SetCardDrop(card, Hand);
		AudioManager.Instance.Play(Constants.Audio.CardWhoosh, tweak: true);

		return card;
	}

	private IEnumerable InitializeGameCoroutine()
	{
		yield return AudioManager.Instance.Play(Constants.Audio.CardsShuffle, pitch: 1.25f, volume: .9f);

		if (IsaacMode)
		{
			TransitionToState(GameState.IsaacMode);
			for (int i = 0; i < 3; i++)
			{
				Isaac_DrawCard();
				yield return new CoroutineDelay(0.25f);
			}
		}
		else
		{
			const int StartingHandSize = 3;
			CurrentTurn = 0;

			if (Creatures == null)
			{
				GD.PushError("No Deck set! Initializing to the Starter Deck.");

				// TODO: Load a deck instead of the whole card pool
				var cardPool = GameLoader.LoadCardPool(Constants.StarterDeckResourcePath);
				var sacrificeCards = cardPool.Cards.Where(c => c.Rarity == CardRarity.Sacrifice);
				var creatureCards = cardPool.Cards.Where(c => c.Rarity != CardRarity.Sacrifice);
				Sacrifices = new Deck(sacrificeCards);
				Creatures = new Deck(creatureCards);

				var moves = new List<ScriptedMove> {
					new ScriptedMove(0, CardBloodCost.Zero, CardRarity.Common),
					new ScriptedMove(1, CardBloodCost.One, CardRarity.Common),
					new ScriptedMove(3, CardBloodCost.Two),
					new ScriptedMove(4, CardBloodCost.Two),
					new ScriptedMove(4, CardBloodCost.Two),
					new ScriptedMove(5, CardBloodCost.Three, CardRarity.Rare),
				};
				Opponent = new EnemyAI(new CardPool(creatureCards, "EnemyDeck"), moves);
			}

			yield return new CoroutineDelay(0.234f);
			for (int i = 0; i < StartingHandSize; i++)
			{
				var drawnCardInfo = Creatures.DrawFromTop();
				InstantiateCardInHand(drawnCardInfo, Hand.GlobalPosition + new Vector2(250, 65));
				yield return new CoroutineDelay(0.234f);
			}

			TransitionToState(GameState.EnemyStageCard);
		}
	}

	/** Isaac Mode! (and other miscellaneous manual test routines) */
	public void Isaac_DrawCard()
	{
		var blueMonsterAvatars = new[] {
			"res://assets/sprites/avatars/avatar_blue_monster_00.jpeg",
			"res://assets/sprites/avatars/avatar_blue_monster_01.jpeg",
			"res://assets/sprites/avatars/avatar_blue_monster_02.jpeg",
		};

		var blueMonsterCard = new CardInfo()
		{
			Id = DrawnCardCount,
			Name = $"Blue Monster",
			AvatarResource = blueMonsterAvatars[Random.Shared.Next(blueMonsterAvatars.Length)],
			Attack = Random.Shared.Next(1, 6),
			Health = Random.Shared.Next(1, 11),
			BloodCost = (CardBloodCost)Random.Shared.Next(1, 4),
			Rarity = CardRarity.Rare,
		};

		var card = InstantiateCardInHand(blueMonsterCard, Hand.GlobalPosition + new Vector2(250, 65));
		card.AddToGroup("DebugCard");

		GD.Print($"Drawing card {blueMonsterCard.Name}");
	}

	public void Isaac_ClearCards()
	{
		var debugCards = GetTree().GetNodesInGroup("DebugCard");
		foreach (var card in debugCards)
		{
			ActiveCardState.Instance.SetCardDrop(card as Card, null);
			card.QueueFree();
		}
	}

	public void Isaac_CheckCards()
	{
		PlayArea[] allAreas = Board.FindChildren("PlayArea*").Select(x => x as PlayArea).Where(x => x != null).ToArray();
		bool isAllFull = allAreas.All(playArea => playArea.CardCount > 0);
		foreach (var playArea in allAreas)
		{
			foreach (var card in playArea.GetChildCards())
			{
				if (isAllFull)
				{
					card.StartShaking();
				}
				else
				{
					card.StopShaking();
				}
			}
		}

		if (isAllFull)
		{
			AudioManager.Instance.Play(Constants.Audio.GameOver_Win, volume: .5f);
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum LobbyState
{
	Initializing,
	GenerateCardPool,
	GenerateStartingDeck,
	SelectLevel,
	DraftResource,
	DraftCreature,
	DraftUncommonCreature,
	DraftRareCreature,
	RemoveCard,
	IncreaseHandSize,
	PlayGame,
}

public partial class GameLobby : Control
{
	public LobbyState CurrentState { get; private set; } = LobbyState.Initializing;

	public bool IsNewGame { get; set; } = false;

	[Export]
	public int StartingDeckSize { get; set; } = 6;

	[Export]
	public bool AutoStartingDeck { get; set; }

	[Export]
	public CanvasItem GeneratingCardsPanel { get; set; }

	[Export]
	public CanvasItem HelloDeckContainer { get; set; }

	[Export]
	public CanvasItem DraftCardsContainer { get; set; }

	[Export]
	public CanvasItem RemoveCardContainer { get; set; }

	[Export]
	public CanvasItem StatUpPanel { get; set; }

	[Export]
	public CanvasItem SelectLevelContainer { get; set; }

	[Export]
	public BaseButton BackButton { get; set; }

	[Export]
	public BaseButton ShowDeckButton { get; set; }

	public override void _Ready()
	{
		BaseButton[] allButtons = FindChildren("*Button").Select(x => x as BaseButton).Where(x => x != null).ToArray();
		foreach (BaseButton button in allButtons)
		{
			button.MouseEntered += () => HoverOverButton(button);
		}

		if (IsNewGame || GameManager.Instance.Progress == null)
		{
			TransitionToState(LobbyState.GenerateCardPool);
		}
		else
		{
			TransitionToState(GameManager.Instance.Progress.CurrentState);
		}
	}

	public void DraftCard(CardInfo cardInfo)
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);

		switch (CurrentState)
		{
			case LobbyState.DraftResource:
			case LobbyState.DraftCreature:
			case LobbyState.DraftUncommonCreature:
			case LobbyState.DraftRareCreature:
				var deck = GameManager.Instance.Progress.DeckCards;
				deck.Add(cardInfo);

				if (GameManager.Instance.Progress.Level == 1 &&
					deck.Count < StartingDeckSize)
				{
					TransitionToState(LobbyState.DraftCreature);
				}
				else
				{
					GameManager.Instance.UpdateProgress(LobbyState.SelectLevel, updatedDeck: deck);
					TransitionToState(LobbyState.SelectLevel);
				}
				break;

			default:
				throw new LobbyStateMachineException(nameof(DraftCard), CurrentState);
		}
	}

	public void RemoveCard(CardInfo cardInfo)
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);

		switch (CurrentState)
		{
			case LobbyState.RemoveCard:
				var deck = GameManager.Instance.Progress.DeckCards;
				deck.Remove(cardInfo);

				GameManager.Instance.UpdateProgress(LobbyState.SelectLevel, updatedDeck: deck);
				TransitionToState(LobbyState.SelectLevel);
				break;

			default:
				throw new LobbyStateMachineException(nameof(RemoveCard), CurrentState);
		}
	}

	public void ContinueToSelectLevel()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);

		switch (CurrentState)
		{
			case LobbyState.GenerateStartingDeck:
			case LobbyState.DraftResource:
			case LobbyState.DraftCreature:
			case LobbyState.DraftUncommonCreature:
			case LobbyState.DraftRareCreature:
			case LobbyState.RemoveCard:
				TransitionToState(LobbyState.SelectLevel);
				break;

			default:
				throw new LobbyStateMachineException(nameof(ContinueToSelectLevel), CurrentState);
		}
	}

	public void SelectLevel(GameLevel level)
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);

		switch (CurrentState)
		{
			case LobbyState.SelectLevel:
				// Reset the random seed to the one that generated this level
				// Note: the StartGame method will then regenerate this level again
				GameManager.Instance.ResetRandomSeed(level.Seed);
				TransitionToState(LobbyState.PlayGame);
				break;

			default:
				throw new LobbyStateMachineException(nameof(SelectLevel), CurrentState);
		}
	}

	public void Click_DeckButton()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		List<CardInfo> deck = GameManager.Instance.Progress.DeckCards;
		DeckPopUp.PopUp(this, deck);
	}

	public void Click_GoBack()
	{
		AudioManager.Instance.Play(Constants.Audio.ClickSnap, pitch: 1.0f, volume: 0.5f);
		SceneLoader.Instance.LoadMainMenu();
	}

	public void HoverOverButton(BaseButton button)
	{
		if (!button.Disabled)
		{
			AudioManager.Instance.Play(Constants.Audio.BalloonSnap, pitch: 1.0f, volume: 0.5f);
		}
	}

	private async void TransitionToState(LobbyState nextState)
	{
		var lastState = CurrentState;
		CurrentState = nextState;

		switch (lastState)
		{
			case LobbyState.GenerateCardPool:
				GeneratingCardsPanel.Visible = false;
				break;

			case LobbyState.GenerateStartingDeck:
				await this.StartCoroutine(FadeOutStartingDeckCoroutine(fadeOutSpeed: 0.025f));
				BackButton.Visible = true;
				ShowDeckButton.Visible = true;
				break;

			case LobbyState.DraftResource:
			case LobbyState.DraftCreature:
			case LobbyState.DraftUncommonCreature:
			case LobbyState.DraftRareCreature:
				await this.StartCoroutine(FadeOutDraftCardsCoroutine(fadeOutSpeed: 0.025f));
				break;

			case LobbyState.RemoveCard:
				await this.StartCoroutine(FadeOutRemoveCardCoroutine(fadeOutSpeed: 0.025f));
				break;

			case LobbyState.IncreaseHandSize:
				await this.StartCoroutine(FadeOutStatUpCoroutine(fadeOutSpeed: 0.025f));
				break;
		}

		switch (nextState)
		{
			case LobbyState.GenerateCardPool:
				BackButton.Visible = false;
				ShowDeckButton.Visible = false;
				await this.StartCoroutine(GenerateCardPoolCoroutine(TimeSpan.FromSeconds(2.5)));
				break;

			case LobbyState.GenerateStartingDeck:
				await this.StartCoroutine(GenerateStartingDeckCoroutine(fadeInSpeed: 0.05f));
				break;

			case LobbyState.SelectLevel:
				GameManager.Instance.UpdateProgress(LobbyState.SelectLevel, updateSeed: true);
				await this.StartCoroutine(SelectLevelsCoroutine(fadeInSpeed: 0.05f));
				break;

			case LobbyState.DraftResource:
				GameManager.Instance.UpdateProgress(LobbyState.DraftResource, updateSeed: true);
				await this.StartCoroutine(DraftCardsCoroutine(fadeInSpeed: 0.05f, CardRarity.Sacrifice));
				break;

			case LobbyState.DraftCreature:
				GameManager.Instance.UpdateProgress(LobbyState.DraftCreature, updateSeed: true);
				await this.StartCoroutine(DraftCardsCoroutine(fadeInSpeed: 0.05f, CardRarity.Common));
				break;

			case LobbyState.DraftUncommonCreature:
				GameManager.Instance.UpdateProgress(LobbyState.DraftUncommonCreature, updateSeed: true);
				await this.StartCoroutine(DraftCardsCoroutine(fadeInSpeed: 0.05f, CardRarity.Uncommon));
				break;

			case LobbyState.DraftRareCreature:
				GameManager.Instance.UpdateProgress(LobbyState.DraftRareCreature, updateSeed: true);
				await this.StartCoroutine(DraftCardsCoroutine(fadeInSpeed: 0.05f, CardRarity.Rare));
				break;

			case LobbyState.RemoveCard:
				GameManager.Instance.UpdateProgress(LobbyState.RemoveCard, updateSeed: true);
				await this.StartCoroutine(RemoveCardCoroutine(fadeInSpeed: 0.05f));
				break;

			case LobbyState.IncreaseHandSize:
				int currentHandSize = GameManager.Instance.Progress.HandSize;
				GameManager.Instance.UpdateProgress(LobbyState.SelectLevel, updateSeed: true, handSize: currentHandSize + 1);
				await this.StartCoroutine(StatUpCoroutine(fadeInSpeed: 0.05f, "Hand Size Up!"));
				break;

			case LobbyState.PlayGame:
				GameManager.Instance.UpdateProgress(LobbyState.PlayGame, updateSeed: true);
				var (sacrificeDeck, creatureDeck, gameLevel) = InitializeGame();
				SceneLoader.Instance.LoadMainGame(sacrificeDeck, creatureDeck, gameLevel);
				break;
		}
	}

	public static (Deck, Deck, GameLevel) InitializeGame()
	{
		GameProgress progress = GameManager.Instance.Progress;
		RandomGenerator rnd = GameManager.Instance.Random;
		GD.Print("Starting game with seed", rnd.Seed);

		var gameLevel = GenerateGameLevel(progress.CardPool, progress.Level, rnd.Seed);

		var sacrificeCards = progress.DeckCards.Where(c => c.Rarity == CardRarity.Sacrifice);
		var creatureCards = progress.DeckCards.Where(c => c.Rarity != CardRarity.Sacrifice);
		var sacrificeDeck = new Deck(sacrificeCards, rnd);
		var creatureDeck = new Deck(creatureCards, rnd);

		return (sacrificeDeck, creatureDeck, gameLevel);
	}

	private IEnumerable GenerateCardPoolCoroutine(TimeSpan delay)
	{
		TextureProgressBar spinner = GeneratingCardsPanel.FindChild("SpinningProgressBar") as TextureProgressBar;

		yield return new CoroutineDelay(0.5);

		GeneratingCardsPanel.Visible = true;
		this.StartCoroutine(SpinProgressBarCoroutine(spinner, 5f));

		DateTime start = DateTime.Now;
		string cardPoolName = $"cards-{start:yyyyMMdd-HHmmss}";

		var cardPool = CardGenerator.GenerateRandomCardPool(cardPoolName);
		GameLoader.SaveCardPool(cardPool, cardPoolName);
		GameManager.Instance.StartNewGame(cardPool);

		TimeSpan delaySoFar = DateTime.Now - start;
		yield return new CoroutineDelay((delay - delaySoFar).TotalSeconds);

		if (AutoStartingDeck)
		{
			TransitionToState(LobbyState.GenerateStartingDeck);
		}
		else
		{
			TransitionToState(LobbyState.DraftCreature);
		}
	}

	private IEnumerable SpinProgressBarCoroutine(TextureProgressBar spinner, float speed)
	{
		while (GeneratingCardsPanel.Visible)
		{
			spinner.RadialInitialAngle += speed;
			yield return null;
		}
	}

	private IEnumerable GenerateStartingDeckCoroutine(float fadeInSpeed)
	{
		Node cardsContainer = HelloDeckContainer.FindChild("CardButtonContainer");
		Label helloLabel = HelloDeckContainer.FindChild("SayHello") as Label;

		foreach (Node child in cardsContainer.GetChildren())
		{
			child.QueueFree();
		}

		var labelOptions = new[] {
			"Here is your new deck!",
			"Say hello to your new team!",
			"I have a good feeling about this deck!",
			"Deck Generated. Let's Play!",
			"These cards want to go with you.",
			"Here is what you're starting with.",
		};

		RandomGenerator rnd = GameManager.Instance.Random;
		helloLabel.Text = labelOptions[rnd.Next(labelOptions.Length)];

		List<CardInfo> startingDeck;
		bool startingDeckIsAcceptable = false;
		do
		{
			startingDeck = GenerateStartingDeck(GameManager.Instance.Progress.CardPool, StartingDeckSize, rnd);

			int startingDeckAttack = startingDeck.Sum(info => info.Attack);
			startingDeckIsAcceptable = startingDeckAttack >= 3;
		}
		while (!startingDeckIsAcceptable);

		GameManager.Instance.UpdateProgress(LobbyState.SelectLevel, updatedDeck: startingDeck);

		foreach (CardInfo cardInfo in startingDeck)
		{
			var card = Constants.CardButtonScene.Instantiate<CardButton>();
			card.ShowCardBack = false;
			card.SetDisabled(true, fade: false);
			card.SetCard(cardInfo);
			cardsContainer.AddChild(card);
		}

		HelloDeckContainer.Visible = true;
		yield return HelloDeckContainer.FadeTo(1, startAlpha: 0, speed: fadeInSpeed);
	}

	private IEnumerable FadeOutStartingDeckCoroutine(float fadeOutSpeed)
	{
		yield return HelloDeckContainer.FadeTo(0, startAlpha: 1, speed: fadeOutSpeed);
		HelloDeckContainer.Visible = false;
	}

	private IEnumerable SelectLevelsCoroutine(float fadeInSpeed)
	{
		// This acts like a "preview of possible levels" - so we try a bunch of random seeds 
		// But only the one the user selects will be saved as the random seed for the level
		Node levelsContainer = SelectLevelContainer.FindChild("SelectLevelContainer");
		Label levelLabel = SelectLevelContainer.FindChild("LevelLabel") as Label;
		levelLabel.Text = GameManager.Instance.Progress.Level.ToString();

		foreach (Node child in levelsContainer.GetChildren())
		{
			child.QueueFree();
		}

		GameProgress progress = GameManager.Instance.Progress;
		RandomGenerator rnd = GameManager.Instance.Random;
		List<GameLevel> levels = GenerateGameLevelPool(progress.CardPool, progress.Level, rnd);

		foreach (GameLevel level in levels)
		{
			var levelPanel = Constants.SelectLevelScene.Instantiate<SelectLevelPanel>();
			levelPanel.Configure(level.Difficulty, level.Reward, () => SelectLevel(level));

			levelsContainer.AddChild(levelPanel);
		}

		SelectLevelContainer.Visible = true;
		yield return SelectLevelContainer.FadeTo(1, startAlpha: 0, speed: fadeInSpeed);
	}

	private IEnumerable DraftCardsCoroutine(float fadeInSpeed, CardRarity rarity)
	{
		Node cardsContainer = DraftCardsContainer.FindChild("CardButtonContainer");
		Button skipButton = DraftCardsContainer.FindChild("SkipButton") as Button;

		foreach (Node child in cardsContainer.GetChildren())
		{
			child.QueueFree();
		}

		var progress = GameManager.Instance.Progress;
		var rnd = GameManager.Instance.Random;
		var candidateCards = SelectRandomCards(progress.CardPool.Cards, count: 3, rnd, rarity);

		foreach (CardInfo cardInfo in candidateCards)
		{
			var card = Constants.CardButtonScene.Instantiate<CardButton>();
			card.ShowCardBack = false;
			card.SetCard(cardInfo);
			card.Pressed += () => DraftCard(cardInfo);
			cardsContainer.AddChild(card);
		}

		skipButton.Visible = progress.Level > 1;

		DraftCardsContainer.Visible = true;
		yield return DraftCardsContainer.FadeTo(1, startAlpha: 0, speed: fadeInSpeed);
	}

	private IEnumerable FadeOutDraftCardsCoroutine(float fadeOutSpeed)
	{
		yield return DraftCardsContainer.FadeTo(0, startAlpha: 1, speed: fadeOutSpeed);
		DraftCardsContainer.Visible = false;
	}

	private IEnumerable RemoveCardCoroutine(float fadeInSpeed)
	{
		Node cardsContainer = RemoveCardContainer.FindChild("CardButtonContainer");
		Button skipButton = RemoveCardContainer.FindChild("SkipButton") as Button;

		foreach (Node child in cardsContainer.GetChildren())
		{
			child.QueueFree();
		}

		var progress = GameManager.Instance.Progress;
		var rnd = GameManager.Instance.Random;
		var candidateCards = SelectRandomCards(progress.DeckCards, count: 5, rnd);

		foreach (CardInfo cardInfo in candidateCards)
		{
			var card = Constants.CardButtonScene.Instantiate<CardButton>();
			card.ShowCardBack = false;
			card.SetCard(cardInfo);
			card.Pressed += () => RemoveCard(cardInfo);
			cardsContainer.AddChild(card);
		}

		RemoveCardContainer.Visible = true;
		yield return RemoveCardContainer.FadeTo(1, startAlpha: 0, speed: fadeInSpeed);
	}

	private IEnumerable FadeOutRemoveCardCoroutine(float fadeOutSpeed)
	{
		yield return RemoveCardContainer.FadeTo(0, startAlpha: 1, speed: fadeOutSpeed);
		RemoveCardContainer.Visible = false;
	}

	private IEnumerable StatUpCoroutine(float fadeInSpeed, string message)
	{
		Label label = StatUpPanel.FindChild("MessageLabel") as Label;

		label.Text = message;

		StatUpPanel.Visible = true;
		yield return StatUpPanel.FadeTo(1, startAlpha: 0, speed: fadeInSpeed);
	}

	private IEnumerable FadeOutStatUpCoroutine(float fadeOutSpeed)
	{
		yield return StatUpPanel.FadeTo(0, startAlpha: 1, speed: fadeOutSpeed);
		StatUpPanel.Visible = false;
	}

	public static IEnumerable<CardInfo> SelectRandomCards(List<CardInfo> cards, int count, RandomGenerator rnd, CardRarity? rarity = null)
	{
		var cardOptions = cards
			.Where(c => rarity == null || c.Rarity == rarity)
			.ToList();

		if (cardOptions.Count == 0)
		{
			throw new Exception("ABORT! We don't have enough cards to draft!");
		}

		if (cardOptions.Count <= count)
		{
			return cardOptions;
		}

		var draftPool = new List<CardInfo>();
		for (int i = 0; i < count; i++)
		{
			int rndIdx = rnd.Next(cardOptions.Count);
			CardInfo candidate = cardOptions[rndIdx];
			draftPool.Add(candidate);
			cardOptions.RemoveAt(rndIdx);
		}

		return draftPool;
	}

	public static List<CardInfo> GenerateStartingDeck(CardPool cardPool, int startingDeckSize, RandomGenerator rnd)
	{
		var deck = new List<CardInfo>();

		// 3 sacrifices (so you can afford a 3 cost card)
		// 20-40% starting deck is one cost cards
		// remainder is random 2-3 cost cards
		int sacrificeCount = 3;
		int oneCostCount = Mathf.CeilToInt(rnd.Nextf(0.2f, 0.4f) * startingDeckSize);
		int otherCount = startingDeckSize - sacrificeCount - oneCostCount;

		GD.Print($"Starting deck: {sacrificeCount} sacrifices; {oneCostCount} one cost; {otherCount} other;");

		var sacrificeCards = cardPool.Cards.Where(c => c.Rarity == CardRarity.Sacrifice).ToList();
		for (int i = 0; i < sacrificeCount; i++)
		{
			int rndIdx = rnd.Next(sacrificeCards.Count);
			deck.Add(sacrificeCards[rndIdx]);
			sacrificeCards.RemoveAt(rndIdx);
		}

		var oneCostCards = cardPool.Cards.Where(c => c.BloodCost == CardBloodCost.One && c.Rarity == CardRarity.Common).ToList();
		for (int i = 0; i < oneCostCount; i++)
		{
			int rndIdx = rnd.Next(oneCostCards.Count);
			deck.Add(oneCostCards[rndIdx]);
			oneCostCards.RemoveAt(rndIdx);
		}

		var otherCards = cardPool.Cards.Where(c => c.BloodCost != CardBloodCost.Zero && c.BloodCost != CardBloodCost.One && c.Rarity == CardRarity.Common).ToList();
		for (int i = 0; i < otherCount; i++)
		{
			int rndIdx = rnd.Next(otherCards.Count);
			deck.Add(otherCards[rndIdx]);
			otherCards.RemoveAt(rndIdx);
		}

		return deck;
	}

	public static List<GameLevel> GenerateGameLevelPool(CardPool cardPool, int level, RandomGenerator rnd)
	{
		int levelCount = level < 4 ? 2 : rnd.SelectRandomOdds(new[] { 2, 3 }, new[] { 50, 50 });

		var gameLevels = new List<GameLevel>();
		while (gameLevels.Count < levelCount)
		{
			int seed = rnd.Next();
			var gameLevel = GenerateGameLevel(cardPool, level, seed);

			if (gameLevel.Difficulty == LevelDifficulty.FailedGuardrail ||
				(level == 1 && gameLevel.Difficulty != LevelDifficulty.Easy) ||
				(level <= 3 && gameLevel.Difficulty == LevelDifficulty.Hard))
			{
				continue;
			}

			gameLevels.Add(gameLevel);
		}

		// Deduplicate levels that appear the same (sorry duplicate levels)
		for (int i = gameLevels.Count - 1; i > 0; i--)
		{
			for (int j = 0; j < i; j++)
			{
				var level1 = gameLevels[i];
				var level2 = gameLevels[j];
				if (level1.Difficulty == level2.Difficulty &&
					level1.Reward == level2.Reward)
				{
					GD.Print("Removing Duplicate Level #", i);
					gameLevels.RemoveAt(i);
					break;
				}
			}
		}

		return gameLevels;
	}

	public static GameLevel GenerateGameLevel(CardPool cardPool, int level, int seed)
	{
		var rnd = new RandomGenerator(seed);
		var ai = GenerateEnemyAI(cardPool, level, rnd);
		var difficulty = CalculateLevelDifficulty(cardPool, ai.Clone(), level);
		var reward = GenerateLevelReward(difficulty, rnd);
		
		return new GameLevel
		{
			Level = level,
			Seed = seed,
			AI = ai,
			Difficulty = difficulty,
			Reward = reward,
		};
	}

	private struct LevelState
	{
		public int Turn { get; set; }
		public int TotalAttack { get; set; }
		public int TotalHealth { get; set; }
		public int TotalCards { get; set; }
	}
	private class DifficultyMarker
	{
		public string Name { get; set; }
		public LevelDifficulty Marker { get; set; }
		public Func<LevelState, bool> Check { get; set; }
	}
	public static LevelDifficulty CalculateLevelDifficulty(CardPool cardPool, EnemyAI ai, int level)
	{
		var markers = new[] {
			/* Invalid Level Markers */
			new DifficultyMarker {
				Name = "Must play a card before turn 2",
				Marker = LevelDifficulty.FailedGuardrail,
				Check = (step) => step.Turn == 1 && step.TotalCards == 0,
			},
			new DifficultyMarker {
				Name = "Must play at least 1 attack before turn 3",
				Marker = LevelDifficulty.FailedGuardrail,
				Check = (step) => step.Turn == 2 && step.TotalAttack == 0,
			},
			new DifficultyMarker {
				Name = "Don't play more than two cards the first turn",
				Marker = LevelDifficulty.FailedGuardrail,
				Check = (step) => step.Turn == 0 && step.TotalCards > 2,
			},
			/* Hard Level Markers */
			new DifficultyMarker {
				Name = "Played 5 attack before turn 2",
				Marker = LevelDifficulty.Hard,
				Check = (step) => step.Turn == 1 && step.TotalAttack >= 5,
			},
			new DifficultyMarker {
				Name = "Played 4 cards before turn 2",
				Marker = LevelDifficulty.Hard,
				Check = (step) => step.Turn == 1 && step.TotalCards >= 4,
			},
			new DifficultyMarker {
				Name = "Played 10 attack before turn 4",
				Marker = LevelDifficulty.Hard,
				Check = (step) => step.Turn == 3 && step.TotalAttack >= 12,
			},
			new DifficultyMarker {
				Name = "Played 8 cards before turn 4",
				Marker = LevelDifficulty.Hard,
				Check = (step) => step.Turn == 3 && step.TotalCards >= 8,
			},
			/* Medium Level Markers */
			new DifficultyMarker {
				Name = "Played 3 attack before turn 2",
				Marker = LevelDifficulty.Medium,
				Check = (step) => step.Turn == 1 && step.TotalAttack >= 3,
			},
			new DifficultyMarker {
				Name = "Played 3 cards before turn 2",
				Marker = LevelDifficulty.Medium,
				Check = (step) => step.Turn == 1 && step.TotalCards >= 3,
			},
			new DifficultyMarker {
				Name = "Played 6 attack before turn 4",
				Marker = LevelDifficulty.Medium,
				Check = (step) => step.Turn == 3 && step.TotalAttack >= 6,
			},
			new DifficultyMarker {
				Name = "Played 6 cards before turn 4",
				Marker = LevelDifficulty.Medium,
				Check = (step) => step.Turn == 3 && step.TotalCards >= 6,
			},
		};

		var markerCount = new Dictionary<LevelDifficulty, int>() { { LevelDifficulty.Easy, 0 }, { LevelDifficulty.Medium, 0 }, { LevelDifficulty.Hard, 0 }, { LevelDifficulty.FailedGuardrail, 0 } };
		var state = new LevelState();
		for (int turnId = 0; turnId <= ai.MaxTurn; turnId++)
		{
			state.Turn = turnId;
			List<PlayedCard> playedCards = ai.GetMovesForTurn(turnId, new bool[4]);
			foreach (var playedCard in playedCards)
			{
				CardInfo cardInfo = playedCard.Card;
				state.TotalAttack += cardInfo.Attack;
				state.TotalHealth += cardInfo.Health;
				state.TotalCards++;
			}

			foreach (DifficultyMarker marker in markers)
			{
				if (marker.Check(state))
				{
					GD.Print($"Generated Level has hit marker: {marker.Name}");
					markerCount[marker.Marker] += 1;
				}
			}
		}

		if (markerCount[LevelDifficulty.FailedGuardrail] > 0)
		{
			return LevelDifficulty.FailedGuardrail;
		}
		else if (markerCount[LevelDifficulty.Hard] > 0)
		{
			return LevelDifficulty.Hard;
		}
		else if (markerCount[LevelDifficulty.Medium] > 0)
		{
			return LevelDifficulty.Medium;
		}
		else
		{
			return LevelDifficulty.Easy;
		}
	}

	public static LevelReward GenerateLevelReward(LevelDifficulty difficulty, RandomGenerator rnd)
	{
		if (difficulty == LevelDifficulty.Hard)
		{
			return rnd.SelectRandomOdds(
				new[] { LevelReward.AddCreature, LevelReward.AddUncommonCreature, LevelReward.AddRareCreature, LevelReward.RemoveCard, LevelReward.IncreaseHandSize },
				new[] { 20, 40, 20, 10, 10 }
			);
		}
		else if (difficulty == LevelDifficulty.Medium)
		{
			return rnd.SelectRandomOdds(
				new[] { LevelReward.AddCreature, LevelReward.AddUncommonCreature, LevelReward.AddRareCreature, LevelReward.RemoveCard, LevelReward.IncreaseHandSize },
				new[] { 50, 15, 05, 25, 05 }
			);
		}
		else
		{
			return rnd.SelectRandomOdds(
				new[] { LevelReward.AddResource, LevelReward.AddCreature },
				new[] { 20, 80 }
			);
		}
	}

	public static EnemyAI GenerateEnemyAI(CardPool cardPool, int level, RandomGenerator rnd)
	{
		const int TOTAL_CARDS_RATE = 1;
		int totalCards = LinearScale(level, TOTAL_CARDS_RATE, min: 4, max: 12, y_intercept: 4, random_amount: 1, rnd: rnd);

		int oneCardsProbability = LinearScale(level, 0f, min: 50, max: 50, y_intercept: 50, x_intercept: 1);
		int twoCardsProbability = LinearScale(level, 5f, min: 0, max: 20, x_intercept: 2);
		int threeCardsProbability = LinearScale(level, 2.5f, min: 0, max: 15, x_intercept: 6);
		int fourCardsProbability = LinearScale(level, 2.5f, min: 0, max: 10, x_intercept: 8);
		int zeroCardsProbability = 100 - oneCardsProbability - twoCardsProbability - threeCardsProbability - fourCardsProbability;

		int uncommonProbability = LinearScale(level, 2.5f, min: 0, max: 25);
		int rareProbability = LinearScale(level, 2.5f, min: 0, max: 25, x_intercept: 2);
		int commonProbability = 100 - uncommonProbability - rareProbability;

		// probability of card cost per turn Idx
		var oneCostProbabilities = new[] { 05, 50, 50, 50, 45, 40, 30, 20, 10, 00 };
		var twoCostProbabilities = new[] { 00, 05, 25, 45, 50, 50, 50, 50, 50, 50 };
		var threeCostProbabilities = new[] { 00, 00, 00, 05, 05, 10, 20, 30, 40, 50 };

		GD.Print($"Generating Enemy AI for level {level}: total={totalCards}; seed={rnd.Seed}[{rnd.N}]; concurrent probabilities=[{zeroCardsProbability:0.00}, {oneCardsProbability:0.00}, {twoCardsProbability:0.00}, {threeCardsProbability:0.00}, {fourCardsProbability:0.00}]; rarity probabilities=[{commonProbability:0.00},{uncommonProbability:0.00},{rareProbability:0.00}]");

		int turnId = 0;
		var moves = new List<ScriptedMove>();
		while (moves.Count < totalCards)
		{
			int concurrentCount = rnd.SelectRandomOdds(
				new[] { 0, 1, 2, 3, 4 },
				new[] { zeroCardsProbability, oneCardsProbability, twoCardsProbability, threeCardsProbability, fourCardsProbability }
			);

			int costProbabilityIdx = Math.Clamp(turnId, 0, 9);
			int oneCostProbability = oneCostProbabilities[costProbabilityIdx];
			int twoCostProbability = twoCostProbabilities[costProbabilityIdx];
			int threeCostProbability = threeCostProbabilities[costProbabilityIdx];
			int zeroCostProbability = 100 - oneCostProbability - twoCostProbability - threeCostProbability;

			GD.Print($"   Turn {turnId}: {concurrentCount} cards; cost probabilities=[{zeroCostProbability:0.00},{oneCostProbability:0.00},{twoCostProbability:0.00},{threeCostProbability:0.00}]");

			for (int i = 0; i < concurrentCount; i++)
			{
				CardBloodCost cost = rnd.SelectRandomOdds(
					new[] { CardBloodCost.Zero, CardBloodCost.One, CardBloodCost.Two, CardBloodCost.Three },
					new[] { zeroCostProbability, oneCostProbability, twoCostProbability, threeCostProbability }
				);

				CardRarity rarity = rnd.SelectRandomOdds(
					new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare },
					new[] { commonProbability, uncommonProbability, rareProbability }
				);

				if (cost == CardBloodCost.Zero && rarity == CardRarity.Common)
				{
					// Just use sacrifice for this
					rarity = CardRarity.Sacrifice;
				}

				GD.Print($"      {cost}:{rarity}");
				moves.Add(new ScriptedMove(turnId, cost, rarity));
			}

			turnId++;
		}

		return new EnemyAI(cardPool, moves, rnd);
	}

	public static float LinearScalef(
		float x,
		float rate,
		float min,
		float max,
		float x_intercept = 0,
		float y_intercept = 0,
		float random_amount = 0,
		RandomGenerator rnd = null)
	{
		float y = (x - x_intercept) * rate + y_intercept;
		if (rnd != null)
		{
			y += rnd.Nextf(-random_amount, random_amount);
		}

		return Mathf.Clamp(y, min, max);
	}

	public static int LinearScale(
		int x,
		float rate,
		int min,
		int max,
		int x_intercept = 0,
		int y_intercept = 0,
		int random_amount = 0,
		RandomGenerator rnd = null)
	{
		float y = LinearScalef(x, rate, min, max, x_intercept, y_intercept, random_amount, rnd);
		return Math.Clamp(Mathf.RoundToInt(y + 1e-6f), min, max);
	}
}

public class LobbyStateMachineException : Exception
{
	public LobbyStateMachineException(string action, LobbyState currentState) :
		base($"Invalid State Transition [{currentState}] Cannot process action \"{action}\"")
	{ }
}

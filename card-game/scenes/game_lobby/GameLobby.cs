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
	DraftCards,
	SelectLevel,
	PlayGame,
}

public partial class GameLobby : Control
{
	public LobbyState CurrentState { get; private set; } = LobbyState.Initializing;

	public bool IsNewGame { get; set; } = false;

	[Export]
	public int StartingDeckSize { get; set; } = 5;

	[Export]
	public bool AutoStartingDeck { get; set; }

	[Export]
	public CanvasItem GeneratingCardsPanel { get; set; }

	[Export]
	public CanvasItem HelloDeckContainer { get; set; }

	[Export]
	public CanvasItem DraftCardsContainer { get; set; }

	[Export]
	public CanvasItem PlayLevelPanel { get; set; }

	[Export]
	public BaseButton BackButton { get; set; }

	[Export]
	public BaseButton ShowDeckButton { get; set; }

	public override void _Ready()
	{
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
		switch (CurrentState)
		{
			case LobbyState.DraftCards:
				var deck = GameManager.Instance.Progress.DeckCards;
				deck.Add(cardInfo);

				if (GameManager.Instance.Progress.Level == 1 &&
					deck.Count < StartingDeckSize)
				{
					TransitionToState(LobbyState.DraftCards);
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

	public void SkipDraft()
	{
		switch (CurrentState)
		{
			case LobbyState.DraftCards:
				TransitionToState(LobbyState.SelectLevel);
				break;

			default:
				throw new LobbyStateMachineException(nameof(SkipDraft), CurrentState);
		}
	}

	public void ContinueToSelectLevel()
	{
		switch (CurrentState)
		{
			case LobbyState.GenerateStartingDeck:
				TransitionToState(LobbyState.SelectLevel);
				break;

			default:
				throw new LobbyStateMachineException(nameof(ContinueToSelectLevel), CurrentState);
		}
	}

	public void Click_DeckButton()
	{
		List<CardInfo> deck = GameManager.Instance.Progress.DeckCards;
		DeckPopUp.PopUp(this, deck);
	}

	public void Click_GoBack()
	{
		SceneLoader.Instance.LoadMainMenu();
	}

	public void StartGame()
	{
		CardPool cardPool = GameManager.Instance.Progress.CardPool;
		List<CardInfo> deck = GameManager.Instance.Progress.DeckCards;

		var sacrificeCards = deck.Where(c => c.Rarity == CardRarity.Sacrifice);
		var creatureCards = deck.Where(c => c.Rarity != CardRarity.Sacrifice);
		var sacrificeDeck = new Deck(sacrificeCards);
		var creatureDeck = new Deck(creatureCards);

		var progress = GameManager.Instance.Progress;
		var opponent = GenerateEnemyAI(cardPool, progress.Level, new RandomNumberGenerator());

		GameManager.Instance.UpdateProgress(LobbyState.PlayGame);
		SceneLoader.Instance.LoadMainGame(sacrificeDeck, creatureDeck, opponent);
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
				await this.StartCoroutine(FadeOutStartingDeckCoroutine(fadeOutSpeed: 0.05f));
				BackButton.Visible = true;
				ShowDeckButton.Visible = true;
				break;

			case LobbyState.DraftCards:
				await this.StartCoroutine(FadeOutDraftCardsCoroutine(fadeOutSpeed: 0.05f));
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

			case LobbyState.DraftCards:
				GameManager.Instance.UpdateProgress(LobbyState.DraftCards);
				await this.StartCoroutine(DraftCardsCoroutine(fadeInSpeed: 0.05f));
				break;

			case LobbyState.SelectLevel:
				GameManager.Instance.UpdateProgress(LobbyState.SelectLevel);
				Label levelLabel = PlayLevelPanel.FindChild("LevelNumber") as Label;
				levelLabel.Text = GameManager.Instance.Progress.Level.ToString();
				PlayLevelPanel.Visible = true;
				break;

			case LobbyState.PlayGame:
				GameManager.Instance.UpdateProgress(LobbyState.PlayGame);
				StartGame();
				break;
		}
	}

	private IEnumerable GenerateCardPoolCoroutine(TimeSpan delay)
	{
		TextureProgressBar spinner = GeneratingCardsPanel.FindChild("SpinningProgressBar") as TextureProgressBar;

		yield return new CoroutineDelay(0.5);

		GeneratingCardsPanel.Visible = true;
		this.StartCoroutine(SpinProgressBarCoroutine(spinner, 5f));

		DateTime start = DateTime.Now;
		string cardPoolName = $"cards-{start:yyyyMMdd-HHmmss}";

		var cardPool = CardGenerator.GenerateRandomCardPool(CardGenerator.DefaultArgs, cardPoolName);
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
			TransitionToState(LobbyState.DraftCards);
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

		helloLabel.Text = labelOptions[Random.Shared.Next(labelOptions.Length)];

		var startingDeck = GenerateStartingDeck(GameManager.Instance.Progress.CardPool, StartingDeckSize);
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

	private IEnumerable DraftCardsCoroutine(float fadeInSpeed)
	{
		Node cardsContainer = DraftCardsContainer.FindChild("CardButtonContainer");
		Button skipButton = DraftCardsContainer.FindChild("SkipButton") as Button;

		foreach (Node child in cardsContainer.GetChildren())
		{
			child.QueueFree();
		}

		var progress = GameManager.Instance.Progress;
		var candidateCards = SelectDraftPool(progress.CardPool, level: progress.Level, count: 3, rnd: new RandomNumberGenerator());
		
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

	public static IEnumerable<CardInfo> SelectDraftPool(CardPool cardPool, int level, int count, RandomNumberGenerator rnd)
	{
		const float UNCOMMON_RATE = 0.04f;
		const float RARE_RATE = 0.02f;

		float uncommonProbability = LinearScalef(level, UNCOMMON_RATE, min: 0.00f, max: 0.40f, x_intercept: 1);
		float rareProbability = LinearScalef(level, RARE_RATE, min: 0.00f, max: 0.20f, x_intercept: 1);
		float commonProbability = 1f - uncommonProbability - rareProbability;

		CardRarity rarity = PickOption(
			new[] { commonProbability, uncommonProbability, rareProbability },
			new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare },
			rnd
		);

		var cardOptions = cardPool.Cards
			.Where(c => c.Rarity == rarity || (rarity == CardRarity.Common && c.Rarity == CardRarity.Sacrifice))
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
			int rndIdx = rnd.RandiRange(0, cardOptions.Count - 1);
			CardInfo candidate = cardOptions[rndIdx];
			draftPool.Add(candidate);
			cardOptions.RemoveAt(rndIdx);

			// only one sacrifice per pool please
			if (candidate.Rarity == CardRarity.Sacrifice)
			{
				cardOptions = cardOptions.Where(c => c.Rarity == rarity).ToList();
				if (cardOptions.Count <= count)
				{
					draftPool.AddRange(cardOptions);
					break;
				}
			}
		}

		return draftPool;
	}

	public static List<CardInfo> GenerateStartingDeck(CardPool cardPool, int startingDeckSize)
	{
		var deck = new List<CardInfo>();

		// 20-40% starting deck is sacrifices
		// 20-40% starting deck is one cost cards
		// remainder is random 2-3 cost cards
		var rnd = new RandomNumberGenerator();
		int sacrificeCount = Mathf.CeilToInt(rnd.RandfRange(0.2f, 0.4f) * startingDeckSize);
		int oneCostCount = Mathf.CeilToInt(rnd.RandfRange(0.2f, 0.4f) * startingDeckSize);
		int otherCount = startingDeckSize - sacrificeCount - oneCostCount;

		GD.Print($"Starting deck: {sacrificeCount} sacrifices; {oneCostCount} one cost; {otherCount} other;");

		var sacrificeCards = cardPool.Cards.Where(c => c.Rarity == CardRarity.Sacrifice).ToList();
		for (int i = 0; i < sacrificeCount; i++)
		{
			int rndIdx = rnd.RandiRange(0, sacrificeCards.Count - 1);
			deck.Add(sacrificeCards[rndIdx]);
			sacrificeCards.RemoveAt(rndIdx);
		}

		var oneCostCards = cardPool.Cards.Where(c => c.BloodCost == CardBloodCost.One && c.Rarity == CardRarity.Common).ToList();
		for (int i = 0; i < oneCostCount; i++)
		{
			int rndIdx = rnd.RandiRange(0, oneCostCards.Count - 1);
			deck.Add(oneCostCards[rndIdx]);
			oneCostCards.RemoveAt(rndIdx);
		}

		var otherCards = cardPool.Cards.Where(c => c.BloodCost != CardBloodCost.Zero && c.BloodCost != CardBloodCost.One && c.Rarity == CardRarity.Common).ToList();
		for (int i = 0; i < otherCount; i++)
		{
			int rndIdx = rnd.RandiRange(0, otherCards.Count - 1);
			deck.Add(otherCards[rndIdx]);
			otherCards.RemoveAt(rndIdx);
		}

		return deck;
	}

	public static EnemyAI GenerateEnemyAI(CardPool cardPool, int level, RandomNumberGenerator rnd)
	{
		const int TOTAL_CARDS_RATE = 1;
		int totalCards = LinearScale(level, TOTAL_CARDS_RATE, min: 4, max: 12, y_intercept: 4, random_amount: 1, rnd: rnd);

		float oneCardsProbability = LinearScalef(level, 0f, min: 0.50f, max: 0.50f, y_intercept: 0.50f, x_intercept: 1);
		float twoCardsProbability = LinearScalef(level, 0.05f, min: 0.00f, max: 0.20f, x_intercept: 2);
		float threeCardsProbability = LinearScalef(level, 0.025f, min: 0.00f, max: 0.15f, x_intercept: 6);
		float fourCardsProbability = LinearScalef(level, 0.025f, min: 0.00f, max: 0.10f,x_intercept: 8);
		float zeroCardsProbability = 1f - oneCardsProbability - twoCardsProbability - threeCardsProbability - fourCardsProbability;

		float uncommonProbability = LinearScalef(level, 0.025f, min: 0, max: .25f);
		float rareProbability = LinearScalef(level, 0.025f, min: 0, max: .25f, x_intercept: 2);
		float commonProbability = 1f - uncommonProbability - rareProbability;

		// probability of card cost per turn Idx
		var oneCostProbabilities = new[] {   0.05f, 0.50f, 0.50f, 0.50f, 0.45f, 0.40f, 0.30f, 0.20f, 0.10f, 0.00f };
		var twoCostProbabilities = new[] {   0.00f, 0.05f, 0.25f, 0.45f, 0.50f, 0.50f, 0.50f, 0.50f, 0.50f, 0.50f };
		var threeCostProbabilities = new[] { 0.00f, 0.00f, 0.00f, 0.05f, 0.05f, 0.10f, 0.20f, 0.30f, 0.40f, 0.50f };

		GD.Print($"Generating Enemy AI for level {level}: total={totalCards}; concurrent probabilities=[{zeroCardsProbability:0.00}, {oneCardsProbability:0.00}, {twoCardsProbability:0.00}, {threeCardsProbability:0.00}, {fourCardsProbability:0.00}]; rarity probabilities=[{commonProbability:0.00},{uncommonProbability:0.00},{rareProbability:0.00}]");

		int turnId = 0;
		var moves = new List<ScriptedMove>();
		while (moves.Count < totalCards)
		{
			int concurrentCount = PickOption(
				new[] { zeroCardsProbability, oneCardsProbability, twoCardsProbability, threeCardsProbability, fourCardsProbability },
				new[] { 0, 1, 2, 3, 4 },
				rnd
			);

			int costProbabilityIdx = Math.Clamp(turnId, 0, 9);
			float oneCostProbability = oneCostProbabilities[costProbabilityIdx];
			float twoCostProbability = twoCostProbabilities[costProbabilityIdx];
			float threeCostProbability = threeCostProbabilities[costProbabilityIdx];
			float zeroCostProbability = 1f - oneCostProbability - twoCostProbability - threeCostProbability;

			GD.Print($"   Turn {turnId}: {concurrentCount} cards; cost probabilities=[{zeroCostProbability:0.00},{oneCostProbability:0.00},{twoCostProbability:0.00},{threeCostProbability:0.00}]");

			for (int i = 0; i < concurrentCount; i++)
			{
				CardBloodCost cost = PickOption(
					new[] { zeroCostProbability, oneCostProbability, twoCostProbability, threeCostProbability },
					new[] { CardBloodCost.Zero, CardBloodCost.One, CardBloodCost.Two, CardBloodCost.Three },
					rnd
				);

				CardRarity rarity = PickOption(
					new[] { commonProbability, uncommonProbability, rareProbability },
					new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare },
					rnd
				);

				GD.Print($"      {cost}:{rarity}");
				moves.Add(new ScriptedMove(turnId, cost, rarity));
			}

			turnId++;
		}

		return new EnemyAI(cardPool, moves);
	}

	public static float LinearScalef(
		float x,
		float rate,
		float min,
		float max,
		float x_intercept = 0,
		float y_intercept = 0,
		float random_amount = 0,
		RandomNumberGenerator rnd = null)
	{
		float y = (x - x_intercept) * rate + y_intercept;
		if (rnd != null)
		{
			y += rnd.RandfRange(-random_amount, random_amount);
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
		RandomNumberGenerator rnd = null)
	{
		float y = LinearScalef(x, rate, min, max, x_intercept, y_intercept, random_amount, rnd);
		return Math.Clamp(Mathf.RoundToInt(y + 1e-6f), min, max);
	}

	public static T PickOption<T>(float[] probabilities, T[] values, RandomNumberGenerator rnd)
	{
		float value = rnd.Randf();
		float sum = 0f;
		for (int i = 0; i < probabilities.Length; i++)
		{
			sum += probabilities[i];
			if (value < sum)
			{
				return values[i];
			}
		}
		GD.PushError($"Failed to PickOption - probabilities didn't cover 100%! {value:0.000}; {string.Join(",", probabilities)}");
		return values[0];
	}
}

public class LobbyStateMachineException : Exception
{
	public LobbyStateMachineException(string action, LobbyState currentState) :
		base($"Invalid State Transition [{currentState}] Cannot process action \"{action}\"")
	{ }
}

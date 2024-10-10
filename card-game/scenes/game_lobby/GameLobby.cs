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

		var moves = new ScriptedMove[] {
			new ScriptedMove(0, CardBloodCost.Zero, CardRarity.Common),
			new ScriptedMove(1, CardBloodCost.One, CardRarity.Common),
			new ScriptedMove(3, CardBloodCost.Two),
			new ScriptedMove(4, CardBloodCost.Two),
			new ScriptedMove(4, CardBloodCost.Two),
			new ScriptedMove(5, CardBloodCost.Three, CardRarity.Rare),
		};
		var opponent = new EnemyAI(cardPool, moves);

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
		var candidateCards = SelectDraftPool(progress.CardPool, level: progress.Level, count: 3);
		
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

	private IEnumerable<CardInfo> SelectDraftPool(CardPool cardPool, int level, int count)
	{
		const float UNCOMMON_RATE = 0.04f;
		const float RARE_RATE = 0.02f;

		int x = Math.Clamp(level - 1, 0, 10);
		float uncommonThreshold = x * UNCOMMON_RATE;
		float rareThreshold = x * RARE_RATE;

		float r = Random.Shared.NextSingle();
		CardRarity rarity;
		if (r < rareThreshold)
		{
			rarity = CardRarity.Rare;
		}
		else if (r < rareThreshold + uncommonThreshold)
		{
			rarity = CardRarity.Uncommon;
		}
		else
		{
			rarity = CardRarity.Common;
		}

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
			int rndIdx = Random.Shared.Next(cardOptions.Count);
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

	private List<CardInfo> GenerateStartingDeck(CardPool cardPool, int startingDeckSize)
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
}

public class LobbyStateMachineException : Exception
{
	public LobbyStateMachineException(string action, LobbyState currentState) :
		base($"Invalid State Transition [{currentState}] Cannot process action \"{action}\"")
	{ }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public enum LobbyState
{
	Initializing,
	GenerateCardPool,
	DraftCards,
	SelectLevel,
	PlayGame,
}

public partial class GameLobby : Control
{
	public LobbyState CurrentState { get; private set; } = LobbyState.Initializing;

	[Export]
	public CanvasItem GeneratingCardsPanel { get; set; }

	[Export]
	public CanvasItem DraftCardsContainer { get; set; }

	[Export]
	public CanvasItem PlayLevelPanel { get; set; }

	public override void _Ready()
	{
		if (GameProgressManager.Instance.State == null)
		{
			TransitionToState(LobbyState.GenerateCardPool);
		}
		else
		{
			TransitionToState(LobbyState.SelectLevel);
		}
	}

	public void DraftCard(CardInfo cardInfo)
	{
		const int STARTING_DECK_SIZE = 5;
		switch (CurrentState)
		{
			case LobbyState.DraftCards:
				var deck = GameProgressManager.Instance.State.DeckCards;
				deck.Add(cardInfo);

				if (GameProgressManager.Instance.State.Level == 0 &&
					deck.Count < STARTING_DECK_SIZE)
				{
					TransitionToState(LobbyState.DraftCards);
				}
				else
				{
					GameProgressManager.Instance.UpdateProgress(LobbyState.SelectLevel, updatedDeck: deck);
					TransitionToState(LobbyState.SelectLevel);
				}
				break;

			default:
				throw new LobbyStateMachineException(nameof(SkipDraft), CurrentState);
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

	public void StartGame()
	{
		CardPool cardPool = GameProgressManager.Instance.State.CardPool;
		List<CardInfo> deck = GameProgressManager.Instance.State.DeckCards;

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

			case LobbyState.DraftCards:
				await this.StartCoroutine(FadeOutDraftCardsCoroutine(fadeOutSpeed: 0.05f));
				break;
		} 

		switch (nextState)
		{
			case LobbyState.GenerateCardPool:
				await this.StartCoroutine(GenerateCardPoolCoroutine(TimeSpan.FromSeconds(2.5)));
				break;

			case LobbyState.DraftCards:
				await this.StartCoroutine(DraftCardsCoroutine(fadeInSpeed: 0.05f));
				break;

			case LobbyState.SelectLevel:
				Label levelLabel = PlayLevelPanel.FindChild("LevelNumber") as Label;
				levelLabel.Text = (GameProgressManager.Instance.State.Level + 1).ToString();
				PlayLevelPanel.Visible = true;
				break;
		}
	}

	private IEnumerable<CardInfo> SelectDraftPool(CardPool cardPool, int level, int count)
	{
		const float UNCOMMON_RATE = 0.04f;
		const float RARE_RATE = 0.02f;

		int x = Math.Clamp(level, 0, 10);
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
			draftPool.Add(cardOptions[rndIdx]);
			cardOptions.RemoveAt(rndIdx);
		}

		return draftPool;
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
		GameProgressManager.Instance.StartNewGame(cardPool);

		TimeSpan delaySoFar = DateTime.Now - start;
		yield return new CoroutineDelay((delay - delaySoFar).TotalSeconds);

		TransitionToState(LobbyState.DraftCards);
	}

	private IEnumerable SpinProgressBarCoroutine(TextureProgressBar spinner, float speed)
	{
		while (GeneratingCardsPanel.Visible)
		{
			spinner.RadialInitialAngle += speed;
			yield return null;
		}
	}

	private IEnumerable DraftCardsCoroutine(float fadeInSpeed)
	{
		Node cardsContainer = DraftCardsContainer.FindChild("CardButtonContainer");
		Button skipButton = DraftCardsContainer.FindChild("SkipButton") as Button;

		foreach (Node child in cardsContainer.GetChildren())
		{
			child.QueueFree();
		}

		var progress = GameProgressManager.Instance.State;
		var candidateCards = SelectDraftPool(progress.CardPool, level: progress.Level, count: 3);
		
		foreach (CardInfo cardInfo in candidateCards)
		{
			var card = Constants.CardButtonScene.Instantiate<CardButton>();
			card.ShowCardBack = false;
			card.SetCard(cardInfo);
			card.Pressed += () => DraftCard(cardInfo);
			cardsContainer.AddChild(card);
		}

		skipButton.Visible = progress.Level > 0;

		DraftCardsContainer.Modulate = new Color(1, 1, 1, 0);
		DraftCardsContainer.Visible = true;

		for (float a = 0; a < 1; a += fadeInSpeed)
		{
			DraftCardsContainer.Modulate = new Color(1, 1, 1, a);
			yield return null;
		}
		DraftCardsContainer.Modulate = new Color(1, 1, 1, 1);
	}

	private IEnumerable FadeOutDraftCardsCoroutine(float fadeOutSpeed)
	{
		DraftCardsContainer.Modulate = new Color(1, 1, 1, 1);
		DraftCardsContainer.Visible = true;

		for (float a = 1; a > 0; a -= fadeOutSpeed)
		{
			DraftCardsContainer.Modulate = new Color(1, 1, 1, a);
			yield return null;
		}
		DraftCardsContainer.Modulate = new Color(1, 1, 1, 0);
		DraftCardsContainer.Visible = false;
	}
}

public class LobbyStateMachineException : Exception
{
	public LobbyStateMachineException(string action, LobbyState currentState) :
		base($"Invalid State Transition [{currentState}] Cannot process action \"{action}\"")
	{ }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GameLobby : Control
{
	public GameProgress GameProgress { get; set; }

	[Export]
	public CanvasItem GeneratingCardsPanel { get; set; }

	public override void _Ready()
	{
		if (GameProgress == null)
		{
			this.StartCoroutine(GenerateCardsCoroutine(TimeSpan.FromSeconds(3)));
		}
	}

	public void StartGame()
	{
		CardPool cardPool = GameProgress.CardPool;
		List<CardInfo> deck = GameProgress.DeckCards;

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

	private IEnumerable GenerateCardsCoroutine(TimeSpan delay)
	{
		yield return new CoroutineDelay(0.5);

		GeneratingCardsPanel.Visible = true;
		TextureProgressBar spinner = GeneratingCardsPanel.FindChild("SpinningProgressBar") as TextureProgressBar;
		this.StartCoroutine(SpinProgressBarCoroutine(spinner, 5f));

		DateTime start = DateTime.Now;
		string cardPoolName = $"cards-{start:yyyyMMdd-HHmmss}";

		var cardPool = CardGenerator.GenerateRandomCardPool(CardGenerator.DefaultArgs, cardPoolName);
		GameLoader.SaveCardPool(cardPool, cardPoolName);
		GD.Print("Generated new card pool at ", cardPoolName);

		GameProgress = new GameProgress
		{
			Level = 0,
			Score = 0,
			CardPool = cardPool,
			DeckCards = cardPool.Cards, // new List<CardInfo>(),
		};

		GameLoader.SaveGame(GameProgress);

		TimeSpan delaySoFar = DateTime.Now - start;
		yield return new CoroutineDelay((delay - delaySoFar).TotalSeconds);

		GeneratingCardsPanel.Visible = false;

		// TODO: draft and lobby events
		StartGame();
	}

	private IEnumerable SpinProgressBarCoroutine(TextureProgressBar spinner, float speed)
	{
		while (GeneratingCardsPanel.Visible)
		{
			spinner.RadialInitialAngle += speed;
			yield return null;
		}
	}
}

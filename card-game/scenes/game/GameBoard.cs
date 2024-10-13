using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class GameBoard : Node2D
{
	private static readonly int PLAYER_INDEX = 0;
	private static readonly int ENEMY_INDEX = 1;
	private static readonly int ENEMY_STAGE_INDEX = 2;
	private PlayArea[][] AllLanes => new[] { Lane0, Lane1, Lane2, Lane3 };
	private PlayArea[] PlayerLanes => AllLanes.Select(lane => lane[PLAYER_INDEX]).ToArray();
	private bool[] StagedLaneHasCard => AllLanes.Select(lane => lane[ENEMY_STAGE_INDEX].CardCount > 0).ToArray();

	private Dictionary<Card, SacrificeCardCallbacks> _sacrificeCallbacks = new Dictionary<Card, SacrificeCardCallbacks>();

	[Export]
	public PlayArea[] Lane0 { get; set; }

	[Export]
	public PlayArea[] Lane1 { get; set; }

	[Export]
	public PlayArea[] Lane2 { get; set; }

	[Export]
	public PlayArea[] Lane3 { get; set; }

	[Export]
	public BackgroundRenderer Background { get; set; }

	[Export]
	public CanvasItem PayCostPanel { get; set; }

	[Export]
	public CanvasItem[] PayBloodCostIcons { get; set; }

	[Export]
	public CanvasItem GameOverPanel { get; set; }

	public int PlayerCardCount => PlayerLanes.Sum(lane => lane.CardCount > 0 ? 1 : 0);

	public override void _Input(InputEvent inputEvent)
	{
		bool clickedWithNoCardDrop = inputEvent.IsActionPressed(Constants.ClickEventName) &&
			ActiveCardState.Instance.SelectedCard != null && ActiveCardState.Instance.ActiveCardDrop == null;

		bool cancelClicked = inputEvent.IsActionPressed(Constants.RightClickEventName) || inputEvent.IsActionPressed(Constants.EscEventName);

		if (clickedWithNoCardDrop || cancelClicked)
		{
			// Clicks that don't have a card drop should clear the selected card.
			// Is there a better place for this responsibility to live?
			ActiveCardState.Instance.SelectCard(null);

			if (MainGame.Instance.CurrentState == GameState.PlayCard_PayPrice)
			{
				// We pretend the card cost was paid, even though it wasn't.
				// This works out because DisablePayThePrice cleans up the ActiveCardState for us.
				MainGame.Instance.CardCostPaid();
			}
		}
	}

	public void OnGameStateTransition(GameState nextState, GameState lastState)
	{
		switch (lastState)
		{
			case GameState.PlayCard:
				DisableLanes();
				break;

			case GameState.PlayCard_PayPrice:
				DisablePayThePrice();
				break;
		}

		Task coroutine;
		switch (nextState)
		{
			case GameState.PlayCard:
				EnableLanes();
				break;

			case GameState.PlayCard_PayPrice:
				Card stagedCard = ActiveCardState.Instance.StagedCard;
				if (stagedCard == null)
				{
					GD.PushError($"[UnexpectedState] Transitioned to PayPrice without a StagedCard.");
					break;
				}
				PopUpPayThePricePanel(stagedCard.Info.BloodCost);
				break;

			case GameState.PlayerCombat:
				coroutine = this.StartCoroutine(PlayerCombatCoroutine());
				break;

			case GameState.EnemyPlayCard:
				coroutine = this.StartCoroutine(OpponentPlayCardsCoroutine());
				break;

			case GameState.EnemyCombat:
				coroutine = this.StartCoroutine(OpponentCombatCoroutine());
				break;

			case GameState.EnemyStageCard:
				int turn = MainGame.Instance.CurrentTurn;
				List<PlayedCard> moves = MainGame.Instance.GameLevel.AI.GetMovesForTurn(turn, StagedLaneHasCard);
				coroutine = this.StartCoroutine(OpponentStageCardsCoroutine(moves));
				break;

			case GameState.GameOver:
				bool playerWon = MainGame.Instance.HealthBar.PlayerPoints > 0;
				PopUpGameOverPanel(playerWon);
				break;

			default:
				DisableLanes();
				break;
		}
	}

	public bool CanPlayCardAtLocation(Card card, CardDrop cardDrop)
	{
		if (card == null || cardDrop == null) return false;

		bool canAfford = PlayerCardCount >= (int)card.Info.BloodCost;
		bool isEmptyPlayArea = cardDrop is PlayArea && cardDrop.CardCount == 0;
		bool isSacrificablePlayArea = cardDrop is PlayArea && (int)card.Info.BloodCost > 0;

		return canAfford && (isEmptyPlayArea || isSacrificablePlayArea);
	}

	private void PopUpPayThePricePanel(CardBloodCost cost)
	{
		PayCostPanel.Modulate = new Color(1, 1, 1, 0);
		PayCostPanel.Visible = true;
		this.StartCoroutine(PayCostPanel.FadeTo(1f, 0.1f));

		AudioManager.Instance.Play(Constants.Audio.Heartbeat, name: "Heartbeat");

		for (int i = 0; i < PayBloodCostIcons.Length; i++)
		{
			PayBloodCostIcons[i].Visible = (i < (int)cost);
			PayBloodCostIcons[i].Modulate = new Color(1, 1, 1, 0.5f);
		}

		foreach (PlayArea lane in PlayerLanes)
		{
			Card laneCard = lane.GetChildCards().FirstOrDefault();
			if (laneCard != null && laneCard != ActiveCardState.Instance.StagedCard)
			{
				var callbacks = new SacrificeCardCallbacks(laneCard);
				_sacrificeCallbacks[laneCard] = callbacks;
				callbacks.AddCallbacks();
			}
		}
	}

	public void UpdatePayThePrice(int cardsPaid)
	{
		for (int i = 0; i < PayBloodCostIcons.Length; i++)
		{
			PayBloodCostIcons[i].Modulate = (i < cardsPaid) ? new Color(1, 1, 1, 1.0f) : new Color(1, 1, 1, 0.5f);
		}
	}

	private void DisablePayThePrice()
	{
		PayCostPanel.Visible = false;
		AudioManager.Instance.Stop("Heartbeat");

		ActiveCardState.Instance.CancelStagedCard();

		foreach (PlayArea lane in PlayerLanes)
		{
			Card laneCard = lane.GetChildCards().FirstOrDefault();
			if (laneCard != null && _sacrificeCallbacks.TryGetValue(laneCard, out SacrificeCardCallbacks callbacks))
			{
				callbacks.RemoveCallbacks();
				ActiveCardState.Instance.RemoveSacrificeCard(callbacks.Card);
			}
		}

		_sacrificeCallbacks.Clear();
	}

	private void PopUpGameOverPanel(bool playerWon)
	{
		GameOverPanel.Modulate = new Color(1, 1, 1, 0);
		GameOverPanel.Visible = true;

		Label title = GameOverPanel.FindChild("Title") as Label;
		Label subtitle = GameOverPanel.FindChild("Subtitle") as Label;
		Button continueButton = GameOverPanel.FindChild("ContinueButton") as Button;

		if (playerWon)
		{
			title.Text = "You Win!";
			subtitle.Text = "";
			continueButton.Visible = true;
			title.AddThemeColorOverride("font_color", Colors.ForestGreen);

			AudioManager.Instance.Play(Constants.Audio.GameOver_Win);
		}
		else
		{
			title.Text = "You Lose";
			subtitle.Text = "Try again?";
			continueButton.Visible = false;
			title.AddThemeColorOverride("font_color", Colors.DarkRed);

			AudioManager.Instance.Play(Constants.Audio.GameOver_Lose);
		}

		this.StartCoroutine(GameOverPanel.FadeTo(1f, 0.05f));
	}

	private void DisableLanes()
	{
		foreach (var lane in PlayerLanes)
		{
			lane.SupportsDrop = false;
		}
	}

	private void EnableLanes()
	{
		foreach (var lane in PlayerLanes)
		{
			lane.SupportsDrop = true;
		}
	}

	private static int EnemyCardCount = 0;
	private Card InstantiateCardInLane(CardInfo cardInfo, int laneIndex)
	{
		PlayArea lane = AllLanes[laneIndex][ENEMY_STAGE_INDEX];
		if (lane.CardCount > 0)
		{
			GD.PushError($"Cannot play enemy card in lane {laneIndex}.");
			return null;
		}

		var card = Constants.CardScene.Instantiate<Card>();
		string nodeName = cardInfo.Name.Replace(" ", "_");
		card.Name = $"e_{nodeName}_{EnemyCardCount++}";
		card.GlobalPosition = lane.GlobalPosition + new Vector2(0, -150);

		card.Info = cardInfo;
		ActiveCardState.Instance.SetCardDrop(card, lane);

		return card;
	}

	private IEnumerable OpponentStageCardsCoroutine(IEnumerable<PlayedCard> moves)
	{
		foreach (PlayedCard move in moves)
		{
			GD.Print($"Playing card {move.Card.Name} in lane {move.Lane}");
			yield return new CoroutineDelay(0.5f);
			InstantiateCardInLane(move.Card, move.Lane);
		}

		MainGame.Instance.OpponentDoneStagingCards();
	}

	private IEnumerable OpponentPlayCardsCoroutine()
	{
		for (int laneIdx = 0; laneIdx < AllLanes.Length; laneIdx++)
		{
			PlayArea[] lane = AllLanes[laneIdx];
			if (lane[ENEMY_STAGE_INDEX].CardCount > 0 &&
				lane[ENEMY_INDEX].CardCount == 0)
			{
				yield return new CoroutineDelay(0.5f);

				Card stagedCard = lane[ENEMY_STAGE_INDEX].GetChildCards().First();
				ActiveCardState.Instance.SetCardDrop(stagedCard, lane[ENEMY_INDEX]);
			}
		}

		MainGame.Instance.OpponentDonePlayingCards();
	}

	private IEnumerable OpponentCombatCoroutine()
	{
		yield return new CoroutineDelay(1.0);

		foreach (var lane in AllLanes)
		{
			Card playerCard = lane[PLAYER_INDEX].GetChildCards().FirstOrDefault();
			Card enemyCard = lane[ENEMY_INDEX].GetChildCards().FirstOrDefault();

			if (enemyCard != null)
			{
				int damage = enemyCard.Info.Attack;
				if (damage == 0) continue;

				enemyCard.ZIndex = 10;
				Vector2 startPosition = enemyCard.GlobalPosition;
				if (playerCard != null)
				{
					yield return enemyCard.LerpGlobalPositionCoroutine(playerCard.GlobalPosition + new Vector2(0, -50), 0.08f);
					GD.Print($"Dealt {damage} damage to {playerCard.Info.Name}!");

					AudioStream damageAudio = damage <= 2 ?
						Constants.Audio.DamageCard_Low :
						Constants.Audio.DamageCard_High;
					AudioManager.Instance.Play(damageAudio, tweak: true);

					playerCard.DealDamage(damage);
				}
				else
				{
					yield return enemyCard.LerpGlobalPositionCoroutine(lane[PLAYER_INDEX].GlobalPosition, 0.08f);
					GD.Print($"Dealt {damage} damage to the player!");

					AudioStream damageAudio = damage <= 2 ?
						Constants.Audio.DamagePlayer_Low :
						Constants.Audio.DamagePlayer_High;
					AudioManager.Instance.Play(damageAudio, tweak: true);

					yield return MainGame.Instance.HealthBar.PlayerTakeDamage(damage);
				}

				yield return enemyCard.LerpGlobalPositionCoroutine(startPosition, 0.1f);
				yield return new CoroutineDelay(0.234);
				enemyCard.ZIndex = 0;
			}
		}

		MainGame.Instance.EndOpponentCombat();
	}

	private IEnumerable PlayerCombatCoroutine()
	{
		yield return new CoroutineDelay(1.0);

		foreach (var lane in AllLanes)
		{
			Card playerCard = lane[PLAYER_INDEX].GetChildCards().FirstOrDefault();
			Card enemyCard = lane[ENEMY_INDEX].GetChildCards().FirstOrDefault();
			Card enemyBackCard = lane[ENEMY_STAGE_INDEX].GetChildCards().FirstOrDefault();

			if (playerCard != null)
			{
				int damage = playerCard.Info.Attack;
				if (damage == 0) continue;

				playerCard.ZIndex = 10;
				Vector2 startPosition = playerCard.GlobalPosition;
				if (enemyCard != null)
				{
					yield return playerCard.LerpGlobalPositionCoroutine(enemyCard.GlobalPosition + new Vector2(0, 50), 0.08f);
					GD.Print($"Dealt {damage} damage to {enemyCard.Info.Name}!");

					AudioStream damageAudio = damage <= 2 ?
						Constants.Audio.DamageCard_Low :
						Constants.Audio.DamageCard_High;
					AudioManager.Instance.Play(damageAudio, tweak: true);

					enemyCard.DealDamage(damage);
				}
				else
				{
					yield return playerCard.LerpGlobalPositionCoroutine(lane[ENEMY_STAGE_INDEX].GlobalPosition + new Vector2(0, 50), 0.08f);
					GD.Print($"Dealt {damage} damage to the enemy!");

					AudioStream damageAudio = damage <= 2 ?
						Constants.Audio.DamagePlayer_Low :
						Constants.Audio.DamagePlayer_High;
					AudioManager.Instance.Play(damageAudio, tweak: true);

					yield return MainGame.Instance.HealthBar.OpponentTakeDamage(damage);
				}

				yield return playerCard.LerpGlobalPositionCoroutine(startPosition, 0.1f);
				yield return new CoroutineDelay(0.234);
				playerCard.ZIndex = 0;
			}
		}

		MainGame.Instance.EndPlayerCombat();
	}

	private class SacrificeCardCallbacks
	{
		private bool _isSelected = false;
		public Card Card;

		public SacrificeCardCallbacks(Card card)
		{
			Card = card;
		}

		public void AddCallbacks()
		{
			Card.Area.AreaClicked += Select;
			Card.Area.AreaMouseOver += StartHovering;
			Card.Area.AreaMouseOut += StopHovering;

			Card.StartShaking();
		}

		public void RemoveCallbacks()
		{
			if (Card.IsQueuedForDeletion())
			{
				return;
			}

			Card.Area.AreaClicked -= Select;
			Card.Area.AreaMouseOver -= StartHovering;
			Card.Area.AreaMouseOut -= StopHovering;

			Card.StopShaking();
			StopHovering();
		}

		public void Select()
		{
			int selectedSacrifices = ActiveCardState.Instance.ProposedSacrifices.Count;
			int cost = (int)ActiveCardState.Instance.StagedCard.Info.BloodCost;

			bool isInSameLaneAsPlayingCard = ActiveCardState.Instance.StagedCard.HomeCardDrop == Card.HomeCardDrop;

			if (_isSelected && !isInSameLaneAsPlayingCard)
			{
				_isSelected = false;
				Card.Modulate = Colors.Red;
				ActiveCardState.Instance.RemoveSacrificeCard(Card);
			}
			else if (selectedSacrifices < cost)
			{
				_isSelected = true;
				Card.Modulate = Colors.White;
				ActiveCardState.Instance.AddSacrificeCard(Card);

				AudioManager.Instance.Play(Constants.Audio.ProposeSacrificeClick, tweak: true);
			}
		}

		public void StartHovering()
		{
			if (!_isSelected)
			{
				Card.Modulate = Colors.Red;
			}
		}

		public void StopHovering()
		{
			Card.Modulate = Colors.White;
		}
	}
}

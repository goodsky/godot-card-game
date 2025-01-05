
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class EnemyAI
{
    private CardPool _cardPool;
    private List<ScriptedMove> _moves;
    private RandomGenerator _rnd;

    public int MaxTurn { get; private set; }

    public RandomGenerator SnapshotRandomGenerator => new RandomGenerator(_rnd.Seed, _rnd.N); 

    public EnemyAI(CardPool cards, List<ScriptedMove> moves, RandomGenerator rnd)
    {
        _cardPool = cards;
        _moves = moves;
        _rnd = new RandomGenerator(rnd.Seed, rnd.N); // copy so other game state doesn't affect this

        Initialize();
    }

    public void Initialize()
    {
        for (int i = 0; i < _moves.Count; i++)
        {
            _moves[i].Resolved = false;
            MaxTurn = Math.Max(MaxTurn, _moves[i].Turn);
        }
    }

    public List<PlayedCard> GetMovesForTurn(int turn, bool[] backLaneHasCard)
    {
        List<ScriptedMove> thisTurnMoves = _moves.Where((move) => move.Turn <= turn && !move.Resolved).ToList();
        List<PlayedCard> playedCards = new List<PlayedCard>();

        foreach (ScriptedMove move in thisTurnMoves)
        {
            if (backLaneHasCard.All(hasCard => hasCard))
            {
                break;
            }

            int lane = -1;
            if (move.Lane != null)
            {
                lane = move.Lane.Value;
                if (lane < 0 || lane >= backLaneHasCard.Length)
                {
                    GD.PushError($"Scripted Move has an invalid lane value {lane}.");
                    move.Resolved = true;
                    continue;
                }
                else if (backLaneHasCard[move.Lane.Value])
                {
                    continue;
                }
            }
            else
            {
                int openLanes = backLaneHasCard.Count((hasCard) => !hasCard);
                int laneIdx = _rnd.Next(openLanes);
                for (lane = 0; lane < backLaneHasCard.Length; lane++)
                {
                    if (backLaneHasCard[lane]) continue;
                    if (laneIdx > 0) laneIdx--;
                    else break;
                }

                if (lane >= backLaneHasCard.Length)
                {
                    GD.PushError($"Unexpected lane for resolved turn! openLanes={openLanes}; lanes=[{string.Join(",", backLaneHasCard)}]");
                    move.Resolved = true;
                    continue;
                }

                backLaneHasCard[lane] = true;
            }

            CardInfo? cardInfo = null;
            if (move.CardToPlay != null)
            {
                cardInfo = move.CardToPlay;
            }
            else if (move.CardCostToPlay != null)
            {
                cardInfo = GetCardByCostAndRarity(move.CardCostToPlay.Value, move.CardRarityToPlay);
            }

            if (cardInfo == null)
            {
                GD.PushError($"Could not find a card for move! Cost = {move.CardToPlay}; Rarity = {move.CardRarityToPlay};");
                move.Resolved = true;
                continue;
            }

            move.Resolved = true;
            playedCards.Add(new PlayedCard { Card = cardInfo.Value, Lane = lane });
        }

        return playedCards;
    }

    private CardInfo? GetCardByCostAndRarity(CardBloodCost cost, CardRarity? rarity)
    {
        var possibleCards = _cardPool.Cards.Where(card => card.BloodCost == cost);
        if (rarity != null)
        {
            possibleCards = possibleCards.Where(card => card.Rarity == rarity.Value);
        }

        if (!possibleCards.Any())
        {
            return null;
        }

        return _rnd.SelectRandom(possibleCards);
    }
}

public class ScriptedMove
{
    public bool Resolved { get; set; } = false;
    public int Turn { get; set; }
    public int? Lane { get; set; }
    public CardInfo? CardToPlay { get; set; }
    public CardBloodCost? CardCostToPlay { get; set; }
    public CardRarity? CardRarityToPlay { get; set; }

    public ScriptedMove(int turn, CardInfo cardInfo, int? lane = null)
    {
        Turn = turn;
        CardToPlay = cardInfo;
        CardCostToPlay = null;
        CardRarityToPlay = null;
        Lane = lane;
    }

    public ScriptedMove(int turn, CardBloodCost cost, CardRarity? rarity = null, int? lane = null)
    {
        Turn = turn;
        CardToPlay = null;
        CardCostToPlay = cost;
        CardRarityToPlay = rarity;
        Lane = lane;
    }
}

public struct PlayedCard
{
    public int Lane { get; set; }
    public CardInfo Card { get; set; }
}

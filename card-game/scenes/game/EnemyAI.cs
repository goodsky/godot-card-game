
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class EnemyAI
{
    private CardPool _cardPool;
    private ScriptedMove[] _moves;

    public EnemyAI(CardPool cards, ScriptedMove[] moves)
    {
        _cardPool = cards;
        _moves = moves;

        for (int i = 0; i < moves.Length; i++)
        {
            _moves[i].Resolved = false;
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
                int laneIdx = Random.Shared.Next(openLanes);
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
            if (move.CardIdToPlay != null)
            {
                cardInfo = GetCardById(move.CardIdToPlay.Value);
            }

            if (cardInfo == null && move.CardCostToPlay != null)
            {
                cardInfo = GetCardByCostAndRarity(move.CardCostToPlay.Value, move.CardRarityToPlay);
            }

            if (cardInfo == null)
            {
                GD.PushError("Could not find a card for move!");
                move.Resolved = true;
                continue;
            }

            move.Resolved = true;
            playedCards.Add(new PlayedCard { Card = cardInfo.Value, Lane = lane });
        }

        return playedCards;
    }

    private CardInfo? GetCardById(int id)
    {
        var cardsWithId = _cardPool.Cards.Where(card => card.Id == id);
        if (!cardsWithId.Any())
        {
            GD.PushError($"Enemy AI is trying to use undefined card id {id}");
            return null;
        }

        return cardsWithId.First();
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
            GD.PushError($"Enemy AI is trying to use a cost and rarity that doesn't exist. Cost = {cost}; Rarity = {rarity};");
            return null;
        }

        CardInfo[] cards = possibleCards.ToArray();
        return cards[Random.Shared.Next(cards.Length)];
    }
}

public class ScriptedMove
{
    public bool Resolved { get; set; } = false;
    public int Turn { get; set; }
    public int? Lane { get; set; }
    public int? CardIdToPlay { get; set; }
    public CardBloodCost? CardCostToPlay { get; set; }
    public CardRarity? CardRarityToPlay { get; set; }

    public ScriptedMove(int turn, int cardId, int? lane = null)
    {
        Turn = turn;
        CardIdToPlay = cardId;
        CardCostToPlay = null;
        CardRarityToPlay = null;
        Lane = lane;
    }

    public ScriptedMove(int turn, CardBloodCost cost, CardRarity? rarity = null, int? lane = null)
    {
        Turn = turn;
        CardIdToPlay = null;
        CardCostToPlay = cost;
        CardRarityToPlay = rarity;
        Lane = lane;
    }
}

public struct PlayedCard
{
    public int? Lane { get; set; }
    public CardInfo Card { get; set; }
}

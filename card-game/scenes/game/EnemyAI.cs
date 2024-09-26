
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
        List<ScriptedMove> scriptedMoves = _moves.Where((move) => move.Turn <= turn && !move.Resolved).ToList();
        List<PlayedCard> playedCards = new List<PlayedCard>();
        
        for (int i = 0; i < scriptedMoves.Count; i++)
        {
            var scriptedMove = scriptedMoves[i];
            if (backLaneHasCard.All(hasCard => hasCard))
            {
                break;
            }

            int lane = -1;
            if (scriptedMove.Lane != null)
            {
                lane = scriptedMove.Lane.Value;
                if (lane < 0 || lane >= backLaneHasCard.Length)
                {
                    GD.PushError($"Scripted Move has an invalid lane value {lane}.");
                    scriptedMove.Resolved = true;
                    continue;
                }
                else if (backLaneHasCard[scriptedMove.Lane.Value])
                {
                    GD.Print($"Lane {scriptedMove} is occupied! Skipping");
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
                    break;
                }

                if (lane >= backLaneHasCard.Length)
                {
                    GD.PushError($"Unexpected lane for resolved turn! openLanes={openLanes}; lanes=[{string.Join(",", backLaneHasCard)}]");
                    scriptedMove.Resolved = true;
                    continue;
                }
            }

            CardInfo? cardInfo = null;
            if (scriptedMove.CardIdToPlay != null)
            {
                cardInfo = GetCardById(scriptedMove.CardIdToPlay.Value);
            }
            
            if (cardInfo == null && scriptedMove.CardRarityToPlay != null)
            {
                cardInfo = GetCardByRarity(scriptedMove.CardRarityToPlay.Value);
            }

            if (cardInfo == null)
            {
                GD.PushError("Could not find a card for move!");
                scriptedMove.Resolved = true;
                continue;
            }

            scriptedMove.Resolved = true;
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

    private CardInfo? GetCardByRarity(CardRarity rarity)
    {
        var cardsWithRarity = _cardPool.Cards.Where(card => card.Rarity == rarity);
        if (!cardsWithRarity.Any())
        {
            GD.PushError($"Enemy AI is trying to use a rarity that doesn't exist {rarity}");
            return null;
        }

        CardInfo[] cards = cardsWithRarity.ToArray();
        return cards[Random.Shared.Next(cards.Length)];
    }
}

public struct ScriptedMove
{
    public bool Resolved { get; set; }
    public int Turn { get; set; }
    public int? Lane { get; set; }
    public int? CardIdToPlay { get; set; }
    public CardRarity? CardRarityToPlay { get; set; }
}

public struct PlayedCard
{
    public int? Lane { get; set; }
    public CardInfo Card { get; set; }
}

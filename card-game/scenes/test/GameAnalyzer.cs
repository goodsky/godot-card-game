using System;
using System.Collections.Generic;
using System.Linq;

public static class GameAnalyzer
{
    public static void AnalyzeGameBalance(int cardPoolCount = 3, int gamesCount = 10, int minLevel = 1, int maxLevel = 12)
    {
        for (int i = 0; i < cardPoolCount; i++)
        {
            CardPool cardPool = CardGenerator.GenerateRandomCardPool("Game Balance Pool");
            for (int level = minLevel; level <= maxLevel; level++)
            {
                SimulationSummary gameSummary = SimulateGames(cardPool, gamesCount, level);
                Console.WriteLine($"Pool {i} Level {level} Summary: {gameSummary.TotalGamesPlayed} games, {gameSummary.PlayerWinCount} player wins, {gameSummary.EnemyWinCount} enemy wins, {gameSummary.StalemateCount} stalemates, {gameSummary.MaxTurnsCount} max turns, {gameSummary.DuplicateStateCount} duplicate states");
            }
        }
    }

    private static SimulationSummary SimulateGames(CardPool cardPool, int gamesCount, int level)
    {
        var rnd = new RandomGenerator();
        var summary = new SimulationSummary();
        for (int gameId = 0; gameId < gamesCount; gameId++)
        {
            var (playerCreatures, playerSacrifices) = GeneratePlayerDeck(cardPool);
            ShuffleCards(playerCreatures, rnd);
            ShuffleCards(playerSacrifices, rnd);
            
            var args = new SimulatorArgs
            {
                EnableLogging = false,
                StartingHandSize = 2, // TODO: make this configurable
                SacrificesDeck = playerSacrifices,
                CreaturesDeck = playerCreatures,
                AI = GameLobby.GenerateEnemyAI(cardPool, level, rnd, log: false),
            };

            var result = new GameSimulator(
                maxTurns: 20,
                maxBranchPerTurn: 2,
                maxStateQueueCircuitBreakerSize: 1000,
                alwaysTryDrawingCreature: true,
                alwaysTryDrawingSacrifice: true).Simulate(args);

            if (result.DuplicateStates > 0)
            {
                Console.WriteLine($"SKYLER!!!! Game {gameId} had {result.DuplicateStates} duplicate states!");
                summary.DuplicateStateCount += result.DuplicateStates;
            }

            summary.TotalGamesPlayed += result.Rounds.Count;
            summary.PlayerWinCount += result.Rounds.Count(r => r.Result == RoundResult.PlayerWin);
            summary.EnemyWinCount += result.Rounds.Count(r => r.Result == RoundResult.EnemyWin);
            summary.MaxTurnsCount += result.Rounds.Count(r => r.Result == RoundResult.MaxTurnsReached);
            summary.StalemateCount += result.Rounds.Count(r => r.Result == RoundResult.Stalemate);
        }

        return summary;
    }

    private static (List<CardInfo> playerCreatures, List<CardInfo> playerSacrifices) GeneratePlayerDeck(CardPool cardPool)
    {
        var sacrifices = cardPool.Cards.Where(c => c.Rarity == CardRarity.Sacrifice).ToList();
        var oneCostCards = cardPool.Cards.Where(c => c.BloodCost == CardBloodCost.One).ToList();
        var twoCostCards = cardPool.Cards.Where(c => c.BloodCost == CardBloodCost.Two).ToList();
        var threeCostCards = cardPool.Cards.Where(c => c.BloodCost == CardBloodCost.Three).ToList();

        // TODO: randomize this a bit more
        var playerCreatures =
            oneCostCards.Take(4).Concat(
                twoCostCards.Take(2)).Concat(
                    threeCostCards.Take(1)).ToList();

        var playerSacrifices = sacrifices.Take(6).ToList();

        return (playerCreatures, playerSacrifices);
    }

    private static void ShuffleCards(List<CardInfo> cards, RandomGenerator rnd)
    {
        var swap = (int src, int dst) =>
        {
            CardInfo temp = cards[src];
            cards[src] = cards[dst];
            cards[dst] = temp;
        };

        for (int i = cards.Count - 1; i >= 1; i--)
        {
            int j = rnd.Next(i + 1);
            swap(i, j);
        }
    }

    public class SimulationSummary
    {
        public int TotalGamesPlayed { get; set; }
        public int PlayerWinCount { get; set; }
        public int EnemyWinCount { get; set; }
        public int StalemateCount { get; set; }
        public int MaxTurnsCount { get; set; }
        public int DuplicateStateCount { get; set; }
    }
}
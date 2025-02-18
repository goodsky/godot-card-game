using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

public static class GameAnalyzer
{
    public static int STARTING_DECK_SIZE = 6;
    public static int STARTING_HAND_SIZE = 2;

    public static void AnalyzeGameBalance(int cardPoolCount = 3, int gamesCount = 10, int minLevel = 1, int maxLevel = 12)
    {
        var results = new List<(int poolId, int level, SimulatorResult result)>();
        var playerCardPerformanceSummary = new CardPerformanceSummary();
        var enemyCardPerformanceSummary = new CardPerformanceSummary();
        try
        {
            for (int i = 0; i < cardPoolCount; i++)
            {
                CardPool cardPool = CardGenerator.GenerateRandomCardPool("Game Balance Pool");
                for (int level = minLevel; level <= maxLevel; level++)
                {
                    Console.WriteLine($"Simulating pool {i} level {level}...");
                    var levelResults = SimulateGames(cardPool, gamesCount, level);
                    results.AddRange(levelResults.Select(r => (i, level, r)));

                    foreach (var result in levelResults)
                    {
                        playerCardPerformanceSummary.Merge(result.PlayerCardPerformanceSummary);
                        enemyCardPerformanceSummary.Merge(result.EnemyCardPerformanceSummary);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("UNHANDLED EXCEPTION WHILE ANALYZING GAMES!!!");
            Console.WriteLine(e.ToString());
        }
        finally
        {
            WriteResultsCsv(results, "GameAnalysis_Results.csv");
            WriteCardPerformanceCsv(playerCardPerformanceSummary, "GameAnalysis_PlayerCardPerformance.csv");
            WriteCardPerformanceCsv(enemyCardPerformanceSummary, "GameAnalysis_EnemyCardPerformance.csv");
        }
    }

    private static List<SimulatorResult> SimulateGames(CardPool cardPool, int gamesCount, int level)
    {
        var rootRnd = new RandomGenerator();
        var results = new List<SimulatorResult>();
        for (int gameId = 0; gameId < gamesCount; gameId++)
        {
            var rnd = new RandomGenerator(rootRnd.Next());
            var fakeProgress = GenerateSimulatedProgress(cardPool, level, rnd);
            (Deck sacrificeDeck, Deck creatureDeck, GameLevel gameLevel) = GameLobby.InitializeGame(fakeProgress, rnd);
            
            var args = new SimulatorArgs
            {
                EnableLogging = false,
                EnableCardSummary = true,
                StartingHandSize = fakeProgress.HandSize,
                SacrificesDeck = sacrificeDeck.Cards,
                CreaturesDeck = creatureDeck.Cards,
                AI = gameLevel.AI,
            };

            var result = new GameSimulator(
                maxTurns: 20,
                maxBranchPerTurn: 2,
                maxStateQueueCircuitBreakerSize: 1000,
                alwaysTryDrawingCreature: true,
                alwaysTryDrawingSacrifice: true).Simulate(args);

            results.Add(result);
        }

        return results;
    }

    public static GameProgress GenerateSimulatedProgress(CardPool cardPool, int level, RandomGenerator rnd)
    {
        int handSize = STARTING_HAND_SIZE;
        var playerDeck = GameLobby.GenerateStartingDeck(cardPool, STARTING_DECK_SIZE, rnd);

        for (int i = 0; i < level; i++)
        {
            var pretendDifficulty = rnd.SelectRandomOdds(
                new[] { LevelDifficulty.Easy, LevelDifficulty.Medium, LevelDifficulty.Hard },
                new[] { 50, 30, 20 }); // I invented these odds
            if (level == 1) pretendDifficulty = LevelDifficulty.Easy;
            if (level <= 3 && pretendDifficulty == LevelDifficulty.Hard) pretendDifficulty = LevelDifficulty.Medium;
            var reward = GameLobby.GenerateLevelReward(pretendDifficulty, rnd);

            switch (reward)
            {
                case LevelReward.AddResource:
                    playerDeck.AddRange(GameLobby.SelectRandomCards(cardPool.Cards, count: 1, rnd, CardRarity.Sacrifice));
                    break;
                case LevelReward.AddCreature:
                    playerDeck.AddRange(GameLobby.SelectRandomCards(cardPool.Cards, count: 1, rnd, CardRarity.Common));
                    break;
                case LevelReward.AddUncommonCreature:
                    playerDeck.AddRange(GameLobby.SelectRandomCards(cardPool.Cards, count: 1, rnd, CardRarity.Uncommon));
                    break;
                case LevelReward.AddRareCreature:
                    playerDeck.AddRange(GameLobby.SelectRandomCards(cardPool.Cards, count: 1, rnd, CardRarity.Rare));
                    break;
                case LevelReward.IncreaseHandSize:
                    handSize++;
                    break;
                case LevelReward.RemoveCard:
                    // Let's only remove common creatures for now
                    var commonCreatures = playerDeck.Where(card => card.Rarity == CardRarity.Common).ToList();
                    if (commonCreatures.Count > 0)
                    {
                        playerDeck.Remove(rnd.SelectRandom(commonCreatures));
                    }
                    break;
            }
        }

        return new GameProgress
        {
            CardPool = cardPool,
            DeckCards = playerDeck,
            HandSize = handSize,
            Level = level,
        };;
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

    private static void WriteResultsCsv(List<(int poolId, int level, SimulatorResult result)> results, string filename)
    {
        DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
        var file = FileAccess.Open($"{Constants.UserDataDirectory}/{filename}", FileAccess.ModeFlags.Write);

        var colHeaders = new List<string> { "Pool", "Level", "TotalGames", "WinRate", "PlayerWin", "EnemyWin", "Stalemate", "MaxTurnsReached" };

        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine(string.Join(", ", colHeaders));

        foreach (var (poolId, level, result) in results)
        {
            int gamesPlayed = result.Rounds.Count;
            int playerWinCount = result.Rounds.Count(round => round.Result == RoundResult.PlayerWin);
            int enemyWinCount = result.Rounds.Count(round => round.Result == RoundResult.EnemyWin);
            int stalemateCount = result.Rounds.Count(round => round.Result == RoundResult.Stalemate);
            int maxTurnsCount = result.Rounds.Count(round => round.Result == RoundResult.MaxTurnsReached);

            float winRate = (float)playerWinCount / gamesPlayed;
            csvBuilder.AppendLine($"{poolId}, {level}, {gamesPlayed}, {winRate:f2}, {playerWinCount}, {enemyWinCount}, {stalemateCount}, {maxTurnsCount}");
        }

        file.StoreString(csvBuilder.ToString());
        file.Close();
    }

    private static void WriteCardPerformanceCsv(CardPerformanceSummary cardSummaries, string filename)
    {
        DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
        var file = FileAccess.Open($"{Constants.UserDataDirectory}/{filename}", FileAccess.ModeFlags.Write);

        var colHeaders = new List<string> { "Rarity", "BloodCost", "Attack", "Health", "AbilitiesCount", "Abilities", "Played Count", "Win Count", "Lose Count", "Total Damage Dealt", "Total Damage Received" };

        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine(string.Join(", ", colHeaders));

        foreach (var (key, summary) in cardSummaries.GetSummaries())
        {
            string abilitiesStr = key.Abilities.Count > 0 ? string.Join("-", key.Abilities) : CardAbilities.None.ToString();
            csvBuilder.AppendLine($"{key.Rarity}, {key.BloodCost}, {key.Attack}, {key.Health}, {key.Abilities.Count}, {abilitiesStr}, {summary.TimesPlayed}, {summary.TimesWon}, {summary.TimesLost}, {summary.DamageDealt}, {summary.DamageReceived}");
        }

        file.StoreString(csvBuilder.ToString());
        file.Close();
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class AIGenerator
{
    private static readonly string ResourceAiGeneratorDataPath = Path.Combine(Constants.GameSettingsDirectory, "ai.data.json");
    private static readonly string UserAiGeneratorDataPath = Path.Combine(Constants.UserDataDirectory, "ai.data.json");

    public class GeneratorData
    {
        [JsonPropertyName("ai_templates")]
        public AITemplate[] Templates { get; set; }
    }

    public class AITemplate
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("weight")]
        public string ProbabilityWeight { get; set; }
        [JsonPropertyName("min_level")]
        public int? MinLevel { get; set; }
        [JsonPropertyName("max_level")]
        public int? MaxLevel { get; set; }

    }

    public class GeneratorScriptedMove
    {

    }

    public static GameLevel GenerateGameLevel(CardPool cardPool, List<CardInfo> playerDeck, int level, int startingHandSize, int seed)
    {
        var rnd = new RandomGenerator(seed);
        var ai = GenerateEnemyAI(cardPool, level, rnd);
        (var difficulty, string guardrailReason) = CalculateLevelDifficulty(playerDeck, ai, level, startingHandSize, seed);
        var reward = GenerateLevelReward(difficulty, rnd);
        
        return new GameLevel
        {
            Level = level,
            Seed = seed,
            AI = ai,
            Difficulty = difficulty,
            GuardrailReason = guardrailReason,
            Reward = reward,
        };
    }

    public static EnemyAI GenerateEnemyAI(CardPool cardPool, int level, RandomGenerator rnd, bool log = true)
    {
        const int TOTAL_CARDS_RATE = 1;
        int totalCards = LinearScale(level, TOTAL_CARDS_RATE, min: 4, max: 12, y_intercept: 4, random_amount: 1, rnd: rnd);

        int oneCardsProbability = LinearScale(level, -5f, min: 20, max: 70, y_intercept: 85);
        int twoCardsProbability = LinearScale(level, 5f, min: 0, max: 30, x_intercept: 2);
        int threeCardsProbability = LinearScale(level, 2.5f, min: 0, max: 15, x_intercept: 4);
        int fourCardsProbability = LinearScale(level, 2.5f, min: 0, max: 10, x_intercept: 8);
        int zeroCardsProbability = 100 - oneCardsProbability - twoCardsProbability - threeCardsProbability - fourCardsProbability;

        int uncommonProbability = LinearScale(level, 2.5f, min: 0, max: 25);
        int rareProbability = LinearScale(level, 2.5f, min: 0, max: 25, x_intercept: 2);
        int commonProbability = 100 - uncommonProbability - rareProbability;

        // probability of card cost per turn Idx
        var oneCostProbabilities = new[] { 50, 75, 60, 50, 45, 40, 30, 20, 10, 00 };
        var twoCostProbabilities = new[] { 00, 05, 25, 45, 50, 50, 50, 50, 50, 50 };
        var threeCostProbabilities = new[] { 00, 00, 00, 05, 05, 10, 20, 30, 40, 50 };

        if (log) GD.Print($"Generating Enemy AI for level {level}: total={totalCards}; seed={rnd.Seed}[{rnd.N}]; concurrent probabilities=[{zeroCardsProbability:0.00}, {oneCardsProbability:0.00}, {twoCardsProbability:0.00}, {threeCardsProbability:0.00}, {fourCardsProbability:0.00}]; rarity probabilities=[{commonProbability:0.00},{uncommonProbability:0.00},{rareProbability:0.00}]");

        int turnId = 0;
        var moves = new List<ScriptedMove>();
        while (moves.Count < totalCards)
        {
            int concurrentCount = rnd.SelectRandomOdds(
                new[] { 0, 1, 2, 3, 4 },
                new[] { zeroCardsProbability, oneCardsProbability, twoCardsProbability, threeCardsProbability, fourCardsProbability }
            );

            int costProbabilityIdx = Math.Clamp(turnId, 0, 9);
            int oneCostProbability = oneCostProbabilities[costProbabilityIdx];
            int twoCostProbability = twoCostProbabilities[costProbabilityIdx];
            int threeCostProbability = threeCostProbabilities[costProbabilityIdx];
            int zeroCostProbability = 100 - oneCostProbability - twoCostProbability - threeCostProbability;

            // Bake-in the guardrails to make sure we play at least one card by turn 2
            int cardsPlayed = moves.Count;
            if (cardsPlayed == 0 && turnId == 1)
            {
                concurrentCount = 1;
            }

            if (log) GD.Print($"   Turn {turnId}: {concurrentCount} cards; cost probabilities=[{zeroCostProbability:0.00},{oneCostProbability:0.00},{twoCostProbability:0.00},{threeCostProbability:0.00}]");

            for (int i = 0; i < concurrentCount; i++)
            {
                CardBloodCost cost = rnd.SelectRandomOdds(
                    new[] { CardBloodCost.Zero, CardBloodCost.One, CardBloodCost.Two, CardBloodCost.Three },
                    new[] { zeroCostProbability, oneCostProbability, twoCostProbability, threeCostProbability }
                );

                CardRarity rarity = rnd.SelectRandomOdds(
                    new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare },
                    new[] { commonProbability, uncommonProbability, rareProbability }
                );

                if (cost == CardBloodCost.Zero && rarity == CardRarity.Common)
                {
                    // Just use sacrifice for this
                    rarity = CardRarity.Sacrifice;
                }

                if (log) GD.Print($"      {cost}:{rarity}");
                moves.Add(new ScriptedMove(turnId, cost, rarity));
            }

            turnId++;
        }

        return new EnemyAI(cardPool, moves, rnd);
    }

    private struct LevelState
    {
        public int Turn { get; set; }
        public int TotalAttack { get; set; }
        public int TotalHealth { get; set; }
        public int TotalCards { get; set; }
    }
    private class DifficultyMarker
    {
        public string Name { get; set; }
        public LevelDifficulty Marker { get; set; }
        public Func<LevelState, bool> Check { get; set; }
    }

    public static (LevelDifficulty difficulty, string error) CalculateLevelDifficulty(List<CardInfo> playerDeck, EnemyAI ai, int level, int startingHandSize, int seed)
    {
        const float MAX_SIMULATED_WIN_RATE = 0.99f;
        const float MIN_SIMULATED_WIN_RATE = 0.01f;
        if (!PassGeneratedLevelGuardrails(ai.Clone()))
        {
            return (LevelDifficulty.FailedGuardrail, "BasicMarkers");
        }
        
        // NB: This must match the GameLobby.InitializeGame method for accurate simulation
        var rnd = new RandomGenerator(seed);
        var sacrificeCards = playerDeck.Where(c => c.Rarity == CardRarity.Sacrifice);
        var creatureCards = playerDeck.Where(c => c.Rarity != CardRarity.Sacrifice);
        var sacrificeDeck = new Deck(sacrificeCards, rnd);
        var creaturesDeck = new Deck(creatureCards, rnd);

        var args = new SimulatorArgs
        {
            EnableLogging = false, // NB: Disable this for real gameplay
            EnableCardSummary = false,
            StartingHandSize = startingHandSize,
            SacrificesDeck = sacrificeDeck.Cards,
            CreaturesDeck = creaturesDeck.Cards,
            AI = ai,
        };

        var result = new GameSimulator(
            maxTurns: 20,
            maxBranchPerTurn: 2,
            maxStateQueueCircuitBreakerSize: 1000,
            alwaysTryDrawingCreature: true,
            alwaysTryDrawingSacrifice: true,
            checkDuplicateStates: true).Simulate(args);

        int gamesPlayed = result.Rounds.Count;
        int playerWinCount = result.Rounds.Count(round => round.Result == RoundResult.PlayerWin);
        int enemyWinCount = result.Rounds.Count(round => round.Result == RoundResult.EnemyWin);
        int stalemateCount = result.Rounds.Count(round => round.Result == RoundResult.Stalemate);
        int maxTurnsCount = result.Rounds.Count(round => round.Result == RoundResult.MaxTurnsReached);

        float winRate = (float)playerWinCount / gamesPlayed;
        GD.Print($"Level {level} simulated win rate: {winRate:0.00}; [{playerWinCount} wins, {enemyWinCount} losses, {stalemateCount} stalemates, {maxTurnsCount} max turns]");
        if (winRate < MIN_SIMULATED_WIN_RATE)
        {
            GD.Print($"FAILED GUARDRAIL. Win rate is too low: {winRate:0.00} < {MIN_SIMULATED_WIN_RATE:0.00}");
            return (LevelDifficulty.FailedGuardrail, "TooHard");
        }
        else if (winRate > MAX_SIMULATED_WIN_RATE)
        {
            GD.Print($"FAILED GUARDRAIL. Win rate is too high: {winRate:0.00} > {MAX_SIMULATED_WIN_RATE:0.00}");
            return (LevelDifficulty.FailedGuardrail, "TooEasy");
        }
        else if (winRate >= 0.80f)
        {
            return (LevelDifficulty.Easy, string.Empty);
        }
        else if (winRate >= 0.45f)
        {
            return (LevelDifficulty.Medium, string.Empty);
        }
        else
        {
            return (LevelDifficulty.Hard, string.Empty);
        }
    }

    // Is this still worth keeping?
    private static bool PassGeneratedLevelGuardrails(EnemyAI ai)
    {
        var markers = new[] {
            /* Invalid Level Markers */
            new DifficultyMarker {
                Name = "Must play a card before turn 2",
                Marker = LevelDifficulty.FailedGuardrail,
                Check = (step) => step.Turn == 1 && step.TotalCards == 0,
            },
            new DifficultyMarker {
                Name = "Must play at least 1 attack before turn 3",
                Marker = LevelDifficulty.FailedGuardrail,
                Check = (step) => step.Turn == 2 && step.TotalAttack == 0,
            },
            // new DifficultyMarker {
            // 	Name = "Don't play more than two cards the first turn",
            // 	Marker = LevelDifficulty.FailedGuardrail,
            // 	Check = (step) => step.Turn == 0 && step.TotalCards > 2,
            // },
            /* Hard Level Markers */
            // new DifficultyMarker {
            // 	Name = "Played 5 attack before turn 2",
            // 	Marker = LevelDifficulty.Hard,
            // 	Check = (step) => step.Turn == 1 && step.TotalAttack >= 5,
            // },
            // new DifficultyMarker {
            // 	Name = "Played 4 cards before turn 2",
            // 	Marker = LevelDifficulty.Hard,
            // 	Check = (step) => step.Turn == 1 && step.TotalCards >= 4,
            // },
            // new DifficultyMarker {
            // 	Name = "Played 10 attack before turn 4",
            // 	Marker = LevelDifficulty.Hard,
            // 	Check = (step) => step.Turn == 3 && step.TotalAttack >= 12,
            // },
            // new DifficultyMarker {
            // 	Name = "Played 8 cards before turn 4",
            // 	Marker = LevelDifficulty.Hard,
            // 	Check = (step) => step.Turn == 3 && step.TotalCards >= 8,
            // },
            /* Medium Level Markers */
            // new DifficultyMarker {
            // 	Name = "Played 3 attack before turn 2",
            // 	Marker = LevelDifficulty.Medium,
            // 	Check = (step) => step.Turn == 1 && step.TotalAttack >= 3,
            // },
            // new DifficultyMarker {
            // 	Name = "Played 3 cards before turn 2",
            // 	Marker = LevelDifficulty.Medium,
            // 	Check = (step) => step.Turn == 1 && step.TotalCards >= 3,
            // },
            // new DifficultyMarker {
            // 	Name = "Played 6 attack before turn 4",
            // 	Marker = LevelDifficulty.Medium,
            // 	Check = (step) => step.Turn == 3 && step.TotalAttack >= 6,
            // },
            // new DifficultyMarker {
            // 	Name = "Played 6 cards before turn 4",
            // 	Marker = LevelDifficulty.Medium,
            // 	Check = (step) => step.Turn == 3 && step.TotalCards >= 6,
            // },
        };

        var markerCount = new Dictionary<LevelDifficulty, int>() { { LevelDifficulty.Easy, 0 }, { LevelDifficulty.Medium, 0 }, { LevelDifficulty.Hard, 0 }, { LevelDifficulty.FailedGuardrail, 0 } };
        var state = new LevelState();
        for (int turnId = 0; turnId <= ai.MaxTurn; turnId++)
        {
            state.Turn = turnId;
            List<PlayedCard> playedCards = ai.GetMovesForTurn(turnId, new bool[4]);
            foreach (var playedCard in playedCards)
            {
                CardInfo cardInfo = playedCard.Card;
                state.TotalAttack += cardInfo.Attack;
                state.TotalHealth += cardInfo.Health;
                state.TotalCards++;
            }

            foreach (DifficultyMarker marker in markers)
            {
                if (marker.Check(state))
                {
                    GD.Print($"Generated Level has hit marker: {marker.Name}");
                    markerCount[marker.Marker] += 1;
                }
            }
        }

        return markerCount[LevelDifficulty.FailedGuardrail] == 0;
    }

    public static LevelReward GenerateLevelReward(LevelDifficulty difficulty, RandomGenerator rnd)
    {
        if (difficulty == LevelDifficulty.Hard)
        {
            return rnd.SelectRandomOdds(
                new[] { LevelReward.AddCreature, LevelReward.AddUncommonCreature, LevelReward.AddRareCreature, LevelReward.RemoveCard, LevelReward.IncreaseHandSize },
                new[] { 20, 40, 20, 10, 10 }
            );
        }
        else if (difficulty == LevelDifficulty.Medium)
        {
            return rnd.SelectRandomOdds(
                new[] { LevelReward.AddCreature, LevelReward.AddUncommonCreature, LevelReward.AddRareCreature, LevelReward.RemoveCard, LevelReward.IncreaseHandSize },
                new[] { 50, 15, 05, 25, 05 }
            );
        }
        else
        {
            return rnd.SelectRandomOdds(
                new[] { LevelReward.AddResource, LevelReward.AddCreature },
                new[] { 20, 80 }
            );
        }
    }

    public static float LinearScalef(
        float x,
        float rate,
        float min,
        float max,
        float x_intercept = 0,
        float y_intercept = 0,
        float random_amount = 0,
        RandomGenerator rnd = null)
    {
        float y = (x - x_intercept) * rate + y_intercept;
        if (rnd != null)
        {
            y += rnd.Nextf(-random_amount, random_amount);
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
        RandomGenerator rnd = null)
    {
        float y = LinearScalef(x, rate, min, max, x_intercept, y_intercept, random_amount, rnd);
        return Math.Clamp(Mathf.RoundToInt(y + 1e-6f), min, max);
    }

    public static void ResetAiGeneratorSettings()
    {
        DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
        // Bug Fix: The string "res://" is resolved as a relative path in a packaged game. So it fails during the CopyAbsolute method.
        // DirAccess.CopyAbsolute(TemplateDeckGeneratorDataPath, UserDeckGeneratorDataPath);
        var dir = DirAccess.Open("res://");
        dir.Copy(ResourceAiGeneratorDataPath, UserAiGeneratorDataPath);
    }

    private static GeneratorData LoadGeneratorData()
    {
        if (OS.IsDebugBuild() || !Godot.FileAccess.FileExists(UserAiGeneratorDataPath))
        {
            GD.Print("Copying over ai.data.json...");
            ResetAiGeneratorSettings();
        }

        var dataStr = Godot.FileAccess.GetFileAsString(UserAiGeneratorDataPath);
        var data = JsonSerializer.Deserialize<GeneratorData>(dataStr, new JsonSerializerOptions() { IncludeFields = true });
        GD.Print($"Loaded AI Generator Data with {data.Templates.Length} templates.");
        return data;
    }
}
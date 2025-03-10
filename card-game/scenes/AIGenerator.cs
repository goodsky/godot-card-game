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
        [JsonPropertyName("levels")]
        public LevelParameters Levels { get; set; }

        [JsonPropertyName("ai_generator_probabilities")]
        public RandomAIParameters RandomParameters { get; set; }

        [JsonPropertyName("ai_templates")]
        public AITemplate[] Templates { get; set; }
    }

    public class LevelParameters
    {
        [JsonPropertyName("use_template_probability")]
        public int[] TemplateProbability { get; set; }
        [JsonPropertyName("reward_odds")]
        public Dictionary<LevelDifficulty, Dictionary<LevelReward, int>> RewardOdds { get; set; }
        [JsonPropertyName("sacrifices_to_add_per_reward")]
        public int SacrificesToAddPerReward { get; set; }
    }

    public class RandomAIParameters
    {
        [JsonPropertyName("total_cards")]
        public LinearScaleParameters TotalCards { get; set; }
        [JsonPropertyName("play_one_card_probability")]
        public LinearScaleParameters PlayOneCardProbability { get; set; }
        [JsonPropertyName("play_two_cards_probability")]
        public LinearScaleParameters PlayTwoCardsProbability { get; set; }
        [JsonPropertyName("play_three_cards_probability")]
        public LinearScaleParameters PlayThreeCardsProbability { get; set; }
        [JsonPropertyName("play_four_cards_probability")]
        public LinearScaleParameters PlayFourCardsProbability { get; set; }
        [JsonPropertyName("play_uncommon_probability")]
        public LinearScaleParameters PlayUncommonProbability { get; set; }
        [JsonPropertyName("play_rare_probability")]
        public LinearScaleParameters PlayRareProbability { get; set; }
        [JsonPropertyName("play_one_cost_probability")]
        public int[] PlayOneCostProbability { get; set; }
        [JsonPropertyName("play_two_cost_probability")]
        public int[] PlayTwoCostProbability { get; set; }
        [JsonPropertyName("play_three_cost_probability")]
        public int[] PlayThreeCostProbability { get; set; }
    }

    public class AITemplate
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("weight")]
        public int? ProbabilityWeight { get; set; }
        [JsonPropertyName("min_level")]
        public int? MinLevel { get; set; }
        [JsonPropertyName("max_level")]
        public int? MaxLevel { get; set; }
        [JsonPropertyName("difficulty_override")]
        public LevelDifficulty? DifficultyOverride { get; set; }
        [JsonPropertyName("noun_override")]
        public string NounOverride { get; set; }
        [JsonPropertyName("adjective_override")]
        public string AdjectiveOverride { get; set; }
        [JsonPropertyName("scripted_moves")]
        public GeneratorScriptedMove[] ScriptedMoves { get; set; }
    }

    public class GeneratorScriptedMove
    {
        [JsonPropertyName("turn")]
        public int Turn { get; set; }
        [JsonPropertyName("min_level")]
        public int? MinLevel { get; set; }
        [JsonPropertyName("max_level")]
        public int? MaxLevel { get; set; }
        [JsonPropertyName("lane")]
        public int? Lane { get; set; }
        [JsonPropertyName("attack")]
        public int? Attack { get; set; }
        [JsonPropertyName("health")]
        public int? Health { get; set; }
        [JsonPropertyName("cost")]
        public CardBloodCost? BloodCost { get; set; }
        [JsonPropertyName("rarity")]
        public CardRarity? Rarity { get; set; }
        [JsonPropertyName("ability")]
        public CardAbilities? Ability { get; set; }
    }

    public static GameLevel GenerateGameLevel(CardPool cardPool, List<CardInfo> playerDeck, int level, int startingHandSize, int seed)
    {
        var rnd = new RandomGenerator(seed);
        var (ai, difficultyOverride) = GenerateEnemyAI(cardPool, level, rnd);
        (var difficulty, string guardrailReason) = CalculateLevelDifficulty(playerDeck, ai, level, startingHandSize, seed, useGuardrails: !ai.IsTemplate);

        if (difficultyOverride != null) difficulty = difficultyOverride.Value;
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

    public static (EnemyAI, LevelDifficulty?) GenerateEnemyAI(CardPool cardPool, int level, RandomGenerator rnd)
    {
        var aiData = LoadGeneratorData();
        var rndParams = aiData.RandomParameters;

        var levelTemplates = aiData.Templates
            .Where(t => t.MinLevel == null || t.MinLevel <= level)
            .Where(t => t.MaxLevel == null || t.MaxLevel >= level)
            .ToArray();

        int useTemplateProbability = GetValueFromArray(level, aiData.Levels.TemplateProbability);
        bool useTemplate = rnd.Next(100) < useTemplateProbability;
        if (useTemplate && levelTemplates.Length > 0)
        {
            AITemplate selectedLevelTemplate = rnd.SelectRandomOdds(
                levelTemplates,
                levelTemplates.Select(t => t.ProbabilityWeight ?? 1).ToArray());

            GD.Print($"Generating AI using Template. {levelTemplates.Length} templates available. Selected '{selectedLevelTemplate.Name}'.");
            EnemyAI templateAi = GenerateTemplateEnemyAI(
                cardPool,
                selectedLevelTemplate,
                level,
                rndParams,
                rnd);
            return (templateAi, selectedLevelTemplate.DifficultyOverride);
        }

        GD.Print("Generating random AI");
        EnemyAI randomAi = GenerateRandomEnemyAI(cardPool, level, rnd, rndParams, log: false);
        return (randomAi, null);
    }

    public static EnemyAI GenerateTemplateEnemyAI(CardPool cardPool, AITemplate template, int level, RandomAIParameters rndParams, RandomGenerator rnd)
    {
        if (!string.IsNullOrEmpty(template.NounOverride) || !string.IsNullOrEmpty(template.AdjectiveOverride))
        {
            GD.Print($"Overriding card pool with Noun='{template.NounOverride}' and Adjective='{template.AdjectiveOverride}'");
            cardPool = CardGenerator.OverrideCardPool(cardPool, rnd, level, template.NounOverride, template.AdjectiveOverride);
        }

        if (template.ScriptedMoves == null || template.ScriptedMoves.Length == 0)
        {
            GD.PushError("Template has no scripted moves!");
            return GenerateRandomEnemyAI(cardPool, level, rnd, rndParams, log: false);
        }

        var scriptedMoves = new List<ScriptedMove>();
        foreach (var move in template.ScriptedMoves)
        {
            if ((move.MinLevel != null && level < move.MinLevel) ||
                (move.MaxLevel != null && level > move.MaxLevel))
            {
                continue;
            }

            // If the blood cost is not specified, then match the curve for randomly generated AIs
            if (move.BloodCost == null)
            {
                int oneCostProbability = GetValueFromArray(move.Turn, rndParams.PlayOneCostProbability);
                int twoCostProbability = GetValueFromArray(move.Turn, rndParams.PlayTwoCostProbability);
                int threeCostProbability = GetValueFromArray(move.Turn, rndParams.PlayThreeCostProbability);
                int zeroCostProbability = 100 - oneCostProbability - twoCostProbability - threeCostProbability;
                move.BloodCost = rnd.SelectRandomOdds(
                    new[] { CardBloodCost.Zero, CardBloodCost.One, CardBloodCost.Two, CardBloodCost.Three },
                    new[] { zeroCostProbability, oneCostProbability, twoCostProbability, threeCostProbability }
                );
            }

            // If the rarity is not specified, then match the curve for randomly generated AIs
            if (move.Rarity == null)
            {
                int uncommonProbability = LinearScale(level, rndParams.PlayUncommonProbability);
                int rareProbability = LinearScale(level, rndParams.PlayRareProbability);
                int commonProbability = 100 - uncommonProbability - rareProbability;
                move.Rarity = rnd.SelectRandomOdds(
                    new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare },
                    new[] { commonProbability, uncommonProbability, rareProbability }
                );
            }

            CardInfo? cardInfo = GetBestMatchingCard(
                turn: move.Turn,
                cardPool: cardPool,
                rnd: rnd,
                cost: move.BloodCost,
                rarity: move.Rarity,
                ability: move.Ability,
                attack: move.Attack,
                health: move.Health
            );

            if (cardInfo == null)
            {
                GD.PushError($"Could not find a card for move! Cost = {move.BloodCost}; Rarity = {move.Rarity}; Attack = {move.Attack}; Health = {move.Health}; Ability = {move.Ability};");
                continue;
            }

            scriptedMoves.Add(new ScriptedMove(move.Turn, cardInfo.Value, move.Lane));
        }

        return new EnemyAI(cardPool, scriptedMoves, rnd, isTemplate: true);
    }

    public static EnemyAI GenerateRandomEnemyAI(CardPool cardPool, int level, RandomGenerator rnd, RandomAIParameters rndParams, bool log)
    {
        int totalCards = LinearScale(level, rndParams.TotalCards, rnd: rnd);

        int oneCardsProbability = LinearScale(level, rndParams.PlayOneCardProbability);
        int twoCardsProbability = LinearScale(level, rndParams.PlayTwoCardsProbability);
        int threeCardsProbability = LinearScale(level, rndParams.PlayThreeCardsProbability);
        int fourCardsProbability = LinearScale(level, rndParams.PlayFourCardsProbability);
        int zeroCardsProbability = 100 - oneCardsProbability - twoCardsProbability - threeCardsProbability - fourCardsProbability;

        int uncommonProbability = LinearScale(level, rndParams.PlayUncommonProbability);
        int rareProbability = LinearScale(level, rndParams.PlayRareProbability);
        int commonProbability = 100 - uncommonProbability - rareProbability;

        var oneCostProbabilities = rndParams.PlayOneCostProbability;
        var twoCostProbabilities = rndParams.PlayTwoCostProbability;
        var threeCostProbabilities = rndParams.PlayThreeCostProbability;

        if (log) GD.Print($"Generating Enemy AI for level {level}: total={totalCards}; seed={rnd.Seed}[{rnd.N}]; concurrent probabilities=[{zeroCardsProbability:0.00}, {oneCardsProbability:0.00}, {twoCardsProbability:0.00}, {threeCardsProbability:0.00}, {fourCardsProbability:0.00}]; rarity probabilities=[{commonProbability:0.00},{uncommonProbability:0.00},{rareProbability:0.00}]");

        int turnId = 0;
        int playedCards = 0;
        var moves = new List<ScriptedMove>();
        while (playedCards < totalCards)
        {
            int concurrentCount = rnd.SelectRandomOdds(
                new[] { 0, 1, 2, 3, 4 },
                new[] { zeroCardsProbability, oneCardsProbability, twoCardsProbability, threeCardsProbability, fourCardsProbability }
            );

            int oneCostProbability = GetValueFromArray(turnId, oneCostProbabilities);
            int twoCostProbability = GetValueFromArray(turnId, twoCostProbabilities);
            int threeCostProbability = GetValueFromArray(turnId, threeCostProbabilities);
            int zeroCostProbability = 100 - oneCostProbability - twoCostProbability - threeCostProbability;

            // Guardrail help: make sure we play at least one card by turn 2
            int cardsPlayed = moves.Count;
            if (cardsPlayed == 0 && turnId == 1)
            {
                concurrentCount = 1;
                if (log) GD.Print("Guardrail help: ensuring card played by turn 2");
            }

            // Guardrail help: make sure we play at least one attack by turn 3
            int? minAttack = null;
            int attackPlayed = moves.Sum(move => move.CardToPlay?.Attack ?? 0);
            if (attackPlayed == 0 && turnId == 2)
            {
                concurrentCount = 1;
                minAttack = 1;

                // Remove zero cost cards (which mostly have 0 attack)
                zeroCostProbability = 0;
                if (log) GD.Print("Guardrail help: removing zero cost cards");
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
                
                // I've switched to only using fully resolved CardInfo scripted moves.
                // This allows me to build in more guardrails for required attack played by the AI.
                var resolvedCardInfo = GetCardByCostRarityAndStats(turnId, cardPool, rnd, cost, rarity, minAttack);
                if (resolvedCardInfo == null)
                {
                    GD.PushError($"Could not find a card for move! Cost = {cost}; Rarity = {rarity};");
                    continue;
                }

                // Balance help: Uncommon and Rare cards count as more than one card played
                if (rarity == CardRarity.Sacrifice || rarity == CardRarity.Common) {
                    playedCards += 1;
                } else {
                    playedCards += 2; // uncommon and rare cards are worth more
                }

                moves.Add(new ScriptedMove(turnId, resolvedCardInfo.Value));
            }

            // Balance help: If we just played 3 or 4 cards, skip the next turn
            if (concurrentCount == 3) {
                turnId += 1;
            } else if (concurrentCount == 4) {
                turnId += 2;
            }

            turnId++;
        }

        return new EnemyAI(cardPool, moves, rnd);
    }

    public static CardInfo? GetCardByCostRarityAndStats(int turn, CardPool cardPool, RandomGenerator rnd, CardBloodCost cost, CardRarity? rarity, int? minAttack = null)
    {
        var possibleCards = cardPool.Cards.Where(card => card.BloodCost == cost);
        if (rarity != null)
        {
            possibleCards = possibleCards.Where(card => card.Rarity == rarity.Value);
        }

        if (minAttack != null)
        {
            possibleCards = possibleCards.Where(card => card.Attack >= minAttack.Value);
        }

        if (!possibleCards.Any())
        {
            return null;
        }

        return rnd.SelectRandom(possibleCards);
    }

    public static CardInfo? GetBestMatchingCard(
        int turn,
        CardPool cardPool,
        RandomGenerator rnd,
        CardBloodCost? cost,
        CardRarity? rarity,
        CardAbilities? ability,
        int? attack,
        int? health)
    {
        IEnumerable<CardInfo> possibleCards = cardPool.Cards;

        // The order of these filters is important. If any filter leaves us with an empty set, then it is skipped.
        // So the first filters are more likely to be applied.
        if (ability != null)
        {
            possibleCards = FilterIfPossible(possibleCards, card => card.Abilities.Contains(ability.Value));
        }

        if (attack != null)
        {
            possibleCards = FilterIfPossible(possibleCards, card => card.Attack == attack.Value);
        }

        if (health != null)
        {
            possibleCards = FilterIfPossible(possibleCards, card => card.Health == health.Value);
        }

        if (cost != null)
        {
            possibleCards = FilterIfPossible(possibleCards, card => card.BloodCost == cost.Value);
        }

        if (rarity != null)
        {
            possibleCards = FilterIfPossible(possibleCards, card => card.Rarity == rarity.Value);
        }

        if (!possibleCards.Any())
        {
            return null;
        }

        return rnd.SelectRandom(possibleCards);
    }

    private static IEnumerable<CardInfo> FilterIfPossible(IEnumerable<CardInfo> cards, Func<CardInfo, bool> filter)
    {
        if (cards == null || cards.Where(filter).Count() == 0)
        {
            return cards;
        }

        return cards.Where(filter);
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

    public static (LevelDifficulty difficulty, string error) CalculateLevelDifficulty(List<CardInfo> playerDeck, EnemyAI ai, int level, int startingHandSize, int seed, bool useGuardrails = true)
    {
        const float MAX_SIMULATED_WIN_RATE = 0.99f;
        const float MIN_SIMULATED_WIN_RATE = 0.01f;
        const int MIN_ENEMY_DAMAGE_DEALT = 3; // while simulating - make sure the enemy at least gets some damage in
        if (useGuardrails && !PassGeneratedLevelGuardrails(ai.Clone()))
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
        int enemyDamageDealt = result.Rounds.Sum(round => round.PlayerDamageReceived);
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
        else if (winRate > MAX_SIMULATED_WIN_RATE && enemyDamageDealt < MIN_ENEMY_DAMAGE_DEALT)
        {
            GD.Print($"FAILED GUARDRAIL. Win rate is too high: {winRate:0.00} > {MAX_SIMULATED_WIN_RATE:0.00} and enemy damage dealt is too low: {enemyDamageDealt} < {MIN_ENEMY_DAMAGE_DEALT}");
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
        var aiData = LoadGeneratorData();

        if (difficulty == LevelDifficulty.FailedGuardrail) difficulty = LevelDifficulty.Easy;
        var rewardOdds = aiData.Levels.RewardOdds[difficulty];     

        var rewardList = new List<LevelReward>();
        var oddsList = new List<int>();
        foreach (var kvp in rewardOdds)
        {
            rewardList.Add(kvp.Key);
            oddsList.Add(kvp.Value);
        }

        return rnd.SelectRandomOdds(rewardList.ToArray(), oddsList.ToArray());
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

    public class LinearScaleParameters
    {
        [JsonPropertyName("rate")]
        public float Rate { get; set; }
        [JsonPropertyName("min")]
        public int Min { get; set; }
        [JsonPropertyName("max")]
        public int Max { get; set; }
        [JsonPropertyName("x_intercept")]
        public int XIntercept { get; set; }
        [JsonPropertyName("y_intercept")]
        public int YIntercept { get; set; }
        [JsonPropertyName("random")]
        public int RandomAmount { get; set; }
    }

    public static int LinearScale(int x, LinearScaleParameters parameters, RandomGenerator rnd = null)
    {
        return LinearScale(
            x: x,
            rate: parameters.Rate,
            min: parameters.Min,
            max: parameters.Max,
            x_intercept: parameters.XIntercept,
            y_intercept: parameters.YIntercept,
            random_amount: parameters.RandomAmount,
            rnd: rnd
        );
    }

    public static T GetValueFromArray<T>(int turn, T[] values)
    {
        int idx = Math.Clamp(turn, 0, values.Length - 1);
        return values[idx];
    }

    public static void ResetAiGeneratorSettings()
    {
        DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
        // Bug Fix: The string "res://" is resolved as a relative path in a packaged game. So it fails during the CopyAbsolute method.
        // DirAccess.CopyAbsolute(TemplateDeckGeneratorDataPath, UserDeckGeneratorDataPath);
        var dir = DirAccess.Open("res://");
        dir.Copy(ResourceAiGeneratorDataPath, UserAiGeneratorDataPath);
        _cachedGeneratorData = null;
    }

    private static GeneratorData _cachedGeneratorData = null;
    public static GeneratorData LoadGeneratorData()
    {
        if (_cachedGeneratorData != null) return _cachedGeneratorData;

        if (OS.IsDebugBuild() || !Godot.FileAccess.FileExists(UserAiGeneratorDataPath))
        {
            GD.Print("Copying over ai.data.json...");
            ResetAiGeneratorSettings();
        }

        var dataStr = Godot.FileAccess.GetFileAsString(UserAiGeneratorDataPath);
        var data = JsonSerializer.Deserialize<GeneratorData>(dataStr, new JsonSerializerOptions() { IncludeFields = true });
        GD.Print($"Loaded AI Generator Data with {data.Templates.Length} templates.");

        _cachedGeneratorData = data;
        return data;
    }
}
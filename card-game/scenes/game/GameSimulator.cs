using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

public class SimulatorArgs
{
    public bool EnableLogging { get; set; }
    public int StartingHandSize { get; set; }
    public List<CardInfo> CreaturesDeck { get; set; }
    public List<CardInfo> SacrificesDeck { get; set; }
    public EnemyAI AI { get; set; }
}

public struct SimulatorRoundResult
{
    public int Turns { get; set; }
    public RoundResult Result { get; set; }
    public int PlayerDamageReceived { get; set; }
    public int EnemyDamageReceived { get; set; }
}

public enum RoundResult
{
    PlayerWin,
    EnemyWin,
    Stalemate,
    MaxTurnsReached,
}

public class SimulatorResult
{
    public List<SimulatorRoundResult> Rounds { get; set; }
}

public class GameSimulator
{
    protected class SimulatorState
    {
        public int Turn { get; set; }
        public bool IsPlayerMove { get; set; }
        public int PlayerDamageReceived { get; set; }
        public int EnemyDamageReceived { get; set; }
        public SimulatorHand Hand { get; set; }
        public SimulatorLanes Lanes { get; set; }
        public SimulatorDeck Creatures { get; set; }
        public SimulatorDeck Sacrifices { get; set; }
        public EnemyAI AI { get; set; }
        public SimulatorLogger Logger { get; set; }
        public List<SimulatorRoundResult> Results { get; set; }

        public SimulatorState Clone()
        {
            return new SimulatorState
            {
                Turn = Turn,
                IsPlayerMove = IsPlayerMove,
                PlayerDamageReceived = PlayerDamageReceived,
                EnemyDamageReceived = EnemyDamageReceived,
                Hand = new SimulatorHand(Hand),
                Lanes = new SimulatorLanes(Lanes),
                Creatures = new SimulatorDeck(Creatures),
                Sacrifices = new SimulatorDeck(Sacrifices),
                AI = AI.Clone(),
                Logger = Logger,
                Results = Results,
            };
        }
    }
    
    private static SimulatorState InitializeSimulationState(SimulatorArgs args)
    {
        SimulatorCard.NextId = 0; // This is not thread safe

        var state = new SimulatorState
        {
            Turn = 1, // turn 0 is only for staging the enemy cards
            IsPlayerMove = true,
            PlayerDamageReceived = 0,
            EnemyDamageReceived = 0,
            Hand = new SimulatorHand(),
            Lanes = new SimulatorLanes(),
            Creatures = new SimulatorDeck(args.CreaturesDeck),
            Sacrifices = new SimulatorDeck(args.SacrificesDeck),
            AI = args.AI,
            Logger = new SimulatorLogger(args.EnableLogging),
            Results = new List<SimulatorRoundResult>(),
        };

        for (int i = 0; i < args.StartingHandSize; i++)
        {
            state.Hand.Add(state.Creatures.DrawFromTop());
        }

        List<PlayedCard> initialCards = state.AI.GetMovesForTurn(0, new bool[SimulatorLanes.COL_COUNT]);
        foreach (var playedCard in initialCards)
        {
            state.Lanes.PlayCard(new SimulatorCard(playedCard.Card), playedCard.Lane, isEnemy: true);
        }

        return state;
    }

    private static void ResolveCombat(SimulatorState state, bool isPlayerTurn)
    {
        state.Logger.LogHeader("COMBAT PHASE");
        for (int laneColumn = 0; laneColumn < SimulatorLanes.COL_COUNT; laneColumn++)
        {
            SimulatorCard playerCard = state.Lanes.GetCardAt(SimulatorLanes.PLAYER_LANE_ROW, laneColumn);
            SimulatorCard enemyCard = state.Lanes.GetCardAt(SimulatorLanes.ENEMY_LANE_ROW, laneColumn);

            if (isPlayerTurn)
            {
                if (playerCard != null)
                {
                    int damage = CombatHelper.CardDamage(playerCard.Card, enemyCard?.Card);
                    if (CombatHelper.IsBlocked(playerCard.Card, enemyCard?.Card))
                    {
                        enemyCard.DamageReceived += damage;
                        if (enemyCard.DamageReceived >= enemyCard.Card.Health)
                        {
                            state.Logger.Log($"Enemy card {enemyCard.Card.Name} killed!");
                            state.Lanes.TryRemoveCardById(enemyCard.Id);
                        }
                    }
                    else
                    {
                        state.Logger.Log($"Enemy takes {damage} damage from {playerCard.Card.Name}");
                        state.EnemyDamageReceived += damage;
                    }
                }
            }
            else // enemy's turn
            {
                if (enemyCard != null)
                {
                    int damage = CombatHelper.CardDamage(enemyCard.Card, playerCard?.Card);
                    if (CombatHelper.IsBlocked(enemyCard.Card, playerCard?.Card))
                    {
                        playerCard.DamageReceived += damage;
                        if (playerCard.DamageReceived >= playerCard.Card.Health)
                        {
                            state.Logger.Log($"Player card {playerCard.Card.Name} killed!");
                            state.Lanes.TryRemoveCardById(playerCard.Id);
                        }
                    }
                    else
                    {
                        state.Logger.Log($"Player takes {damage} damage from {enemyCard.Card.Name}");
                        state.PlayerDamageReceived += damage;
                    }
                }
            }
        }
    }

    private static void ResolveAction(SimulatorState state, PlayerTurnAction action)
    {
        if (action.DrawAction == DrawAction.DrawFromCreatures)
        {
            var newCreature = state.Creatures.DrawFromTop();
            state.Hand.Add(newCreature);
        }
        else if (action.DrawAction == DrawAction.DrawFromSacrifices)
        {
            var newSacrifice = state.Sacrifices.DrawFromTop();
            state.Hand.Add(newSacrifice);
        }

        foreach (var cardAction in action.CardActions)
        {
            PlayCardFromHand(state, cardAction.PlayedCard, cardAction.LaneColumnIndex, cardAction.SacrificeCards);
        }
    }

    private static void PlayCardFromHand(SimulatorState state, SimulatorCard card, int laneCol, IEnumerable<SimulatorCard> sacrifices)
    {
        // Clear sacrifices
        foreach (var sacrifice in sacrifices)
        {
            if (state.Hand.RemoveCardById(sacrifice.Id))
            {
                state.Logger.Log($"Sacrificed {sacrifice.Card.Name} from hand.");
            }
            else if (state.Lanes.TryRemoveCardById(sacrifice.Id))
            {
                state.Logger.Log($"Sacrificed {sacrifice.Card.Name} from board.");
            }
            else
            {
                throw new InvalidOperationException($"Attempted to sacrifice a card that was not in the hand or on the board! [{sacrifice.Card.Name}]");
            }
        }

        // Remove the card from the hand
        if (!state.Hand.RemoveCardById(card.Id))
        {
            throw new InvalidOperationException($"Attempted to play a card that was not in the hand! [{card.Card.Name}]");
        }

        // Play the card in the lane
        state.Lanes.PlayCard(card, laneCol, isEnemy: false);
    }

    private int _maxTurns;
    private int _maxCardActionBranchesPerTurn;
    private bool _alwaysTryDrawingCreature;
    private bool _alwaysTryDrawingSacrifice;

    public GameSimulator(
        int maxTurns = 50,
        int maxBranchPerTurn = 1,
        bool alwaysTryDrawingCreature = false,
        bool alwaysTryDrawingSacrifice = false)
    {
        _maxTurns = maxTurns;
        _maxCardActionBranchesPerTurn = maxBranchPerTurn;
        _alwaysTryDrawingCreature = alwaysTryDrawingCreature;
        _alwaysTryDrawingSacrifice = alwaysTryDrawingSacrifice;
    }

    /** Game Simulator entry point */
    public SimulatorResult Simulate(SimulatorArgs args)
    {
        SimulatorState initialState = InitializeSimulationState(args);
        SimulatorLogger logger = initialState.Logger;

        try {
            var stateQueue = new Queue<SimulatorState>();
            var roundResults = new List<SimulatorRoundResult>();
            var enqueueStateIfNotGameOver = (SimulatorState state) =>
            {
                RoundResult? result = null;
                if (IsGameOver(state))
                {
                    result = state.PlayerDamageReceived < state.EnemyDamageReceived ? RoundResult.PlayerWin : RoundResult.EnemyWin;
                }
                else if (IsStalemate(state))
                {
                    result = RoundResult.Stalemate;
                }
                else if (state.Turn > _maxTurns)
                {
                    result = RoundResult.MaxTurnsReached;
                }

                if (result != null)
                {
                    var roundResult = new SimulatorRoundResult
                    {
                        Turns = state.Turn,
                        Result = result.Value,
                        PlayerDamageReceived = state.PlayerDamageReceived,
                        EnemyDamageReceived = state.EnemyDamageReceived,
                    };

                    logger.LogRoundResult(roundResult);
                    roundResults.Add(roundResult);
                }
                else
                {
                    stateQueue.Enqueue(state);
                }
            };

            stateQueue.Enqueue(initialState);
            while (stateQueue.Count > 0)
            {
                SimulatorState state = stateQueue.Dequeue();
                if (state.IsPlayerMove)
                {
                    state.Logger.LogHeader($"TURN {state.Turn}");
                    state.Logger.LogHeader(">> PLAYER TURN <<");
                    state.Logger.LogHand(state.Hand);
                    state.Logger.LogDeck(state.Creatures, "Creatures");
                    state.Logger.LogDeck(state.Sacrifices, "Sacrifices");
                    state.Logger.LogLanes(state.Lanes);

                    IEnumerable<SimulatorState> playerNextStates = StepPlayerTurnState(state);
                    foreach (var nextState in playerNextStates)
                    {
                        enqueueStateIfNotGameOver(nextState);
                    }
                }
                else
                {
                    state.Logger.LogHeader(">> AI TURN <<");
                    SimulatorState enemyNextState = StepEnemyTurnState(state);
                    enqueueStateIfNotGameOver(enemyNextState);
                }
            }
            
            logger.Log($"Completed simulation with {roundResults.Count} rounds.");
            return new SimulatorResult
            {
                Rounds = roundResults,
            };
        }
        finally
        {
            logger.Dispose();
        }
    }

    private IEnumerable<SimulatorState> StepPlayerTurnState(SimulatorState state)
    {
        IEnumerable<PlayerTurnAction> actions = GetPlayerActions(state);
        foreach (var action in actions)
        {
            SimulatorState nextState = state.Clone();
            ResolveAction(nextState, action);
            ResolveCombat(nextState, isPlayerTurn: true);
            nextState.IsPlayerMove = false;
            yield return nextState;
        }
    }

    private SimulatorState StepEnemyTurnState(SimulatorState state)
    {
        state.Lanes.PromoteStagedCards();

        bool[] stageLaneOccupied = state.Lanes.GetRowCards(SimulatorLanes.ENEMY_STAGE_LANE_ROW).Select(c => c != null).ToArray();
        List<PlayedCard> enemyMoves = state.AI.GetMovesForTurn(state.Turn, stageLaneOccupied);
        foreach (var playedCard in enemyMoves)
        {
            state.Logger.Log($"Enemy plays {playedCard.Card.Name} in lane {playedCard.Lane}");
            state.Lanes.PlayCard(new SimulatorCard(playedCard.Card), playedCard.Lane, isEnemy: true);
        }

        ResolveCombat(state, isPlayerTurn: false);
        state.Turn++;
        state.IsPlayerMove = true;
        return state; // NB: not cloning here since we should not be forking the state
    }

    private IEnumerable<PlayerTurnAction> GetPlayerActions(SimulatorState state)
    {
        List<PlayerTurnAction> bestPlayerActions = PlayerTurnAction.TakeTop(
            count: _maxCardActionBranchesPerTurn,
            minValue: 1,
            CalculateBestPlayerActionsDrawCreature(state),
            CalculateBestPlayerActionsDrawSacrifice(state));

        if (_alwaysTryDrawingCreature && state.Creatures.RemainingCardCount > 0)
        {
            bestPlayerActions.Add(new PlayerTurnAction() {
                DrawAction = DrawAction.DrawFromCreatures,
                CardActions = new PlayCardAction[0],
            });
        }

        if (_alwaysTryDrawingSacrifice && state.Sacrifices.RemainingCardCount > 0)
        {
            bestPlayerActions.Add(new PlayerTurnAction() {
                DrawAction = DrawAction.DrawFromSacrifices,
                CardActions = new PlayCardAction[0],
            });
        }

        if (bestPlayerActions.Count == 0)
        {
            state.Logger.Log($"No action found! Let's just try drawing a card if possible.");

            DrawAction drawAction = DrawAction.NoDraw;
            if (state.Sacrifices.RemainingCardCount > 0)
            {
                drawAction = DrawAction.DrawFromSacrifices;
            }
            else if (state.Creatures.RemainingCardCount > 0)
            {
                drawAction = DrawAction.DrawFromCreatures;
            }

            bestPlayerActions.Add(new PlayerTurnAction() {
                DrawAction = drawAction,
                CardActions = new PlayCardAction[0]
            });
        }

        state.Logger.LogHeader("Next Player Actions:");
        state.Logger.LogActions(bestPlayerActions);
        return bestPlayerActions;
    }

    private IEnumerable<PlayerTurnAction> CalculateBestPlayerActionsDrawCreature(SimulatorState state)
    {
        if (state.Creatures.RemainingCardCount == 0)
        {
            state.Logger.Log("Creatures deck is empty! - Skipping player actions for drawing creatures");
            return Enumerable.Empty<PlayerTurnAction>();
        }

        var newCreature = state.Creatures.PeekTop();
        var newHand = new SimulatorHand(state.Hand).Add(newCreature);

        state.Logger.LogHeader("[Calculate Action] DRAW CREATURE");
        return CalculateBestPlayerActions(DrawAction.DrawFromCreatures, newHand, state.Lanes, state.Logger, maxActions: _maxCardActionBranchesPerTurn);
    }

    private IEnumerable<PlayerTurnAction> CalculateBestPlayerActionsDrawSacrifice(SimulatorState state)
    {
        if (state.Sacrifices.RemainingCardCount == 0)
        {
            state.Logger.Log("Sacrifices deck is empty! - Skipping player actions for drawing sacrifices");
            return Enumerable.Empty<PlayerTurnAction>();
        }

        var newSacrifice = state.Sacrifices.PeekTop();
        var newHand = new SimulatorHand(state.Hand).Add(newSacrifice);

        state.Logger.LogHeader("[Calculate Action] DRAW SACRIFICE");
        return CalculateBestPlayerActions(DrawAction.DrawFromSacrifices, newHand, state.Lanes, state.Logger, maxActions: _maxCardActionBranchesPerTurn);
    }

    protected static IEnumerable<PlayerTurnAction> CalculateBestPlayerActions(
        DrawAction action,
        SimulatorHand hand,
        SimulatorLanes lanes,
        SimulatorLogger logger,
        int maxActions,
        bool maxOneActionPerCard = true)
    {
        List<SimulatorCard> sacrificesOnBoard = GetAvailableSacrifices(lanes, sort: true);
        List<SimulatorCard> sacrificesInHand = hand.Cards.Where(card => card.Card.BloodCost == CardBloodCost.Zero).ToList();
        List<SimulatorCard> creaturesInHand = hand.Cards.Where(card => card.Card.BloodCost != CardBloodCost.Zero).ToList();
        int openLanes = SimulatorLanes.COL_COUNT - sacrificesOnBoard.Count;

        var bestActions = new List<PlayerTurnAction>();
        foreach (SimulatorCard creatureInHand in creaturesInHand)
        {
            logger.Log($"+++ Checking if we can play {creatureInHand.Card.Name} [{creatureInHand.Card.Attack}/{creatureInHand.Card.Health}]");
            int neededSacrificeCards = (int)creatureInHand.Card.BloodCost;

            List<SimulatorCard> sacrifices = new List<SimulatorCard>();

            int possibleSacrificesFromHand = Math.Min(sacrificesInHand.Count, openLanes);
            int neededSacrificesFromHand = Math.Min(neededSacrificeCards, possibleSacrificesFromHand);
            sacrifices.AddRange(sacrificesInHand.Take(neededSacrificesFromHand));

            int neededSacrificesFromBoard = neededSacrificeCards - sacrifices.Count;
            sacrifices.AddRange(sacrificesOnBoard.Take(neededSacrificesFromBoard));

            if (sacrifices.Count != neededSacrificeCards)
            {
                logger.Log($"   Could not play {creatureInHand.Card.Name} - found {sacrifices.Count} sacrifices, needed {neededSacrificeCards}");
                continue;
            }

            int opportunityCostOfSacrifices = sacrifices.Sum(sacrifice => OpportunityCostOfSacrifice(sacrifice, lanes));
            PlayerTurnAction bestActionForCard = null;
            for (int laneColumn = 0; laneColumn < SimulatorLanes.COL_COUNT; laneColumn++)
            {
                SimulatorCard existingCard = lanes.GetCardAt(SimulatorLanes.PLAYER_LANE_ROW, laneColumn);
                if (existingCard != null && !sacrifices.Contains(existingCard))
                {
                    logger.Log($"   Not playing {creatureInHand.Card.Name} in lane {laneColumn} - occupied by {existingCard.Card.Name}");
                    continue;
                }

                int opportunityScore = OpportunityScoreOfPlayingCard(creatureInHand, laneColumn, lanes);
                int actionHeuristicScore = opportunityScore - opportunityCostOfSacrifices;

                logger.Log($"   >>> {creatureInHand.Card.Name} in lane {laneColumn} : Heuristic = {actionHeuristicScore} (opportunity score {opportunityScore} - opportunity cost {opportunityCostOfSacrifices}) [{string.Join(", ", sacrifices.Select(card => card.Card.Name))}]");
                var actionForCard = new PlayerTurnAction
                {
                    DrawAction = action,
                    CardActions = new PlayCardAction[]
                    {
                        new PlayCardAction
                        {
                            PlayedCard = creatureInHand,
                            LaneColumnIndex = laneColumn,
                            SacrificeCards = sacrifices,
                        }
                    },
                    HeuristicScore = actionHeuristicScore,
                };

                if (maxOneActionPerCard)
                {
                    if (bestActionForCard == null || actionForCard.HeuristicScore > bestActionForCard.HeuristicScore)
                    {
                        bestActionForCard = actionForCard;
                    }
                }
                else
                {
                    PlayerTurnAction.AddIfInTopN(bestActions, maxActions, actionForCard);
                }
            }

            if (maxOneActionPerCard && bestActionForCard != null)
            {
                PlayerTurnAction.AddIfInTopN(bestActions, maxActions, bestActionForCard);
            }
        }

        return bestActions;
    }

    /** Get available sacrifices from the board, sorted by how willing we are to sacrifice them */
    protected static List<SimulatorCard> GetAvailableSacrifices(SimulatorLanes lanes, bool sort = true)
    {
        var availableSacrifices = lanes.GetRowCards(SimulatorLanes.PLAYER_LANE_ROW).Where(c => c != null).ToList();

        if (sort)
        {
            availableSacrifices.Sort(SacrificeCardComparerFactory(lanes));
        }
        
        return availableSacrifices;
    }

    /** Sort cards by how willing we are to sacrifice them */
    protected static IComparer<SimulatorCard> SacrificeCardComparerFactory(SimulatorLanes lanes)
    {
        Dictionary<SimulatorCard, int> opportunityCosts = new Dictionary<SimulatorCard, int>();
        return Comparer<SimulatorCard>.Create((cardA, cardB) =>
        {
            if (!opportunityCosts.TryGetValue(cardA, out int opportunityCostA))
            {
                opportunityCosts[cardA] = OpportunityCostOfSacrifice(cardA, lanes);
            }

            if (!opportunityCosts.TryGetValue(cardB, out int opportunityCostB))
            {
                opportunityCosts[cardB] = OpportunityCostOfSacrifice(cardB,  lanes);
            }

            return opportunityCostA - opportunityCostB;
        });
    }

    /** Heuristic score to represent the opportunity cost of sacrificing a card
        Calculated as # damage preventing + damage dealing over the next N turns */
    protected static int OpportunityCostOfSacrifice(SimulatorCard card, SimulatorLanes lanes, int turnCount = 3)
    {
        var lane = lanes.GetLaneCards(card);
        if (lane == null)
        {
            // A card that isn't on the board yet has no opportunity cost (it may be in hand)
            return 0;
        }

        LaneCombatAnalysis statusQuoAnalysis = AnalyzeLaneCombat(
            playerCard: lane[SimulatorLanes.PLAYER_LANE_ROW],
            enemyCard: lane[SimulatorLanes.ENEMY_LANE_ROW],
            stagedCard: lane[SimulatorLanes.ENEMY_STAGE_LANE_ROW],
            turnCount: turnCount
        );

        LaneCombatAnalysis sacrificeCardAnalysis = AnalyzeLaneCombat(
            playerCard: null,
            enemyCard: lane[SimulatorLanes.ENEMY_LANE_ROW],
            stagedCard: lane[SimulatorLanes.ENEMY_STAGE_LANE_ROW],
            turnCount: turnCount
        );

        int damagePreventing = sacrificeCardAnalysis.PlayerDamageReceived - statusQuoAnalysis.PlayerDamageReceived;
        int damageDealing = statusQuoAnalysis.EnemyDamageReceived - sacrificeCardAnalysis.EnemyDamageReceived;

        return damagePreventing + damageDealing;
    }

    /** Heuristic score to represent the opportunity of playing a card in a lane
        Calculated as # damage preventing + damage dealing over the next N turns */
    protected static int OpportunityScoreOfPlayingCard(SimulatorCard card, int laneIdx, SimulatorLanes lanes, int turnCount = 3)
    {
        var lane = lanes.GetLaneCards(laneIdx);
        // NB: this assumes that we have sacrificed any card that is already in the lane.
        LaneCombatAnalysis withoutCardAnalysis = AnalyzeLaneCombat(
            playerCard: null,
            enemyCard: lane[SimulatorLanes.ENEMY_LANE_ROW],
            stagedCard: lane[SimulatorLanes.ENEMY_STAGE_LANE_ROW],
            turnCount: turnCount
        );

        LaneCombatAnalysis playingCardAnalysis = AnalyzeLaneCombat(
            playerCard: card,
            enemyCard: lane[SimulatorLanes.ENEMY_LANE_ROW],
            stagedCard: lane[SimulatorLanes.ENEMY_STAGE_LANE_ROW],
            turnCount: turnCount
        );

        int damagePreventing = withoutCardAnalysis.PlayerDamageReceived - playingCardAnalysis.PlayerDamageReceived;
        int damageDealing = playingCardAnalysis.EnemyDamageReceived - withoutCardAnalysis.EnemyDamageReceived;

        return damagePreventing + damageDealing;
    }

    internal struct LaneCombatAnalysis
    {
        public int PlayerDamageReceived { get; set; }
        public int PlayerCardDamageReceived { get; set; }
        public int EnemyDamageReceived { get; set; }
        public int EnemyCardDamageReceived { get; set; }
    }

    internal static LaneCombatAnalysis AnalyzeLaneCombat(
        SimulatorCard playerCard,
        SimulatorCard enemyCard,
        SimulatorCard stagedCard,
        int turnCount = 3,
        bool playerMovesFirst = true)
    {
        var analysis = new LaneCombatAnalysis
        {
            PlayerDamageReceived = 0,
            PlayerCardDamageReceived = 0,
            EnemyDamageReceived = 0,
            EnemyCardDamageReceived = 0,
        };

        // make copies so we don't affect the original cards damage state
        playerCard = playerCard == null ? null :  new SimulatorCard(playerCard);
        enemyCard = enemyCard == null ? null : new SimulatorCard(enemyCard);
        stagedCard = stagedCard == null ? null : new SimulatorCard(stagedCard);

        // maybe this is overkill - but actually step through the combat to see what is going to happen
        for (int turns = 0; turns < turnCount; turns++)
        {
            for (int i = 0; i < 2; i++)
            {
                bool isPlayersTurn = i == 0 && playerMovesFirst || i == 1 && !playerMovesFirst; 
                if (isPlayersTurn) // player's turn
                {
                    if (playerCard != null)
                    {
                        int damage = CombatHelper.CardDamage(playerCard.Card, enemyCard?.Card);
                        if (CombatHelper.IsBlocked(playerCard.Card, enemyCard?.Card))
                        {
                            analysis.EnemyCardDamageReceived += damage;

                            enemyCard.DamageReceived += damage;
                            if (enemyCard.DamageReceived >= enemyCard.Card.Health)
                            {
                                enemyCard = null;
                            }
                        }
                        else
                        {
                            analysis.EnemyDamageReceived += damage;
                        }
                    }
                }
                else // enemy's turn
                {
                    if (enemyCard == null && stagedCard != null)
                    {
                        enemyCard = stagedCard;
                        stagedCard = null;
                    }

                    if (enemyCard != null)
                    {
                        int damage = CombatHelper.CardDamage(enemyCard.Card, playerCard?.Card);
                        if (CombatHelper.IsBlocked(enemyCard.Card, playerCard?.Card))
                        {
                            analysis.PlayerCardDamageReceived += damage;

                            playerCard.DamageReceived += damage;
                            if (playerCard.DamageReceived >= playerCard.Card.Health)
                            {
                                playerCard = null;
                            }
                        }
                        else
                        {
                            analysis.PlayerDamageReceived += damage;
                        }
                    }
                }
            }
        }

        return analysis;
    }

    private bool IsGameOver(SimulatorState state)
    {
        int netDamage = state.EnemyDamageReceived - state.PlayerDamageReceived;
        state.Logger.Log($"[CHECK IF GAME OVER] Net Damage={netDamage} [EnemyReceived: {state.EnemyDamageReceived} / PlayerReceived: {state.PlayerDamageReceived}]\n");
        return Math.Abs(netDamage) >= 5 || state.Turn > 100;
    }

    private bool IsStalemate(SimulatorState state)
    {
        bool playerHasNoCards = state.Hand.Cards.Count == 0 && state.Creatures.RemainingCardCount == 0;
        bool enemyHasNoMoreCards = state.AI.MaxTurn < state.Turn;
        if (playerHasNoCards && enemyHasNoMoreCards)
        {
            int netPlayerAttack = state.Lanes.GetRowCards(SimulatorLanes.PLAYER_LANE_ROW).Sum(card => card?.Card.Attack ?? 0);
            int netEnemyAttack = state.Lanes.GetRowCards(SimulatorLanes.ENEMY_LANE_ROW).Sum(card => card?.Card.Attack ?? 0)
                + state.Lanes.GetRowCards(SimulatorLanes.ENEMY_STAGE_LANE_ROW).Sum(card => card?.Card.Attack ?? 0);

            return netPlayerAttack == netEnemyAttack;
        }

        return false;
    }
}

public class SimulatorCard
{
    public static int NextId = 1;

    public int Id { get; set; }
    public CardInfo Card { get; set; }
    public int DamageReceived { get; set; }

    public SimulatorCard(CardInfo cardInfo)
    {
        Id = NextId++;
        Card = cardInfo;
        DamageReceived = 0;
    }

    public SimulatorCard(SimulatorCard other)
    {
        Id = other.Id;
        Card = other.Card;
        DamageReceived = other.DamageReceived;
    }
}

public class SimulatorHand
{
    public List<SimulatorCard> Cards { get; set; }

    public SimulatorHand()
    {
        Cards = new List<SimulatorCard>();
    }

    public SimulatorHand(SimulatorHand other)
    {
        Cards = other.Cards.Select(card => new SimulatorCard(card)).ToList();
    }

    public SimulatorHand Add(SimulatorCard card)
    {
        Cards.Add(card);
        return this;
    }

    public bool RemoveCardById(int cardId)
    {
        int idx = Cards.FindIndex(x => x.Id == cardId);
        if (idx == -1)
        {
            return false;
        }

        Cards.RemoveAt(idx);
        return true;
    }
}

public class SimulatorDeck
{
    private List<SimulatorCard> _deck;
    private int _drawnCardsCount;

    public int RemainingCardCount => _deck.Count - _drawnCardsCount;

    public SimulatorDeck(List<CardInfo> cards)
    {
        _deck = new List<SimulatorCard>(cards.Select(card => new SimulatorCard(card)));
        _drawnCardsCount = 0;
    }

    public SimulatorDeck(SimulatorDeck other)
    {
        _deck = new List<SimulatorCard>(other._deck);
        _drawnCardsCount = other._drawnCardsCount;
    }

    public SimulatorCard DrawFromTop()
    {
        return _deck[_deck.Count - 1 - _drawnCardsCount++];
    }

    public SimulatorCard PeekTop()
    {
        return _deck[_deck.Count - 1 - _drawnCardsCount];
    }
}

public class SimulatorLanes
{
    public static readonly int COL_COUNT = 4;
    public static readonly int ROW_COUNT = 3;
    public static readonly int PLAYER_LANE_ROW = 0;
	public static readonly int ENEMY_LANE_ROW = 1;
	public static readonly int ENEMY_STAGE_LANE_ROW = 2;

    private SimulatorCard[,] _lanes;

    public SimulatorLanes()
    {
        _lanes = new SimulatorCard[COL_COUNT, ROW_COUNT];
    }

    public SimulatorLanes(SimulatorLanes other)
    {
        _lanes = new SimulatorCard[COL_COUNT, ROW_COUNT];
        for (int col = 0; col < COL_COUNT; col++)
        {
            for (int row = 0; row < ROW_COUNT; row++)
            {
                _lanes[col, row] = other._lanes[col, row] != null ? new SimulatorCard(other._lanes[col, row]) : null;
            }
        }
    }

    public SimulatorCard GetCardAt(int row, int col)
    {
        return _lanes[col, row];
    }

    public List<SimulatorCard> GetLaneCards(int col)
    {
        var lane = new List<SimulatorCard>(ROW_COUNT);
        for (int i = 0; i < ROW_COUNT; i++)
        {
            lane.Add(_lanes[col, i]);
        }

        return lane;
    }

    public List<SimulatorCard> GetLaneCards(SimulatorCard card)
    {
        for (int col = 0; col < COL_COUNT; col++)
        {
            if (_lanes[col, PLAYER_LANE_ROW] == card)
            {
                return GetLaneCards(col);
            }
        }

        return null;
    }

    public List<SimulatorCard> GetRowCards(int row)
    {
        var cards = new List<SimulatorCard>();
        for (int col = 0; col < COL_COUNT; col++)
        {
            cards.Add(_lanes[col, row]);
        }

        return cards;
    }

    public void PlayCard(SimulatorCard card, int laneColumn, bool isEnemy)
    {
        int playerLaneRow = isEnemy ? ENEMY_STAGE_LANE_ROW : PLAYER_LANE_ROW;
        if (_lanes[laneColumn, playerLaneRow] != null)
        {
            throw new InvalidOperationException($"Attempted to play card in occupied lane {laneColumn}!");
        }

        _lanes[laneColumn, playerLaneRow] = card;
    }

    public bool TryRemoveCardById(int cardId)
    {
        for (int col = 0; col < COL_COUNT; col++)
        {
            for (int row = 0; row < ROW_COUNT; row++)
            {
                if (_lanes[col, row]?.Id == cardId)
                {
                    _lanes[col, row] = null;
                    return true;
                }
            }
        }

        return false;
    }

    public void PromoteStagedCards()
    {
        for (int col = 0; col < COL_COUNT; col++)
        {
            var stagedCard = _lanes[col, ENEMY_STAGE_LANE_ROW];
            var activeCard = _lanes[col, ENEMY_LANE_ROW];
            if (activeCard == null && stagedCard != null)
            {
                _lanes[col, ENEMY_LANE_ROW] = stagedCard;
                _lanes[col, ENEMY_STAGE_LANE_ROW] = null;
            }
        }
    }
}

public class PlayerTurnAction
{
    public DrawAction DrawAction { get; set; }
    public PlayCardAction[] CardActions { get; set; }
    public int? HeuristicScore { get; set; }

    public static List<PlayerTurnAction> TakeTop(int count, int minValue, params IEnumerable<PlayerTurnAction>[] actionParams)
    {
        var top = new List<PlayerTurnAction>(count);
        foreach (IEnumerable<PlayerTurnAction> actions in actionParams)
        {
            foreach (PlayerTurnAction action in actions)
            {
                if (action.HeuristicScore >= minValue)
                {
                    AddIfInTopN(top, count, action);
                }
            }
        }

        return top;
    }

    public static void AddIfInTopN(List<PlayerTurnAction> top, int n, PlayerTurnAction action)
    {
        if (top.Count < n)
        {
            top.Add(action);
        }
        else if (action.HeuristicScore > top[n - 1].HeuristicScore)
        {
            top[n - 1] = action;
            for (int i = top.Count - 1; i > 0; i--)
            {
                if (top[i - 1].HeuristicScore < top[i].HeuristicScore)
                {
                    (top[i - 1], top[i]) = (top[i], top[i - 1]);
                }
            }
        }
    }
}

public class PlayCardAction
{
    public SimulatorCard PlayedCard { get; set; }
    public int LaneColumnIndex { get; set; }
    public List<SimulatorCard> SacrificeCards { get; set; }
}

public enum DrawAction
{
    NoDraw,
    DrawFromCreatures,
    DrawFromSacrifices,
}

public class SimulatorLogger : IDisposable
{
    private FileAccess _file;
    private const string FILENAME = "simulation.log";

    public SimulatorLogger(bool enableLogging = true)
    {
        if (enableLogging)
        {
            DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
            _file = FileAccess.Open($"{Constants.UserDataDirectory}/{FILENAME}", FileAccess.ModeFlags.Write);
        }
    }

    public void Log(string line)
    {
        _file?.StoreLine(line);
    }

    public void LogHeader(string header)
    {
        Log($" --- {header.PadRight(24  )} ---------------------------------");
    }

    public void LogActions(IEnumerable<PlayerTurnAction> actions)
    {
        int actionId = 0;
        foreach (PlayerTurnAction action in actions)
        {
            Log($"[Action {actionId}] {action.DrawAction}");
            foreach (var cardAction in action.CardActions)
            {
                Log($"   [Action {actionId}] Play {cardAction.PlayedCard.Card.Name} in lane {cardAction.LaneColumnIndex}");
                Log($"   [Action {actionId}] Sacrifices: {string.Join(", ", cardAction.SacrificeCards.Select(card => card.Card.Name))}");
            }

            actionId++;
        }
    }

    public void LogHand(SimulatorHand hand)
    {
        Log($"[Hand] {string.Join(", ", hand.Cards.Select(card => $"{card.Card.Name}[id: {card.Id} cost: {(int)card.Card.BloodCost}]"))}");
    }

     public void LogDeck(SimulatorDeck deck, string name)
    {
        int RemainingCardCount = deck.RemainingCardCount;
        if (RemainingCardCount > 0)
        {
            var nextCard = deck.PeekTop();
            Log($"[{name}] Next Card: {nextCard?.Card.Name ?? "NULL"} ({RemainingCardCount} remaining)");
        }
        else
        {
            Log($"[{name}] 0 cards remaining");
        }
    }

    public void LogLanes(SimulatorLanes lanes)
    {
        const int MAX_CARD_NAME_LENGTH = 32;

        var str = new StringBuilder();
        LogHeader("GAME BOARD");
        for (int row = SimulatorLanes.ROW_COUNT - 1; row >= 0; row--)
        {
            // Row 1: Card Name
            for (int col = 0; col < SimulatorLanes.COL_COUNT; col++)
            {
                if (col != 0) str.Append(" | ");

                string cardName = lanes.GetCardAt(row, col)?.Card.Name ?? "_Empty_";
                cardName = cardName.Substr(0, MAX_CARD_NAME_LENGTH);
                str.Append(cardName.PadRight(MAX_CARD_NAME_LENGTH));
            }
            str.AppendLine();

            // Row 2: Attack / Health
            for (int col = 0; col < SimulatorLanes.COL_COUNT; col++)
            {
                if (col != 0) str.Append(" | ");

                SimulatorCard card = lanes.GetCardAt(row, col);
                string cardDamageStr = card != null ? $"[⚔️ {card.Card.Attack}; 💖: {card.Card.Health - card.DamageReceived}]" : "";
                str.Append(cardDamageStr.PadRight(MAX_CARD_NAME_LENGTH));
            }
            str.AppendLine();

            // Row 3: Abilities
            for (int col = 0; col < SimulatorLanes.COL_COUNT; col++)
            {
                if (col != 0) str.Append(" | ");

                SimulatorCard card = lanes.GetCardAt(row, col);
                string cardDamageStr = card != null && card.Card.Abilities.Count > 0 ? $"[{string.Join(",", card.Card.Abilities)}]" : "";
                str.Append(cardDamageStr.PadRight(MAX_CARD_NAME_LENGTH));
            }
            str.AppendLine();
        }
        Log(str.ToString());
    }

    public void LogRoundResult(SimulatorRoundResult result)
    {
        LogHeader("ROUND RESULT");
        Log($"[Result] Turns: {result.Turns}");
        Log($"[Result] Result: {result.Result}");
        Log($"[Result] Player Damage: {result.PlayerDamageReceived}");
        Log($"[Result] Enemy Damage: {result.EnemyDamageReceived}");
    }

    public void Dispose()
    {
        _file?.Close();
    }
}
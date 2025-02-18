public enum LevelDifficulty
{
    Easy,
    Medium,
    Hard,
    FailedGuardrail,
}

public enum LevelReward
{
    AddResource,
    AddCreature,
    AddUncommonCreature,
    AddRareCreature,
    RemoveCard,
    IncreaseHandSize,
}

public class GameLevel
{
    public int Level { get; set; }

    public int Seed { get; set; }

    public EnemyAI AI { get; set; }

    public LevelDifficulty Difficulty { get; set; }
    public string GuardrailReason { get; set; }

    public LevelReward Reward { get; set; }
}
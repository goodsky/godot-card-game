namespace CardGame.Core;

public enum CostType
{
    UnitSacrifice = 0,
    Bones = 1,
}

public class ResourceCost
{
    public CostType Type { get; set; }
    public int Amount { get; set; }
}
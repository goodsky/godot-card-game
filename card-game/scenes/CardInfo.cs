using System.Text.Json.Serialization;

public enum CardBloodCost
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
}

public enum CardRarity
{
    Sacrifice = 0,
    Common = 1,
    Uncommon = 2,
    Rare = 3,
}

public enum CardAbilities
{
    None = 0,
    Agile = 1,
    Guard = 2,
}

public struct CardInfo
{
    [JsonIgnore]
    public readonly string Name => $"{NameAdjective} {NameNoun}";

    [JsonPropertyName("name_noun")]
    public string NameNoun { get; set; }

    [JsonPropertyName("name_adj")]
    public string NameAdjective { get; set; }

    [JsonPropertyName("avatar")]
    public string AvatarResource { get; set; }

    [JsonPropertyName("foil")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string CardFoilHexcode { get; set; }

    [JsonPropertyName("attack")]
    public int Attack { get; set; }

    [JsonPropertyName("health")]
    public int Health { get; set; }

    [JsonPropertyName("abilities")]
    public CardAbilities[] Abilities { get; set; }

    [JsonPropertyName("cost")]
    public CardBloodCost BloodCost { get; set; }

    [JsonPropertyName("rarity")]
    public CardRarity Rarity { get; set; }
}
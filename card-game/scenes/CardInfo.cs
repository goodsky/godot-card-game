using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardBloodCost
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardRarity
{
    Sacrifice = 0,
    Common = 1,
    Uncommon = 2,
    Rare = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardAbilities
{
    None = 0,
    Flying = 1,
    Tall = 2,
    Lethal = 3,
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
    public List<CardAbilities> Abilities { get; set; }

    [JsonPropertyName("cost")]
    public CardBloodCost BloodCost { get; set; }

    [JsonPropertyName("rarity")]
    public CardRarity Rarity { get; set; }
}

public static class CardInfoExtensions
{
    public static string GetCardSummary(this CardInfo cardInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[center][font_size=16]{cardInfo.Name}[/font_size][/center]");
        sb.AppendLine($"[b]Rarity:[/b] {cardInfo.Rarity}");
        sb.AppendLine($"[b]Cost:[/b] {cardInfo.BloodCost} Sacrifice{((int)cardInfo.BloodCost > 1 ? "s" : string.Empty)}");
        sb.AppendLine($"[b]Attack:[/b] [color=red]{cardInfo.Attack}[/color]\t[b]Health:[/b] [color=blue]{cardInfo.Health}[/color]");
        if (cardInfo.Abilities != null && cardInfo.Abilities.Count > 0)
        {
            var cardData = CardGenerator.LoadGeneratorData();
            var abilityTooltips = cardData.Stats.AbilityTooltips;

            sb.AppendLine(string.Empty);
            foreach (var ability in cardInfo.Abilities)
            {
                if (abilityTooltips.TryGetValue(ability, out var tooltip))
                {
                    sb.AppendLine($"[u]{tooltip.Label}[/u]: {tooltip.Description}");
                }
                else
                {
                    sb.AppendLine($"[u]{ability}[/u]");
                }
            }
        }

        return sb.ToString();
    }
}
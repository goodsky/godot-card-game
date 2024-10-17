using Xunit.Sdk;

[assembly: TestFramework("CardGame.Tests.DisableGodotLogging", "card-game-tests")]


namespace CardGame.Tests
{
    public sealed class DisableGodotLogging : XunitTestFramework
    {
        public DisableGodotLogging(IMessageSink messageSink) : base(messageSink)
        {
            Log.SkipAllLogging = true;
        }
    }

    public static class TestUtils
    {
        private static CardBloodCost[] CardCosts = new[] {
                CardBloodCost.Zero,
                CardBloodCost.One,
                CardBloodCost.Two,
                CardBloodCost.Three,
            };

        private static CardRarity[] CardRarities = new[] {
                CardRarity.Sacrifice,
                CardRarity.Common,
                CardRarity.Uncommon,
                CardRarity.Rare,
            };

        public static IEnumerable<CardInfo> GenerateCardInfo(int n = 1)
        {
            int id = 0;
            foreach (CardBloodCost cost in CardCosts)
            {
                foreach (CardRarity rarity in CardRarities)
                {
                    for (int i = 0; i < n; i++)
                    {
                        yield return new CardInfo
                        {
                            Name = $"{rarity}_{cost}_{id}",
                            Attack = 0,
                            Health = 1,
                            BloodCost = cost,
                            Rarity = rarity,
                            Id = id++,
                        };
                    }
                }
            }
        }
    }
}
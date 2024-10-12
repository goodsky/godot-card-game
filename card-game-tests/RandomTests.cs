using Godot;

namespace CardGame.Tests;

public class RandomTests
{
    private readonly ITestOutputHelper _output;

    public RandomTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(0.20f, 0.40f)]
    [InlineData(-10f, 100f)]
    public void RandomRangef(float min, float max)
    {
        var rnd = new RandomGenerator();
        var values = new List<float>();
        for (int i = 0; i < 100; i++)
        {
            float value = rnd.Nextf(min, max);
            values.Add(value);
            Assert.True(min <= value && value < max);
        }
        _output.WriteLine("({0:0.00}, {1:0.00}): [{2}]", min, max, string.Join(",", values.Select(v => $"{v:0.00}")));
    }

    [Fact]
    public void SeededRandomRangef()
    {
        int seed = 947681303;
        var expected = new float[] {
            0.19718514f,
            0.65953195f,
            0.3651696f,
            0.13151458f,
            0.35287306f,
            0.68467087f,
            0.8945172f,
            0.99659747f,
            0.82881033f,
            0.969978f
        };

        var rnd = new RandomGenerator(seed);
        for (int i = 0; i < 10; i++)
        {
            float value = rnd.Nextf(0f, 1f);
            Assert.Equal(expected[i], value);
        }
    }

    [Fact]
    public void LoadRandomStateFromSeedAndN()
    {
        int seed = 2011124;
        int targetN = 10;

        var rnd1 = new RandomGenerator(seed);
        var values1 = new List<object>();
        for (int n = 0; n < targetN; n++)
        {
            if (n % 2 == 0)
            {
                values1.Add(rnd1.Nextf(0f, 1f));
            }
            else
            {
                values1.Add(rnd1.Next(10));
            }
        }

        _output.WriteLine($"Rnd1: [{string.Join(",", values1)}]");
        Assert.Equal(targetN, rnd1.N);

        var rnd2 = new RandomGenerator(seed);
        var values2 = new List<object>();
        for (int n = 0; n < targetN; n++)
        {
            values2.Add(rnd2.Next(10));
        }

        _output.WriteLine($"Rnd2: [{string.Join(",", values2)}]");
        Assert.Equal(targetN, rnd2.N);
        
        var rnd3 = new RandomGenerator(seed, targetN);
        var value3 = rnd3.Nextf(0f, 1f);

        Assert.Equal(value3, rnd1.Nextf(0f, 1f));
        Assert.Equal(value3, rnd2.Nextf(0f, 1f));

        for (int nextN = 0; nextN < targetN; nextN++)
        {
            var val1 = rnd1.Nextf(-1f, 1f);
            var val2 = rnd2.Nextf(-1f, 1f);
            var val3 = rnd3.Nextf(-1f, 1f);
            _output.WriteLine("{0}, {1}, {2}", val1, val2, val3);
            Assert.Equal(val1, val2);
            Assert.Equal(val2, val3);
        }
    }
}
namespace CardGame.Tests;

public class GameLobbyTests
{
    private readonly ITestOutputHelper _output;

    public GameLobbyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void LinearScaleRangeTest1()
    {
        float rate = 1;
        int min = 0, max = 37;
        int x_intercept = 0, y_intercept = 0;

        var values = new List<string>();
        for (int i = -10; i < 50; i++)
        {
            int value = GameLobby.LinearScale(i, rate, min, max, x_intercept, y_intercept);
            Assert.True(min <= value && value <= max);
            values.Add($"{i}={value}");
        }

        _output.WriteLine("rate: {0}; x_intercept: {1}; y_intercept: {2}; min: {3}; max: {4}", rate, x_intercept, y_intercept, min, max);
        _output.WriteLine("[{0}]", string.Join("; ", values));
    }

    [Fact]
    public void LinearScaleRangeTest2()
    {
        float rate = 0.5f;
        int min = 5, max = 12;
        int x_intercept = 5, y_intercept = 0;

        var values = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            float valuef = GameLobby.LinearScalef(i, rate, min, max, x_intercept, y_intercept);
            int valuei = GameLobby.LinearScale(i, rate, min, max, x_intercept, y_intercept);
            Assert.True(min <= valuef && valuef <= max);
            Assert.True(min <= valuei && valuei <= max);
            values.Add($"{i}={valuef:0.00}/{valuei}");
        }

        _output.WriteLine("rate: {0}; x_intercept: {1}; y_intercept: {2}; min: {3}; max: {4}", rate, x_intercept, y_intercept, min, max);
        _output.WriteLine("[{0}]", string.Join("; ", values));
    }

    [Fact]
    public void LinearScaleRangeTest3()
    {
        float rate = 0.01f;
        float min = 0.0f, max = 1.0f;
        float x_intercept = 0.0f, y_intercept = 0.20f;

        var values = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            float value = GameLobby.LinearScalef(i, rate, min, max, x_intercept, y_intercept);
            Assert.True(min <= value && value <= max);
            values.Add($"{i}={value:0.00}");
        }

        _output.WriteLine("rate: {0}; x_intercept: {1}; y_intercept: {2}; min: {3}; max: {4}", rate, x_intercept, y_intercept, min, max);
        _output.WriteLine("[{0}]", string.Join("; ", values));
    }
}
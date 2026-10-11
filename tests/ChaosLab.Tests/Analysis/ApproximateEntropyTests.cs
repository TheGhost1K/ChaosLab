using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class ApproximateEntropyTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void PeriodicSeries_LowValue()
    {
        var series = new double[2000];
        for (int i = 0; i < series.Length; i++)
            series[i] = Math.Sin(i * 0.1);

        double apEn = ApproximateEntropy.Compute(series, 2, 0.2);
        apEn.Should().BeLessThan(0.5);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void WhiteNoise_HighValue()
    {
        var rng = new Random(42);
        var series = new double[2000];
        for (int i = 0; i < series.Length; i++) series[i] = rng.NextDouble();

        double apEn = ApproximateEntropy.Compute(series, 2, 0.2);
        apEn.Should().BeGreaterThan(1.5);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void ConstantSeries_ReturnsNaN()
    {
        var series = new double[200];
        Array.Fill(series, 1.0);

        double apEn = ApproximateEntropy.Compute(series);
        double.IsNaN(apEn).Should().BeTrue();
    }
}

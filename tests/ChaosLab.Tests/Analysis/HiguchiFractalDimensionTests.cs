using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class HiguchiFractalDimensionTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void WhiteNoise_ApproxTwo()
    {
        var rng = new Random(42);
        var series = new double[2000];
        for (int i = 0; i < series.Length; i++) series[i] = rng.NextDouble();

        double d = HiguchiFractalDimension.Compute(series, kMax: 20);
        d.Should().BeApproximately(2.0, 0.15);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void PeriodicSeries_ApproxOne()
    {
        var series = new double[2000];
        for (int i = 0; i < series.Length; i++) series[i] = Math.Sin(i * 0.1);

        double d = HiguchiFractalDimension.Compute(series, kMax: 20);
        d.Should().BeApproximately(1.0, 0.15);
    }
}

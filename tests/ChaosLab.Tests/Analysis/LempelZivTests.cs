using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class LempelZivTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void ConstantSeries_LowComplexity()
    {
        var series = new double[2000];
        Array.Fill(series, 1.0);

        double c = LempelZivComplexity.Compute(series);
        c.Should().BeLessThan(0.3);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void WhiteNoise_HighComplexity()
    {
        var rng = new Random(42);
        var series = new double[2000];
        for (int i = 0; i < series.Length; i++) series[i] = rng.NextDouble();

        double c = LempelZivComplexity.Compute(series);
        c.Should().BeGreaterThan(0.5);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void ResultAlwaysInUnitInterval()
    {
        var rng = new Random(7);
        for (int trial = 0; trial < 10; trial++)
        {
            var series = new double[1000];
            for (int i = 0; i < series.Length; i++) series[i] = rng.NextDouble();

            double c = LempelZivComplexity.Compute(series);
            c.Should().BeInRange(0.0, 1.0);
        }
    }
}

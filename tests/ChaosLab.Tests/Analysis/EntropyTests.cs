using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class EntropyTests
{
    [Fact]
    public void SampleEntropy_ConstantSeries_ReturnsNaN()
    {
        // SampEn требует ненулевой дисперсии
        var series = new double[200];
        Array.Fill(series, 1.0);

        double se = EntropyMeasures.SampleEntropy(series, 2, 0.2);
        se.Should().Be(double.NaN);
    }

    [Fact]
    public void SampleEntropy_WhiteNoise_HighEntropy()
    {
        var rng = new Random(42);
        var series = new double[2000];
        for (int i = 0; i < series.Length; i++)
            series[i] = rng.NextDouble();

        double se = EntropyMeasures.SampleEntropy(series, 2, 0.2);

        // Для белого шума SampEn ≈ 2.2
        se.Should().BeGreaterThan(1.5);
    }

    [Fact]
    public void SampleEntropy_PeriodicSeries_LowEntropy()
    {
        var series = new double[2000];
        for (int i = 0; i < series.Length; i++)
            series[i] = Math.Sin(i * 0.1);

        double se = EntropyMeasures.SampleEntropy(series, 2, 0.2);

        // Для периодики SampEn низкое
        se.Should().BeLessThan(0.5);
    }

    [Fact]
    public void PermutationEntropy_MonotonicSeries_IsZero()
    {
        var series = new double[100];
        for (int i = 0; i < series.Length; i++) series[i] = i;

        double pe = EntropyMeasures.PermutationEntropy(series, order: 3);
        pe.Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void PermutationEntropy_WhiteNoise_NearOne()
    {
        var rng = new Random(42);
        var series = new double[5000];
        for (int i = 0; i < series.Length; i++)
            series[i] = rng.NextDouble();

        double pe = EntropyMeasures.PermutationEntropy(series, order: 3);
        pe.Should().BeGreaterThan(0.95).And.BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void PermutationEntropy_IsAlwaysIn01()
    {
        var rng = new Random(7);
        var series = new double[1000];
        for (int i = 0; i < series.Length; i++) series[i] = rng.NextDouble();

        for (int m = 2; m <= 5; m++)
        {
            double pe = EntropyMeasures.PermutationEntropy(series, order: m);
            pe.Should().BeInRange(0.0, 1.0);
        }
    }
}

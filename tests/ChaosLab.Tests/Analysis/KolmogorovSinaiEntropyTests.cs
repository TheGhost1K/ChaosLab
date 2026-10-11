using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class KolmogorovSinaiEntropyTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void AllNegative_ReturnsZero()
    {
        var spec = new[] { -0.5, -1.0, -2.0 };
        KolmogorovSinaiEntropy.Compute(spec).Should().Be(0.0);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void OnePositive_SumsPositive()
    {
        var spec = new[] { 0.9, 0.0, -14.5 };
        KolmogorovSinaiEntropy.Compute(spec).Should().BeApproximately(0.9, 1e-9);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void TwoPositive_SumsBoth()
    {
        var spec = new[] { 0.9, 0.5, -3.0 };
        KolmogorovSinaiEntropy.Compute(spec).Should().BeApproximately(1.4, 1e-9);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void CountsPositiveExponents()
    {
        var spec = new[] { 0.9, 0.5, 0.2, -1.0 };
        KolmogorovSinaiEntropy.PositiveExponentCount(spec).Should().Be(3);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void EmptySpectrum_ReturnsNaN()
    {
        KolmogorovSinaiEntropy.Compute(Array.Empty<double>()).Should().Be(double.NaN);
    }
}

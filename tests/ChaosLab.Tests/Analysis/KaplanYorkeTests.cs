using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class KaplanYorkeTests
{
    [Fact]
    public void TwoPositive_ReturnsIntegerPartPlusFraction()
    {
        // [2, 0, -3]: j=2, сумма=2, D_KY = 2 + 2/3 ≈ 2.667
        var spec = new[] { 2.0, 0.0, -3.0 };
        KaplanYorke.Compute(spec).Should().BeApproximately(2.0 + 2.0 / 3.0, 1e-9);
    }

    [Fact]
    public void OnePositive_ReturnsOnePlusFraction()
    {
        // [1, 0, -2]: j=2 (сумма=1 ≥ 0), D_KY = 2 + 1/2 = 2.5
        var spec = new[] { 1.0, 0.0, -2.0 };
        KaplanYorke.Compute(spec).Should().BeApproximately(2.5, 1e-9);
    }

    [Fact]
    public void AllNegative_ReturnsZero()
    {
        var spec = new[] { -0.5, -0.5, -0.5 };
        KaplanYorke.Compute(spec).Should().Be(0.0);
    }

    [Fact]
    public void EmptySpectrum_ReturnsNaN()
    {
        KaplanYorke.Compute(Array.Empty<double>()).Should().Be(double.NaN);
    }
}

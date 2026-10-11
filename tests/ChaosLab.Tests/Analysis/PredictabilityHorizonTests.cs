using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class PredictabilityHorizonTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Lorenz_HorizonInExpectedRange()
    {
        // Классика: λ₁ = 0.906, Δ = 40, ε₀ = 10⁻³ → t_h ≈ 11.7
        var res = PredictabilityHorizon.Estimate(0.906, 40.0, 1e-3);
        res.Horizon.Should().BeInRange(5.0, 20.0);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void NegativeLambda_ZeroHorizon()
    {
        var res = PredictabilityHorizon.Estimate(-0.5, 10.0);
        res.Horizon.Should().Be(0);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void ZeroEpsilon_ZeroHorizon()
    {
        var res = PredictabilityHorizon.Estimate(0.9, 40.0, epsilon0: 0);
        res.Horizon.Should().Be(0);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void SmallerEpsilon_LongerHorizon()
    {
        var coarse = PredictabilityHorizon.Estimate(0.9, 40.0, 1e-2);
        var fine = PredictabilityHorizon.Estimate(0.9, 40.0, 1e-4);
        fine.Horizon.Should().BeGreaterThan(coarse.Horizon);
    }
}

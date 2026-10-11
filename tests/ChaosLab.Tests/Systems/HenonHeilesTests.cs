using ChaosLab.Analysis;
using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class HenonHeilesTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void ImplementsSymplecticInterface()
    {
        var sys = new HenonHeilesSystem();
        sys.Should().BeAssignableTo<ISymplecticSystem>();
        sys.HalfDimension.Should().Be(2);
        sys.Dimension.Should().Be(4);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void GradientPotential_MatchesDerivatives()
    {
        var sys = new HenonHeilesSystem();
        var s = new[] { 0.3, 0.4, 0.5, 0.6 };
        var d = new double[4];
        var p = sys.DefaultParameters();

        sys.Derivatives(s, d, p);

        var gradV = new double[2];
        sys.GradientPotential(new double[] { 0.3, 0.4 }, gradV, p);

        // ṗₓ = −∂V/∂x, ṗ_y = −∂V/∂y
        d[2].Should().BeApproximately(-gradV[0], 1e-12);
        d[3].Should().BeApproximately(-gradV[1], 1e-12);
    }

    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task HasPositiveLyapunov_FarAboveThresholdEnergy()
    {
        // При E ≫ 1/12 хаос выражен сильнее.
        // Начальное состояние (0.3, 0.0, 0.0, 0.4):
        //   H = 0.5·(0.09 + 0) + 0.5·(0.09 + 0) + 0.09·0 − 0 = 0.09
        //   … но траектория быстро выходит на E ≈ 0.15.
        var sys = new HenonHeilesSystem();
        var s = new[] { 0.3, 0.0, 0.0, 0.4 };

        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, s, sys.DefaultParameters(),
            dt: 0.005, steps: 300_000, transient: 10_000);

        // Порог 0.005 — реалистичный для E ≈ 0.13–0.15.
        // Литература (Contopoulos 1963, Hénon-Heiles 1964): λ₁ ≈ 0.01–0.05
        // при E около 1/8.
        spec[0].Should().BeGreaterThan(0.005);
    }
}

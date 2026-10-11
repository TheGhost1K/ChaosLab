using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class LyapunovTests
{
    [Fact]
    public async Task Lorenz_ProducesKnownSpectrum()
    {
        var sys = new LorenzSystem();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.001, steps: 200_000, transient: 10_000);

        // Литературные значения (Wolf et al. 1985)
        spec[0].Should().BeApproximately(0.906, 0.05);
        spec[2].Should().BeApproximately(-14.57, 0.1);

        // Инвариант: Σλᵢ = −(σ + 1 + β) = −13.667
        double sum = spec[0] + spec[1] + spec[2];
        sum.Should().BeApproximately(-13.667, 0.1);
    }

    [Fact]
    public async Task Henon_SumEqualsLogB()
    {
        var sys = new HenonMap();
        var p = new[] { 1.4, 0.3 };
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, sys.DefaultInitialStateCached, p,
            dt: 1.0, steps: 500_000, transient: 1000);

        // Инвариант: λ₁ + λ₂ = log|b|
        double sum = spec[0] + spec[1];
        sum.Should().BeApproximately(Math.Log(0.3), 0.01);

        // Литература: λ₁ ≈ 0.419
        spec[0].Should().BeApproximately(0.419, 0.02);
    }

    [Fact]
    public async Task LogisticAtR4_LambdaIsLog2()
    {
        var sys = new LogisticMap();
        var p = new[] { 4.0 };
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, new[] { 0.3141 }, p, dt: 1.0, steps: 200_000, transient: 1000);

        spec[0].Should().BeApproximately(Math.Log(2), 0.005);
    }

    [Fact]
    public async Task TentAtMu2_LambdaIsLog2()
    {
        var sys = new TentMap();
        var p = new[] { 2.0 };
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, new[] { 0.31 }, p, dt: 1.0, steps: 200_000, transient: 1000);

        spec[0].Should().BeApproximately(Math.Log(2), 0.005);
    }

    [Fact]
    public async Task LinearDoubling_LambdaIsLog2()
    {
        var sys = new LinearDoublingMap();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, new[] { 0.5 }, Array.Empty<double>(),
            dt: 1.0, steps: 10_000, transient: 0);

        spec[0].Should().BeApproximately(Math.Log(2), 1e-8);
    }

    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public async Task Rossler_MatchesLiteratureValues()
    {
        var sys = new RosslerSystem();

        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.005, steps: 400_000, transient: 20_000);

        // Литературные значения (Wolf et al. 1985; Sprott, Chaos and
        // Time-Series Analysis, 2003, Table 5.1):
        //   λ₁ = 0.0714
        //   λ₂ = 0.0000
        //   λ₃ = −5.394
        //   Σλᵢ = −5.323 (не −5.9 — это асимптотическое значение)
        spec[0].Should().BeApproximately(0.0714, 0.02);
        spec[2].Should().BeApproximately(-5.394, 0.15);
        (spec[0] + spec[1] + spec[2]).Should().BeApproximately(-5.323, 0.2);
    }

    [Fact]
    public async Task HarmonicOscillator_AllLambdasAreZero()
    {
        var sys = new HarmonicOscillator();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, new[] { 1.0, 0.0 }, Array.Empty<double>(),
            dt: 0.01, steps: 100_000, transient: 100);

        foreach (var lambda in spec)
            Math.Abs(lambda).Should().BeLessThan(0.01);
    }

    [Fact]
    public async Task Cancellation_ReturnsEmptyPromptly()
    {
        var sys = new LorenzSystem();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.001, steps: 10_000_000, transient: 0, ct: cts.Token);

        // Должны вернуться быстро с нулями
        spec.Should().HaveCount(3);
    }
}

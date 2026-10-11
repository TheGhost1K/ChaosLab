using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class ArnoldCatMapTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task LambdaIsLogGoldenRatioSquared()
    {
        // Аналитически: λ₁ = log((3 + √5)/2) ≈ 0.9624
        var sys = new ArnoldCatMap();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, new[] { 0.1, 0.2 }, new[] { 1.0 },
            dt: 1.0, steps: 50_000, transient: 100);

        double expected = Math.Log((3 + Math.Sqrt(5)) / 2);
        spec[0].Should().BeApproximately(expected, 0.01);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task SumOfLambdas_IsZero()
    {
        // Сохраняет площадь → λ₁ + λ₂ = 0
        var sys = new ArnoldCatMap();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, new[] { 0.1, 0.2 }, new[] { 1.0 },
            dt: 1.0, steps: 50_000, transient: 100);

        (spec[0] + spec[1]).Should().BeApproximately(0, 0.01);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Next_StaysOnTorus()
    {
        var sys = new ArnoldCatMap();
        var s = new[] { 0.7, 0.8 };
        var n = new double[2];
        var p = new[] { 1.0 };

        for (int i = 0; i < 100; i++)
        {
            sys.Next(s, n, p);
            Array.Copy(n, s, 2);

            s[0].Should().BeInRange(0.0, 1.0);
            s[1].Should().BeInRange(0.0, 1.0);
        }
    }
}

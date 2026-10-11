using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class CircleMapTests
{
    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task ChaosAtLargeK()
    {
        // K > 1 → необратимое отображение → хаос в областях между языками
        var sys = new CircleMap();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, new[] { 0.1 }, new[] { 0.6065, 1.5 },
            dt: 1.0, steps: 100_000, transient: 1000);

        spec[0].Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task NoChaosAtSmallK()
    {
        // K < 1 → только периодические и квазипериодические орбиты
        var sys = new CircleMap();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, new[] { 0.1 }, new[] { 0.3, 0.5 },
            dt: 1.0, steps: 100_000, transient: 1000);

        spec[0].Should().BeLessThanOrEqualTo(0.001);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Next_StaysOnUnitCircle()
    {
        var sys = new CircleMap();
        var s = new[] { 0.5 };
        var n = new double[1];
        var p = new[] { 0.6065, 1.5 };

        for (int i = 0; i < 100; i++)
        {
            sys.Next(s, n, p);
            s[0] = n[0];
            s[0].Should().BeInRange(0.0, 1.0);
        }
    }
}

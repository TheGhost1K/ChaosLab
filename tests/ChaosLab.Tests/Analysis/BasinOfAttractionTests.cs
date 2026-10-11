using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class BasinOfAttractionTests
{
    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public async Task Lorenz_ProducesNonEmptyBasin()
    {
        var sys = new LorenzSystem();
        var res = await BasinOfAttraction.ComputeAsync(
            sys, sys.DefaultParameters(),
            xLo: -10, xHi: 10, yLo: -10, yHi: 10,
            width: 16, height: 16, dt: 0.01,
            transient: 500, minSamples: 50);

        res.Width.Should().Be(16);
        res.Height.Should().Be(16);
        res.Labels.Length.Should().Be(256);
        res.AttractorCount.Should().BeGreaterThan(0);

        // Все метки в диапазоне [0, AttractorCount)
        foreach (var lbl in res.Labels)
        {
            lbl.Should().BeInRange(0, res.AttractorCount - 1);
        }
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task DiscreteSystem_ReturnsEmpty()
    {
        var sys = new LogisticMap();
        var res = await BasinOfAttraction.ComputeAsync(
            sys, new[] { 3.9 },
            xLo: 0, xHi: 1, yLo: 0, yHi: 1,
            width: 10, height: 10, dt: 1.0);

        res.AttractorCount.Should().Be(0);
    }
}

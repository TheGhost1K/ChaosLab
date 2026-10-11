using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class PoincareSectionTests
{
    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public async Task Lorenz_ProducesFiniteNumberOfPoints()
    {
        var sys = new LorenzSystem();
        var res = await PoincareSection.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.005, zPlane: 27.0, sign: +1,
            maxPoints: 500, transient: 2000);

        res.Count.Should().BeGreaterThan(20);
        res.X.Should().HaveCount(res.Count);
        res.Y.Should().HaveCount(res.Count);

        // Все точки — конечные числа
        foreach (var v in res.X) double.IsFinite(v).Should().BeTrue();
        foreach (var v in res.Y) double.IsFinite(v).Should().BeTrue();
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task DiscreteSystem_ReturnsEmpty()
    {
        var sys = new LogisticMap();
        var res = await PoincareSection.ComputeAsync(
            sys, new[] { 0.3 }, new[] { 3.9 },
            dt: 1.0, zPlane: 0.5, maxPoints: 100);

        res.Count.Should().Be(0);
    }
}

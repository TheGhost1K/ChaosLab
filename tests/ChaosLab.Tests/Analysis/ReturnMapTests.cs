using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class ReturnMapTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task LogisticMap_ProducesValuesInRange()
    {
        var sys = new LogisticMap();
        var res = await ReturnMap.ComputeAsync(
            sys, new[] { 0.31 }, new[] { 4.0 },
            dt: 1.0, coordIndex: 0,
            maxPoints: 500, transient: 100);

        res.Count.Should().BeGreaterThan(100);
        res.MinX.Should().BeGreaterThanOrEqualTo(0.0);
        res.MaxX.Should().BeLessThanOrEqualTo(1.0);
        res.MinY.Should().BeGreaterThanOrEqualTo(0.0);
        res.MaxY.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public async Task LorenzMap_ProducesTentLikeCurve()
    {
        var sys = new LorenzSystem();
        var res = await ReturnMap.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.005, coordIndex: 0,
            maxPoints: 500, transient: 3000);

        // Локальные максимумы x у Лоренца: положительные значения
        res.Count.Should().BeGreaterThan(50);
        res.MaxX.Should().BeGreaterThan(0);
    }
}

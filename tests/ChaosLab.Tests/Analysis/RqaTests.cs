using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class RqaTests
{
    [Fact]
    public async Task Lorenz_RqaMetricsInValidRange()
    {
        var sys = new LorenzSystem();
        var res = await RecurrenceAnalysis.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, N: 400, epsFactor: 0.1, embeddingDim: 3, delay: 8,
            transient: 2000);

        res.RecurrenceRate.Should().BeInRange(0.0, 1.0);
        res.Determinism.Should().BeInRange(0.0, 1.0);
        res.Laminarity.Should().BeInRange(0.0, 1.0);
        res.Matrix.GetLength(0).Should().Be(400);
    }

    [Fact]
    public async Task PeriodicOrbit_HighDeterminism()
    {
        // Синус — периодический ряд — DET должен быть высоким
        var sys = new LogisticMap();
        // r = 3.5 → устойчивый 4-цикл
        var res = await RecurrenceAnalysis.ComputeAsync(
            sys, new[] { 0.1 }, new[] { 3.5 },
            dt: 1.0, N: 200, epsFactor: 0.05, embeddingDim: 2, delay: 1,
            transient: 5000);

        res.Determinism.Should().BeGreaterThan(0.9);
    }
}

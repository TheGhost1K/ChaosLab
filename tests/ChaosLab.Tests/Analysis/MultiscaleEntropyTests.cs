using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class MultiscaleEntropyTests
{
    [Fact]
    public async Task Lorenz_ReturnsFullCurve()
    {
        var sys = new LorenzSystem();
        var res = await MultiscaleEntropy.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, maxScale: 8, sampleCount: 2000, transient: 2000);

        res.Cancelled.Should().BeFalse();
        res.Scales.Should().HaveCount(8);
        res.SampleEntropies.Should().HaveCount(8);

        // Хотя бы одна точка должна быть валидной
        res.SampleEntropies.Any(s => !double.IsNaN(s)).Should().BeTrue();
    }

    [Fact]
    public async Task Cancellation_ReturnsCancelledFlag()
    {
        var sys = new LorenzSystem();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var res = await MultiscaleEntropy.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, maxScale: 20, sampleCount: 20_000,
            transient: 2000, ct: cts.Token);

        res.Cancelled.Should().BeTrue();
    }
}

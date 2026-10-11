using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class MultifractalTests
{
    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public async Task Lorenz_ProducesValidDq()
    {
        var sys = new LorenzSystem();
        var res = await Multifractal.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.02, sampleCount: 2000,
            embeddingDim: 3, delay: 10,
            transient: 3000);

        res.Q.Length.Should().Be(21);
        res.Dq.Length.Should().Be(21);

        // D_0 (q=0, индекс 10) — box-counting размерность ≈ 2
        res.Dq[10].Should().BeApproximately(2.0, 0.4);

        // Общий тренд: D_{-5} ≥ D_{+5}
        if (!double.IsNaN(res.Dq[0]) && !double.IsNaN(res.Dq[20]))
            res.Dq[0].Should().BeGreaterThanOrEqualTo(res.Dq[20] - 0.3);
    }
}

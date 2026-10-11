using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class EmbeddingDiagnosticsTests
{
    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task MutualInformation_DecreasesWithLag()
    {
        var sys = new LorenzSystem();
        var series = await EntropyMeasures.CollectSeriesAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, count: 2000, transient: 1000);

        var (tau, mi) = EmbeddingDiagnostics.MutualInformation(series, maxTau: 40);

        tau.Length.Should().Be(41);
        mi.Length.Should().Be(41);

        // MI(0) = 0, MI(1) > 0, MI(∞) → 0
        mi[0].Should().BeApproximately(0.0, 1e-9);
        mi[1].Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task Fnn_DecreasesWithDimension()
    {
        var sys = new LorenzSystem();
        var series = await EntropyMeasures.CollectSeriesAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.02, count: 1500, transient: 1000);

        var (dims, ratios) = EmbeddingDiagnostics.FalseNearestNeighbors(
            series, maxDim: 6, delay: 10);

        dims.Length.Should().Be(5);      // maxDim − 1
        ratios.Length.Should().Be(5);

        // Для Лоренца FNN должно упасть к нулю при m ≈ 3
        ratios[^1].Should().BeLessThan(ratios[0]);
    }
}

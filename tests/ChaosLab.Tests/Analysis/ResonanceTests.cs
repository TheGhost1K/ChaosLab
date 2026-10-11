using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class ResonanceTests
{
    [Fact]
    public async Task HarmonicOscillator_HasDiscretePeak()
    {
        var sys = new HarmonicOscillator();
        var res = await ResonanceAnalysis.ComputeAsync(
            sys, new[] { 1.0, 0.0 }, Array.Empty<double>(),
            dt: 0.01, nSamples: 8192, transient: 1000, topPeaks: 4);

        res.PeakFrequencies.Should().NotBeEmpty();
        res.FundamentalFrequency.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task Lorenz_HasNoDominantDiscreteTone()
    {
        var sys = new LorenzSystem();
        var res = await ResonanceAnalysis.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.005, nSamples: 8192, transient: 2000, topPeaks: 6);

        // Не проверяем число резонансов — оно нестабильно. Проверяем,
        // что пиков либо нет, либо мощность равномерна (нет доминирующей частоты).
        if (res.PeakPowers.Length >= 2)
        {
            double ratio = res.PeakPowers[0] / res.PeakPowers[1];
            // Для хаоса доминирующий пик не должен сильно выделяться
            ratio.Should().BeLessThan(10);
        }
    }
}

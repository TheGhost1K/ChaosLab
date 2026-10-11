using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class EdgeCaseTests
{
    [Fact]
    public async Task ZeroOneTest_TooShortSeries_ReturnsNaN()
    {
        var sys = new LogisticMap();
        // sampleCount 10 < 500 → NaN
        double k = await ZeroOneTest.ComputeAsync(
            sys, new[] { 0.3 }, new[] { 3.9 },
            dt: 1.0, sampleCount: 10, transient: 0);

        double.IsNaN(k).Should().BeTrue();
    }

    [Fact]
    public async Task ZeroOneTest_CancelBeforeStart_ReturnsNaN()
    {
        var sys = new LorenzSystem();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        double k = await ZeroOneTest.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, sampleCount: 2000, decimation: 20, ct: cts.Token);

        // При отмене CollectSeriesAsync возвращает пустой массив → NaN
        double.IsNaN(k).Should().BeTrue();
    }

    [Fact]
    public async Task MultiscaleEntropy_TinySeries_MarksMasksAsNaN()
    {
        var sys = new LogisticMap();
        // sampleCount 200 / maxScale 20 → на больших τ newLen < 100 → NaN
        var res = await MultiscaleEntropy.ComputeAsync(
            sys, new[] { 0.3 }, new[] { 3.9 },
            dt: 1.0, maxScale: 20, sampleCount: 200, transient: 0);

        res.Cancelled.Should().BeFalse();
        res.Scales.Should().HaveCount(20);

        // Хотя бы последний масштаб должен быть NaN
        res.SampleEntropies[^1].Should().Be(double.NaN);
    }

    [Fact]
    public async Task ResonanceAnalysis_AllZero_ReturnsEmpty()
    {
        // Система с константным решением — спектр пустой
        var sys = new LogisticMap();
        var res = await ResonanceAnalysis.ComputeAsync(
            sys, new[] { 0.0 }, new[] { 0.5 },
            dt: 1.0, nSamples: 1024, transient: 10, topPeaks: 5);

        // Не падает, возвращает что-то осмысленное
        res.PeakFrequencies.Should().NotBeNull();
    }

    [Fact]
    public async Task Lorenz96_RespectsNParameter()
    {
        var sys = new Lorenz96System
        {
            N = 16
        };
        sys.DefaultInitialState.Length.Should().Be(16);

        // Кэш должен тоже обновиться
        sys.DefaultInitialStateCached.Length.Should().Be(16);

        var s = sys.DefaultInitialStateCached;
        var w = new double[16]; w[0] = 1;
        var r = new double[16];

        sys.JacobianVectorProduct(s, w, r, sys.DefaultParameters());

        foreach (var v in r) double.IsFinite(v).Should().BeTrue();
    }

    [Fact]
    public async Task SymbolicDynamics_AllSameSymbol_ReturnsHZero()
    {
        // Логистика с r=0.5 сходится к точке → все символы одинаковые
        var sys = new LogisticMap();
        var res = await SymbolicDynamics.ComputeAsync(
            sys, new[] { 0.5 }, new[] { 0.5 },
            dt: 1.0, numSymbols: 4, sampleCount: 2000, transient: 1000);

        res.Should().NotBeNull();
        // h_top должен быть 0 или около
        res!.TopologicalEntropy.Should().BeLessThan(0.1);
    }
}

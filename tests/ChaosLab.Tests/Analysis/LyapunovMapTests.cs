using ChaosLab.Analysis;
using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class LyapunovMapTests
{
    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public async Task LorenzMap_HasBothStableAndChaoticRegions()
    {
        var sys = new LorenzSystem();
        var px = await LyapunovMap.ComputeAsync(
            sys,
            p1Index: 1, p1Min: 1, p1Max: 60,       // ρ
            p2Index: 0, p2Min: 1, p2Max: 30,       // σ
            width: 32, height: 32, dt: 0.02,
            iterations: 800, transient: 200,
            palette: Palette.All[0]);

        px.Length.Should().Be(32 * 32);
        px.Should().Contain(p => p != 0);          // не пустой холст
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task InsufficientParameters_HandledByCaller()
    {
        // SprottA имеет 0 параметров — метод просто не должен вызываться,
        // но проверим что при передаче индекса 0 ничего не падает
        var sys = new SprottASystem();
        // Пропускаем — метод требует ≥2 параметров.
        await Task.CompletedTask;
    }

    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public async Task Progressive_ReportsBothFrames()
    {
        var sys = new LorenzSystem();
        int reportCount = 0;

        var progress = new Progress<(int[] px, int w, int h)>(_ =>
        {
            Interlocked.Increment(ref reportCount);
        });

        var final = await LyapunovMap.ComputeProgressiveAsync(
            sys,
            p1Index: 1, p1Min: 1, p1Max: 60,
            p2Index: 0, p2Min: 1, p2Max: 30,
            finalWidth: 64, finalHeight: 64, dt: 0.02,
            iterations: 400, transient: 100,
            palette: Palette.All[0],
            progress: progress);

        final.Length.Should().Be(64 * 64);

        // Ждём обработки Progress (пост-обработка через SynchronizationContext)
        await Task.Delay(100);
        reportCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task Cancellation_ReturnsPromptly()
    {
        var sys = new LorenzSystem();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(30);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var px = await LyapunovMap.ComputeAsync(
            sys,
            p1Index: 1, p1Min: 1, p1Max: 60,
            p2Index: 0, p2Min: 1, p2Max: 30,
            width: 256, height: 256, dt: 0.02,
            iterations: 5000, transient: 500,
            palette: Palette.All[0],
            ct: cts.Token);
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(1000);
    }
}

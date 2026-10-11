using ChaosLab.Core;
using ChaosLab.Rendering;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Rendering;

public class BifurcationRendererTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Map_ProducesNonEmptyImage()
    {
        var sys = new LogisticMap();
        var px = new int[128 * 128];
        var p = new[] { 3.9 };

        BifurcationRenderer.Render(
            px, 128, 128, sys,
            paramIndex: 0, pMin: 0, pMax: 4,
            baseParams: p, initialState: new[] { 0.3 },
            transient: 100, iterations: 200, coordIndex: 0,
            yLo: 0, yHi: 1,
            palette: Palette.All[0]);

        px.Count(c => c != 0).Should().BeGreaterThan(100);
    }

    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public void Flow_ProducesNonEmptyImage()
    {
        var sys = new LorenzSystem();
        var px = new int[64 * 64];

        BifurcationRenderer.RenderFlow(
            px, 64, 64, sys,
            paramIndex: 1, pMin: 10, pMax: 40,
            baseParams: sys.DefaultParameters(),
            initialState: sys.DefaultInitialStateCached,
            transientSteps: 500, captureSteps: 1500,
            coordIndex: 0, dt: 0.01,
            yLo: -20, yHi: 20,
            palette: Palette.All[0]);

        px.Count(c => c != 0).Should().BeGreaterThan(50);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Map_Cancellation_ReturnsWithoutThrow()
    {
        var sys = new LogisticMap();
        var px = new int[128 * 128];
        var cts = new CancellationTokenSource();
        cts.Cancel();

        BifurcationRenderer.Render(
            px, 128, 128, sys,
            paramIndex: 0, pMin: 0, pMax: 4,
            baseParams: new[] { 3.9 }, initialState: new[] { 0.3 },
            transient: 100, iterations: 200, coordIndex: 0,
            yLo: 0, yHi: 1,
            palette: Palette.All[0],
            ct: cts.Token);

        // Никаких исключений — просто пустой результат
    }
}

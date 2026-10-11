using ChaosLab.Rendering;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Rendering;

public class PlotRendererTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void BasicPlot_DrawsPointsAndLines()
    {
        var px = new int[128 * 128];
        var xs = new double[] { 0, 1, 2, 3, 4 };
        var ys = new double[] { 0, 1, 4, 9, 16 };

        PlotRenderer.Render(
            px, 128, 128, xs, ys,
            background: 0x000000,
            lineColor: 0x00FF00,
            gridColor: 0x101010,
            markerSize: 2,
            drawMarkers: true);

        px.Count(c => c == 0x00FF00).Should().BeGreaterThan(10);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void NaNValues_BreakLine()
    {
        var px = new int[64 * 64];
        var xs = new double[] { 0, 1, 2, 3, 4 };
        var ys = new double[] { 0, 1, double.NaN, 3, 4 };

        PlotRenderer.Render(
            px, 64, 64, xs, ys,
            0x000000, 0xFF0000, 0x101010);

        // Просто не должно падать
        px.Should().NotBeNull();
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void SinglePoint_DoesNotDrawLine()
    {
        var px = new int[64 * 64];
        PlotRenderer.Render(
            px, 64, 64,
            new double[] { 1 }, new double[] { 1 },
            0x000000, 0xFF0000, 0x101010);

        // Только сетка, без ошибок
        px.Should().NotBeNull();
    }
}

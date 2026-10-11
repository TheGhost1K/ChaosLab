using ChaosLab.Core;
using ChaosLab.Rendering;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Rendering;

public class CobwebRendererTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Logistic_ProducesDiagonalAndFunction()
    {
        var sys = new LogisticMap();
        var px = new int[128 * 128];

        CobwebRenderer.Render(
            px, 128, 128, sys,
            p: new[] { 3.9 },
            xMin: 0, xMax: 1, x0: 0.3,
            iterations: 30,
            palette: Palette.All[0]);

        // Диагональ + функция + ломаная = много ненулевых пикселей
        px.Count(c => c != 0).Should().BeGreaterThan(500);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Non1DSystem_ProducesEmptyImage()
    {
        var sys = new HenonMap();
        var px = new int[64 * 64];

        CobwebRenderer.Render(
            px, 64, 64, sys,
            p: new[] { 1.4, 0.3 },
            xMin: -1, xMax: 1, x0: 0,
            iterations: 10,
            palette: Palette.All[0]);

        px.Should().AllBeEquivalentTo(0);
    }
}

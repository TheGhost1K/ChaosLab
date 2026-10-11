using ChaosLab.Core;
using ChaosLab.Rendering;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Rendering;

public class VectorFieldRendererTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void HenonMap_DrawsArrows()
    {
        var sys = new HenonMap();
        var px = new int[128 * 128];
        var cam = new Camera3D
        {
            Yaw = 0,
            Pitch = 0,
            Zoom = 50,
            CenterX = 0,
            CenterY = 0,
            CenterZ = 0,
            ScreenW = 128,
            ScreenH = 128,
        };

        VectorFieldRenderer.RenderOver(
            px, 128, 128, sys,
            p: new[] { 1.4, 0.3 },
            cam: cam,
            xMin: -1.5, xMax: 1.5, yMin: -1.0, yMax: 1.0,
            arrowsX: 12, arrowsY: 12);

        px.Count(c => c != 0).Should().BeGreaterThan(50);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void NonMap_DoesNothing()
    {
        var sys = new LorenzSystem();   // не дискретная
        var px = new int[64 * 64];
        var cam = new Camera3D { Zoom = 10, ScreenW = 64, ScreenH = 64 };

        VectorFieldRenderer.RenderOver(
            px, 64, 64, sys,
            p: sys.DefaultParameters(), cam: cam,
            xMin: -10, xMax: 10, yMin: -10, yMax: 10);

        px.Should().AllBeEquivalentTo(0);
    }
}

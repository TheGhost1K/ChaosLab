using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class Camera3DTests
{
    [Fact]
    public void Project_OriginCentered_ReturnsScreenCenter()
    {
        var cam = new Camera3D
        {
            Yaw = 0,
            Pitch = 0,
            Zoom = 10,
            CenterX = 0,
            CenterY = 0,
            CenterZ = 0,
            ScreenW = 800,
            ScreenH = 600,
        };

        var (x, y) = cam.Project(0, 0, 0);
        x.Should().Be(400);
        y.Should().Be(300);
    }

    [Fact]
    public void Project_ScalesWithZoom()
    {
        var cam = new Camera3D
        {
            Yaw = 0,
            Pitch = 0,
            Zoom = 10,
            ScreenW = 800,
            ScreenH = 600,
        };

        var (x1, _) = cam.Project(1, 0, 0);
        cam.Zoom = 20;
        var (x2, _) = cam.Project(1, 0, 0);

        (x2 - 400).Should().Be(2 * (x1 - 400));
    }

    [Fact]
    public void Project_Yaw90_SwapsXYAxes()
    {
        var cam = new Camera3D
        {
            Yaw = Math.PI / 2,
            Pitch = 0,
            Zoom = 10,
            ScreenW = 800,
            ScreenH = 600,
        };

        // При yaw=π/2 точка (1,0) должна проецироваться около x=400 (без учёта
        // округления). Project возвращает int, поэтому используем Be с допуском 1.
        var (x, _) = cam.Project(1, 0, 0);
        Math.Abs(x - 400).Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void Clone_ProducesIndependentCopy()
    {
        var cam = new Camera3D { Yaw = 1.0, Zoom = 10 };
        var clone = cam.Clone();
        clone.Yaw = 2.0;

        cam.Yaw.Should().Be(1.0);
        clone.Yaw.Should().Be(2.0);
    }
}

using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class Camera3DExtraTests
{
    [Fact]
    public void CopyFrom_CopiesAllState()
    {
        var src = new Camera3D
        {
            Yaw = 1.2,
            Pitch = -0.4,
            Zoom = 25,
            CenterX = 3,
            CenterY = -2,
            CenterZ = 5,
            ScreenW = 800,
            ScreenH = 600,
        };
        var dst = new Camera3D();

        dst.CopyFrom(src);

        dst.Yaw.Should().Be(1.2);
        dst.Pitch.Should().Be(-0.4);
        dst.Zoom.Should().Be(25);
        dst.CenterX.Should().Be(3);
        dst.CenterY.Should().Be(-2);
        dst.CenterZ.Should().Be(5);
        dst.ScreenW.Should().Be(800);
        dst.ScreenH.Should().Be(600);
    }

    [Fact]
    public void Clone_DoesNotShareState()
    {
        var a = new Camera3D { Yaw = 1.0, Zoom = 10, ScreenW = 800, ScreenH = 600 };
        var b = a.Clone();

        b.Yaw = 2.0;
        b.CenterX = 5;

        a.Yaw.Should().Be(1.0);
        a.CenterX.Should().Be(0);
    }
}

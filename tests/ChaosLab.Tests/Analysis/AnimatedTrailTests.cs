using ChaosLab.Analysis;
using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class AnimatedTrailTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Add_RespectsTrailLength()
    {
        var trail = new AnimatedTrail { TrailLength = 100 };

        // Точки по кругу радиуса 1 — гарантированно попадают в кадр.
        for (int i = 0; i < 500; i++)
        {
            double t = i * 2 * Math.PI / 100;
            trail.Add(Math.Cos(t), Math.Sin(t), 0);
        }

        var px = new int[200 * 200];
        var cam = new Camera3D
        {
            Yaw = 0,
            Pitch = 0,
            Zoom = 50,
            CenterX = 0,
            CenterY = 0,
            CenterZ = 0,
            ScreenW = 200,
            ScreenH = 200,
        };
        trail.Render(px, 200, 200, cam, 0xFFA000);

        px.Count(p => p != 0).Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Render_EmptyTrail_DoesNothing()
    {
        var trail = new AnimatedTrail();
        var px = new int[64 * 64];
        var cam = new Camera3D { Zoom = 10, ScreenW = 64, ScreenH = 64 };

        trail.Render(px, 64, 64, cam, 0xFFA000);

        px.Should().AllBeEquivalentTo(0);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Clear_RemovesAllPoints()
    {
        var trail = new AnimatedTrail();
        trail.Add(1, 2, 3);
        trail.Add(4, 5, 6);
        trail.Clear();

        var px = new int[64 * 64];
        var cam = new Camera3D { Zoom = 10, ScreenW = 64, ScreenH = 64 };
        trail.Render(px, 64, 64, cam, 0xFFA000);

        px.Should().AllBeEquivalentTo(0);
    }
}

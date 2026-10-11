using ChaosLab.Analysis;
using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class AnimatedTrailExtraTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void TrailWithLorenz_ProducesPoints()
    {
        var trail = new AnimatedTrail { TrailLength = 500 };
        var sys = new LorenzSystem();
        var state = (double[]) sys.DefaultInitialStateCached.Clone();
        var scratch = new double[sys.Dimension];
        var rk = new Rk4Scratch(sys.Dimension);
        var p = sys.DefaultParameters();

        var xyz = new double[3];
        for (int i = 0; i < 3000; i++)
        {
            Integrator.Rk4Step(sys, state, p, 0.005, rk);
            sys.Project3D(state, xyz);
            trail.Add(xyz[0], xyz[1], xyz[2]);
        }

        // Рендерим и проверяем, что есть ненулевые пиксели
        var px = new int[256 * 256];
        var cam = new Camera3D
        {
            Yaw = 0.4,
            Pitch = -0.35,
            Zoom = 6,
            CenterX = 0,
            CenterY = 0,
            CenterZ = 25,
            ScreenW = 256,
            ScreenH = 256,
        };
        trail.Render(px, 256, 256, cam, 0x57A9FF);

        px.Count(p => p != 0).Should().BeGreaterThan(0);
    }
}

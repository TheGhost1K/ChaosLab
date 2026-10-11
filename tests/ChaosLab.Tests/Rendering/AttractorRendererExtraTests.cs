using ChaosLab.Core;
using ChaosLab.Rendering;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Rendering;

public class AttractorRendererExtraTests
{
    private static (AttractorRenderer, Camera3D) Setup(int size)
    {
        var r = new AttractorRenderer(size, size);
        var cam = new Camera3D
        {
            Yaw = 0.4,
            Pitch = -0.35,
            Zoom = 8,
            CenterX = 0,
            CenterY = 0,
            CenterZ = 25,
            ScreenW = size,
            ScreenH = size,
        };
        return (r, cam);
    }

    [Fact]
    public void Colorize_WithGamma_AppliesPowerLaw()
    {
        var (r, cam) = Setup(128);
        var sys = new LorenzSystem();
        var state = (double[]) sys.DefaultInitialStateCached.Clone();
        var p = sys.DefaultParameters();

        r.Accumulate(sys, state, p, 0.005, 5000, cam);
        r.Colorize(Palette.All[0], gamma: 2.0);

        r.Pixels.Any(px => px != 0).Should().BeTrue();
    }

    [Fact]
    public void Colorize_EmptyDensity_FillsBackground()
    {
        var r = new AttractorRenderer(64, 64);
        r.Colorize(Palette.All[0], gamma: 1.0, background: 0x123456);

        r.Pixels.Should().AllBeEquivalentTo(0x123456);
    }

    [Fact]
    public void Accumulate_1DDiscrete_PlotsPoints()
    {
        var r = new AttractorRenderer(128, 128);
        var sys = new LogisticMap();
        var state = new[] { 0.3 };
        var p = new[] { 3.9 };
        var cam = new Camera3D
        {
            Zoom = 100,
            CenterX = 0.5,
            CenterY = 0.5,
            ScreenW = 128,
            ScreenH = 128,
        };

        r.Accumulate(sys, state, p, dt: 1.0, steps: 2000, cam, warmup: 100);

        r.Density.Any(d => d > 0).Should().BeTrue();
    }

    [Fact]
    public void Accumulate_2DMap_PlotsPoints()
    {
        var r = new AttractorRenderer(128, 128);
        var sys = new HenonMap();
        var state = (double[]) sys.DefaultInitialStateCached.Clone();
        var p = sys.DefaultParameters();
        var cam = new Camera3D
        {
            Zoom = 40,
            CenterX = 0,
            CenterY = 0,
            ScreenW = 128,
            ScreenH = 128,
        };

        r.Accumulate(sys, state, p, dt: 1.0, steps: 2000, cam, warmup: 100);

        r.Density.Any(d => d > 0).Should().BeTrue();
    }

    [Fact]
    public void DrawRqaMatrix_EmptyMatrix_DoesNothing()
    {
        var r = new AttractorRenderer(64, 64);
        var matrix = new int[0, 0];

        r.DrawRqaMatrix(matrix, 0, Palette.All[0]);

        r.Pixels.Should().AllBeEquivalentTo(0);
    }

    [Fact]
    public void DrawRqaMatrix_FullDiagonal()
    {
        var r = new AttractorRenderer(64, 64);
        var matrix = new int[32, 32];
        for (int i = 0; i < 32; i++) matrix[i, i] = 1;

        r.DrawRqaMatrix(matrix, 32, Palette.All[0]);

        r.Pixels.Any(px => px != 0).Should().BeTrue();
    }

    [Fact]
    public void DrawBasins_EmptyResult_DoesNothing()
    {
        var r = new AttractorRenderer(64, 64);
        var basins = new ChaosLab.Analysis.BasinResult
        {
            Width = 0,
            Height = 0,
            Labels = Array.Empty<int>(),
            AttractorCount = 0,
        };

        r.DrawBasins(basins, Palette.All[0]);

        r.Pixels.Should().AllBeEquivalentTo(0);
    }

    [Fact]
    public void DrawBasins_TwoAttractors_ProducesColors()
    {
        var r = new AttractorRenderer(64, 64);
        var labels = new int[16 * 16];
        for (int i = 0; i < labels.Length; i++)
            labels[i] = i % 2;

        var basins = new ChaosLab.Analysis.BasinResult
        {
            Width = 16,
            Height = 16,
            Labels = labels,
            AttractorCount = 2,
        };

        r.DrawBasins(basins, Palette.All[0]);

        r.Pixels.Any(px => px != 0).Should().BeTrue();
    }

    [Fact]
    public void DrawPoincare_EmptyResult_DoesNothing()
    {
        var r = new AttractorRenderer(64, 64);
        var poincare = new ChaosLab.Analysis.PoincareResult
        {
            X = Array.Empty<double>(),
            Y = Array.Empty<double>(),
            Count = 0,
        };
        var cam = new Camera3D { Zoom = 10, ScreenW = 64, ScreenH = 64 };

        r.DrawPoincare(poincare, cam, Palette.All[0]);

        r.Pixels.Should().AllBeEquivalentTo(0);
    }
}

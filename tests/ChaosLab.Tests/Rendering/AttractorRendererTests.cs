using ChaosLab.Core;
using ChaosLab.Rendering;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Rendering;

public class AttractorRendererTests
{
    [Fact]
    public void NewRenderer_IsEmpty()
    {
        var r = new AttractorRenderer(64, 64);
        r.Density.Should().AllBeEquivalentTo((ushort) 0);
        r.Pixels.Should().AllBeEquivalentTo(0);
    }

    [Fact]
    public void Accumulate_AddsPointsToDensity()
    {
        var r = new AttractorRenderer(128, 128);
        var sys = new LorenzSystem();
        var state = (double[]) sys.DefaultInitialStateCached.Clone();
        var p = sys.DefaultParameters();
        var cam = new Camera3D
        {
            Yaw = 0.4,
            Pitch = -0.35,
            Zoom = 8,
            CenterX = 0,
            CenterY = 0,
            CenterZ = 25,
            ScreenW = 256,
            ScreenH = 256,   // или 512 в зависимости от размера
        };

        r.Accumulate(sys, state, p, dt: 0.005, steps: 10_000, cam);

        int nonZero = 0;
        for (int i = 0; i < r.Density.Length; i++)
            if (r.Density[i] > 0) nonZero++;

        nonZero.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Colorize_ProducesNonZeroPixels()
    {
        // Холст должен быть достаточно большим, чтобы поймать аттрактор,
        // либо zoom — достаточно маленьким.
        var r = new AttractorRenderer(512, 512);
        var sys = new LorenzSystem();
        var state = (double[]) sys.DefaultInitialStateCached.Clone();
        var p = sys.DefaultParameters();

        // Lorenz: x∈[−20,20], z∈[0,50]. При zoom=8 центр = 256:
        // проекция x*8 + 256 ∈ [96, 416] — внутри [0, 512].
        var cam = new Camera3D
        {
            Yaw = 0.4,
            Pitch = -0.35,
            Zoom = 8,
            CenterX = 0,
            CenterY = 0,
            CenterZ = 25,
            ScreenW = 512,
            ScreenH = 512,
        };

        r.Accumulate(sys, state, p, dt: 0.005, steps: 20_000, cam);
        r.Colorize(Palette.All[0], gamma: 0.85);

        r.Pixels.Any(px => px != 0).Should().BeTrue();
    }

    [Fact]
    public void Clear_ResetsDensity()
    {
        var r = new AttractorRenderer(32, 32);
        var sys = new LorenzSystem();
        var state = (double[]) sys.DefaultInitialStateCached.Clone();
        var p = sys.DefaultParameters();
        var cam = new Camera3D
        {
            Yaw = 0.4,
            Pitch = -0.35,
            Zoom = 8,
            CenterX = 0,
            CenterY = 0,
            CenterZ = 25,
            ScreenW = 256,
            ScreenH = 256,   // или 512 в зависимости от размера
        };

        r.Accumulate(sys, state, p, 0.005, 1000, cam);
        r.Clear();

        r.Density.Should().AllBeEquivalentTo((ushort) 0);
        r.Pixels.Should().AllBeEquivalentTo(0);
    }

    [Fact]
    public void Accumulate_Cancellation_ReturnsPromptly()
    {
        var r = new AttractorRenderer(512, 512);
        var sys = new LorenzSystem();
        var state = (double[]) sys.DefaultInitialStateCached.Clone();
        var p = sys.DefaultParameters();
        var cam = new Camera3D { Zoom = 20, ScreenW = 512, ScreenH = 512 };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(20);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        r.Accumulate(sys, state, p, 0.005, 100_000_000, cam, ct: cts.Token);
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(500);
    }
}

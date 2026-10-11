using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class GrayScottTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Constructor_AllocatesArrays()
    {
        var field = new GrayScottField();
        field.U.Length.Should().Be(field.GridW * field.GridH);
        field.V.Length.Should().Be(field.GridW * field.GridH);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Reset_FillsUAndClearsV()
    {
        var field = new GrayScottField();
        field.Reset();

        // u = 1 везде, кроме затравок; v = 0 везде, кроме затравок
        double uSum = 0, vSum = 0;
        for (int i = 0; i < field.U.Length; i++)
        {
            uSum += field.U[i];
            vSum += field.V[i];
        }
        uSum.Should().BeGreaterThan(0);
        vSum.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public void Step_PreservesFiniteValues()
    {
        var field = new GrayScottField();
        field.Reset();
        field.Step(100, field.DefaultParameters());

        foreach (var v in field.U) double.IsFinite(v).Should().BeTrue();
        foreach (var v in field.V) double.IsFinite(v).Should().BeTrue();
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Step_Cancellation_ReturnsPromptly()
    {
        var field = new GrayScottField();
        field.Reset();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        field.Step(1000, field.DefaultParameters(), cts.Token);
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void RenderTo_ProducesNonEmptyImage()
    {
        var field = new GrayScottField();
        field.Reset();
        field.Step(50, field.DefaultParameters());

        var px = new int[128 * 128];
        field.RenderTo(px, 128, 128, Palette.All[0]);

        px.Count(c => c != 0).Should().BeGreaterThan(0);
    }
}

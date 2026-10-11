using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class BoxCountingTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void EmptyDensity_ReturnsNaN()
    {
        var density = new ushort[64 * 64];
        double d = BoxCounting.Compute(density, 64, 64);
        d.Should().Be(double.NaN);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void FullDensity_ReturnsDimensionTwo()
    {
        // Заполнены все пиксели → размерность = 2
        var density = new ushort[128 * 128];
        Array.Fill(density, (ushort) 1);

        double d = BoxCounting.Compute(density, 128, 128);
        d.Should().BeApproximately(2.0, 0.05);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void LineDensity_ReturnsDimensionOne()
    {
        // Горизонтальная линия → размерность 1
        const int w = 256, h = 256;
        var density = new ushort[w * h];
        for (int x = 0; x < w; x++) density[128 * w + x] = 1;

        double d = BoxCounting.Compute(density, w, h);
        d.Should().BeApproximately(1.0, 0.05);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void SinglePixel_ReturnsZero()
    {
        var density = new ushort[64 * 64];
        density[32 * 64 + 32] = 1;

        double d = BoxCounting.Compute(density, 64, 64);
        d.Should().BeApproximately(0.0, 0.05);
    }
}

using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class PaletteTests
{
    [Fact]
    public void Sample_IsDeterministic()
    {
        var p = Palette.All[0];
        var a = p.Sample(0.5);
        var b = p.Sample(0.5);
        a.Should().Be(b);
    }

    [Fact]
    public void Sample_HandlesOutOfRange_ByClamping()
    {
        var p = Palette.All[0];
        p.Sample(-1.0).Should().Be(p.Sample(0.0));
        p.Sample(2.0).Should().Be(p.Sample(1.0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Sample_ReturnsValidBgr32(double t)
    {
        foreach (var p in Palette.All)
        {
            int c = p.Sample(t);
            int r = (c >> 16) & 0xFF;
            int g = (c >> 8) & 0xFF;
            int b = c & 0xFF;
            r.Should().BeInRange(0, 255);
            g.Should().BeInRange(0, 255);
            b.Should().BeInRange(0, 255);
        }
    }

    [Fact]
    public void ByName_ReturnsMatchingPalette()
    {
        var inferno = Palette.ByName("Inferno");
        inferno.Name.Should().Be("Inferno");
    }

    [Fact]
    public void ByName_FallsBackToFirst_WhenNotFound()
    {
        var p = Palette.ByName("NonexistentName");
        p.Should().BeSameAs(Palette.All[0]);
    }
}

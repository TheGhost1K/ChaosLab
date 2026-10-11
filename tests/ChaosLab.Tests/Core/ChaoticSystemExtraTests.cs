using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class ChaoticSystemExtraTests
{
    [Fact]
    public void ToString_ReturnsName()
    {
        var sys = new LorenzSystem();
        sys.ToString().Should().Be("Lorenz");
    }

    [Fact]
    public void Project3D_Default_ReturnsFirstThreeComponents()
    {
        var sys = new SprottASystem();
        var s = new double[] { 1, 2, 3 };
        var xyz = new double[3];

        sys.Project3D(s, xyz);

        xyz[0].Should().Be(1);
        xyz[1].Should().Be(2);
        xyz[2].Should().Be(3);
    }

    [Fact]
    public void Project3D_For1DSystem_UsesZeros()
    {
        var sys = new LogisticMap();
        var s = new double[] { 0.7 };
        var xyz = new double[3];

        sys.Project3D(s, xyz);

        xyz[0].Should().Be(0.7);
        xyz[1].Should().Be(0);
        xyz[2].Should().Be(0);
    }

    [Fact]
    public void Project3D_Lorenz96_ReturnsFirstThree()
    {
        var sys = new Lorenz96System();
        sys.N = 8;
        var s = new double[8];
        for (int i = 0; i < 8; i++) s[i] = i + 1;
        var xyz = new double[3];

        sys.Project3D(s, xyz);

        xyz[0].Should().Be(1);
        xyz[1].Should().Be(2);
        xyz[2].Should().Be(3);
    }

    [Fact]
    public void DefaultInitialStateCached_ReturnsSameReference()
    {
        var sys = new LorenzSystem();
        var a = sys.DefaultInitialStateCached;
        var b = sys.DefaultInitialStateCached;

        a.Should().BeSameAs(b);
    }
}

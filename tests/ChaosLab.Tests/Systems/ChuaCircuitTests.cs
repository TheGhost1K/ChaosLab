using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class ChuaCircuitTests
{
    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task HasPositiveLyapunov()
    {
        var sys = new ChuaCircuitSystem();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, steps: 100_000, transient: 5000);

        spec[0].Should().BeGreaterThan(0.1);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Nonlinearity_IsPiecewiseLinear()
    {
        var sys = new ChuaCircuitSystem();
        var p = sys.DefaultParameters();
        var s = new double[] { 0.0, 0.0, 0.0 };
        var d = new double[3];

        sys.Derivatives(s, d, p);
        // При x = 0: g(0) = 0, ẋ = α(y − x − 0) = 0, ẏ = 0, ż = 0
        d[0].Should().Be(0);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Jvp_ProducesFiniteOutput()
    {
        var sys = new ChuaCircuitSystem();
        var s = sys.DefaultInitialStateCached;
        var w = new double[] { 1, 0.5, 0.3 };
        var r = new double[3];

        sys.JacobianVectorProduct(s, w, r, sys.DefaultParameters());

        foreach (var v in r) double.IsFinite(v).Should().BeTrue();
    }
}

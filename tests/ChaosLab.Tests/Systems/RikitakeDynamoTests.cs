using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class RikitakeDynamoTests
{
    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task HasPositiveLyapunov()
    {
        var sys = new RikitakeDynamoSystem();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, steps: 100_000, transient: 5000);

        spec[0].Should().BeGreaterThan(0.05);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Jvp_MatchesFormula()
    {
        var sys = new RikitakeDynamoSystem();
        var s = new double[] { 1, 2, 3 };
        var w = new double[] { 1, 0, 0 };
        var r = new double[3];
        var p = new[] { 1.0, 5.0 };

        sys.JacobianVectorProduct(s, w, r, p);

        // J = [[-μ, z, y], [z-a, -μ, x], [-y, -x, 0]]
        r[0].Should().BeApproximately(-1.0, 1e-9);
        r[1].Should().BeApproximately(-2.0, 1e-9);   // z-a = 3-5 = -2
        r[2].Should().BeApproximately(-2.0, 1e-9);   // -y = -2
    }
}

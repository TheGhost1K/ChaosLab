using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class LorenzTests
{
    [Fact]
    public void Derivatives_MatchAnalyticalFormula()
    {
        var sys = new LorenzSystem();
        var s = new[] { 1.0, 2.0, 3.0 };
        var d = new double[3];
        var p = new[] { 10.0, 28.0, 8.0 / 3.0 };

        sys.Derivatives(s, d, p);

        d[0].Should().BeApproximately(10.0 * (2.0 - 1.0), 1e-12);
        d[1].Should().BeApproximately(1.0 * (28.0 - 3.0) - 2.0, 1e-12);
        d[2].Should().BeApproximately(1.0 * 2.0 - (8.0 / 3.0) * 3.0, 1e-12);
    }

    [Fact]
    public void Jvp_MatchesNumericalJacobian()
    {
        var sys = new LorenzSystem();
        var s = new[] { 1.0, 2.0, 3.0 };
        var w = new[] { 1.0, 0.5, -0.3 };
        var p = new[] { 10.0, 28.0, 8.0 / 3.0 };

        var analytic = new double[3];
        sys.JacobianVectorProduct(s, w, analytic, p);

        // Численная аппроксимация через базовый класс
        var numerical = new double[3];
        var baseSys = new TestLorenzWithoutJvp();
        baseSys.JacobianVectorProduct(s, w, numerical, p);

        for (int i = 0; i < 3; i++)
            analytic[i].Should().BeApproximately(numerical[i], 1e-4);
    }

    private sealed class TestLorenzWithoutJvp : ChaosLab.Core.ChaoticSystem
    {
        public override string Name => "Lorenz";
        public override string Category => "Flow";
        public override int Dimension => 3;
        public override double[] DefaultInitialState => new[] { 0.1, 0, 0 };
        public override ChaosLab.Core.ParameterDef[] Parameters => Array.Empty<ChaosLab.Core.ParameterDef>();

        public override void Derivatives(double[] s, double[] d, double[] p)
        {
            double sigma = 10, rho = 28, beta = 8.0 / 3.0;
            d[0] = sigma * (s[1] - s[0]);
            d[1] = s[0] * (rho - s[2]) - s[1];
            d[2] = s[0] * s[1] - beta * s[2];
        }
    }
}

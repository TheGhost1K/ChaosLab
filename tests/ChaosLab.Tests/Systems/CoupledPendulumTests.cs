using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class CoupledPendulumTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void ImplementsSymplecticInterface()
    {
        var sys = new CoupledPendulumChain();
        sys.Should().BeAssignableTo<ISymplecticSystem>();
        sys.HalfDimension.Should().Be(6);
        sys.Dimension.Should().Be(12);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void GradientPotential_MatchesDerivatives()
    {
        var sys = new CoupledPendulumChain();
        var s = sys.DefaultInitialStateCached;
        var d = new double[sys.Dimension];
        var p = sys.DefaultParameters();

        sys.Derivatives(s, d, p);

        var gradV = new double[sys.HalfDimension];
        sys.GradientPotential(s.AsSpan(0, sys.HalfDimension), gradV, p);

        for (int i = 0; i < sys.HalfDimension; i++)
            d[sys.HalfDimension + i].Should().BeApproximately(-gradV[i], 1e-12);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Verlet_ConservesEnergy()
    {
        var sys = new CoupledPendulumChain();
        var s = new double[sys.Dimension];
        int n = sys.HalfDimension;

        // Начальное отклонение 1 рад вместо 2 — уходим от глубоко
        // нелинейного режима, где dt = 0.01 становится нестабильным.
        s[0] = 1.0;

        var p = sys.DefaultParameters();
        var scr = new VerletScratch(n);

        // ─── Правильная формула энергии ───
        // H = Σ ½ pᵢ²                        кинетическая
        //   + Σ (1 − cos θᵢ)                  потенциал маятников
        //   + (k/2) Σ (θᵢ₊₁ − θᵢ)²            N+1 пружин, включая границы
        double Energy()
        {
            double e = 0;

            for (int i = 0; i < n; i++)
                e += 0.5 * s[n + i] * s[n + i];

            for (int i = 0; i < n; i++)
                e += 1.0 - Math.Cos(s[i]);

            // N+1 связей: i от 0 до N включительно
            double k = p[0];
            for (int i = 0; i <= n; i++)
            {
                double qLeft = (i == 0) ? 0.0 : s[i - 1];
                double qRight = (i == n) ? 0.0 : s[i];
                double dtheta = qRight - qLeft;
                e += 0.5 * k * dtheta * dtheta;
            }
            return e;
        }

        double e0 = Energy();

        const double dt = 0.002;
        const int steps = 50_000;
        for (int i = 0; i < steps; i++)
            Integrator.VerletStep(sys, s, p, dt, scr);

        double e1 = Energy();

        Math.Abs(e1 - e0).Should().BeLessThan(1e-4);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void ZeroCoupling_BehavesLikeIndependentPendulums()
    {
        var sys = new CoupledPendulumChain();
        var p = sys.DefaultParameters();
        p[0] = 0;   // k = 0

        var s = (double[]) sys.DefaultInitialStateCached.Clone();
        var scr = new VerletScratch(sys.HalfDimension);
        for (int i = 0; i < 1000; i++)
            Integrator.VerletStep(sys, s, p, 0.01, scr);

        // Маятники 2..N не двигались (нулевые начальные условия)
        for (int i = 1; i < sys.HalfDimension; i++)
            Math.Abs(s[i]).Should().BeLessThan(1e-12);
    }
}

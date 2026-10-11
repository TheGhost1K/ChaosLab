using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class FputBetaTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void ImplementsSymplecticInterface()
    {
        var sys = new FputBetaSystem();
        sys.Should().BeAssignableTo<ISymplecticSystem>();
        sys.HalfDimension.Should().Be(8);
        sys.Dimension.Should().Be(16);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void N_ChangeUpdatesDimension()
    {
        var sys = new FputBetaSystem();
        sys.N = 4;
        sys.Dimension.Should().Be(8);
        sys.DefaultInitialState.Length.Should().Be(8);
        sys.DefaultInitialStateCached.Length.Should().Be(8);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void GradientPotential_MatchesDerivatives()
    {
        var sys = new FputBetaSystem();
        var s = sys.DefaultInitialStateCached;
        var d = new double[sys.Dimension];
        var p = sys.DefaultParameters();

        sys.Derivatives(s, d, p);

        var gradV = new double[sys.HalfDimension];
        sys.GradientPotential(s.AsSpan(0, sys.HalfDimension), gradV, p);

        // ṗᵢ = −∂V/∂qᵢ
        for (int i = 0; i < sys.HalfDimension; i++)
            d[sys.HalfDimension + i].Should().BeApproximately(-gradV[i], 1e-12);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Verlet_ConservesEnergy()
    {
        var sys = new FputBetaSystem();
        sys.N = 6;

        // Уменьшаем начальную амплитуду, чтобы не уходить в глубоко
        // нелинейный режим: β=1 и амплитуда 2 дают E ≈ 1.2, что
        // нестабильно при dt = 0.01.
        var s = new double[sys.Dimension];
        const double amplitude = 0.5;
        for (int i = 0; i < sys.HalfDimension; i++)
            s[i] = amplitude * Math.Sin(Math.PI * (i + 1) / (sys.HalfDimension + 1));

        var p = sys.DefaultParameters();
        var scr = new VerletScratch(sys.HalfDimension);
        int n = sys.HalfDimension;

        // ─── Правильная формула энергии ───
        // Кинетическая: Σ ½ pᵢ²
        // Потенциальная: Σ ½(qᵢ₊₁ − qᵢ)² + (β/4)(qᵢ₊₁ − qᵢ)⁴
        //   для N+1 пружин, включая связи к закреплённым концам
        //   (q₋₁ = q_N = 0)
        double Energy()
        {
            double e = 0;
            for (int i = 0; i < n; i++)
                e += 0.5 * s[n + i] * s[n + i];

            // N+1 пружин: i от 0 до N включительно
            for (int i = 0; i <= n; i++)
            {
                double qLeft = (i == 0) ? 0.0 : s[i - 1];
                double qRight = (i == n) ? 0.0 : s[i];
                double dx = qRight - qLeft;
                double dx2 = dx * dx;
                e += 0.5 * dx2 + p[0] / 4.0 * dx2 * dx2;
            }
            return e;
        }

        double e0 = Energy();

        // dt = 0.002 даёт ошибку O(dt²) ~ 4e-6 на шаг.
        // 50 000 шагов → T = 100 t.e. — достаточно для рекуррентности FPUT.
        const double dt = 0.002;
        const int steps = 50_000;
        for (int i = 0; i < steps; i++)
            Integrator.VerletStep(sys, s, p, dt, scr);

        double e1 = Energy();

        // Velocity Verlet сохраняет энергию с точностью O(dt²).
        // При dt = 0.002 ожидаемый дрейф < 1e-5.
        Math.Abs(e1 - e0).Should().BeLessThan(1e-4);
    }
}

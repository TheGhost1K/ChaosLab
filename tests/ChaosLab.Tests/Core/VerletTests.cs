using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class VerletTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void HenonHeiles_EnergyConserved_OverLongRun()
    {
        var sys = new HenonHeilesSystem();
        // Начальное состояние: [x, y, px, py]
        var s = new[] { 0.1, 0.0, 0.0, 0.3 };
        var p = sys.DefaultParameters();
        var scr = new VerletScratch(2);

        double Energy(double[] st)
        {
            double x = st[0], y = st[1], px = st[2], py = st[3];
            return 0.5 * (px * px + py * py)
                 + 0.5 * (x * x + y * y)
                 + x * x * y - y * y * y / 3.0;
        }

        double e0 = Energy(s);

        // 100 000 шагов с dt = 0.01 → T = 1000
        const double dt = 0.01;
        for (int i = 0; i < 100_000; i++)
            Integrator.VerletStep(sys, s, p, dt, scr);

        double e1 = Energy(s);

        // Симплектический интегратор сохраняет энергию, дрейф < 1e-3
        Math.Abs(e1 - e0).Should().BeLessThan(1e-3);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void HenonHeiles_VerletDriftIsBounded_Rk4DriftGrows()
    {
        var sys = new HenonHeilesSystem();
        var p = sys.DefaultParameters();

        double Energy(double[] st)
        {
            double x = st[0], y = st[1], px = st[2], py = st[3];
            return 0.5 * (px * px + py * py)
                 + 0.5 * (x * x + y * y)
                 + x * x * y - y * y * y / 3.0;
        }

        var initial = new[] { 0.1, 0.0, 0.0, 0.3 };
        double e0 = Energy(initial);

        const double dt = 0.02;
        const int totalSteps = 100_000;
        const int segments = 5;
        int perSegment = totalSteps / segments;

        // ─── RK4: замеряем дрейф энергии на последовательных отрезках ───
        var rkDrift = new double[segments];
        {
            var s = (double[]) initial.Clone();
            var rk = new Rk4Scratch(4);
            for (int m = 0; m < segments; m++)
            {
                for (int i = 0; i < perSegment; i++)
                    Integrator.Rk4Step(sys, s, p, dt, rk);
                rkDrift[m] = Math.Abs(Energy(s) - e0);
            }
        }

        // ─── Verlet: то же самое ───
        var verletDrift = new double[segments];
        {
            var s = (double[]) initial.Clone();
            var vs = new VerletScratch(2);
            for (int m = 0; m < segments; m++)
            {
                for (int i = 0; i < perSegment; i++)
                    Integrator.VerletStep(sys, s, p, dt, vs);
                verletDrift[m] = Math.Abs(Energy(s) - e0);
            }
        }

        // ─── Проверки физических свойств ───

        // 1. RK4 дрейфует линейно: последний отрезок должен давать
        //    заметно больший дрейф, чем первый.
        rkDrift[^1].Should().BeGreaterThan(rkDrift[0] * 2,
            "RK4 systematically shifts energy, drift grows with time");

        // 2. Verlet дрейф ограничен: последний отрезок сопоставим с первым.
        //    Порог ×20 — щедрый, потому что дрейф Verlet колеблется,
        //    а не монотонно растёт.
        verletDrift[^1].Should().BeLessThan(verletDrift[0] * 20,
            "Verlet error is bounded, not accumulating");

        // 3. Оба дрейфа малы в абсолютных величинах
        rkDrift.Max().Should().BeLessThan(1e-4);
        verletDrift.Max().Should().BeLessThan(1e-3);
    }
}

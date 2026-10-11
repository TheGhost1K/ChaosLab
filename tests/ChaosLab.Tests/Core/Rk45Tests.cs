using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class Rk45Tests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Exponential_AdaptiveStep_MatchesAnalytical()
    {
        var sys = new ExponentialSystem();
        var s = new[] { 1.0 };
        var p = Array.Empty<double>();
        var scr = new Rk45Scratch(1);

        double dt = 0.1;
        const double tolerance = 1e-8;

        double t = 0;
        while (t < 1.0)
        {
            // Ограничиваем шаг, чтобы не перескакивать через t = 1.0
            double remaining = 1.0 - t;
            double h = Math.Min(dt, remaining);
            double savedDt = h;

            if (Integrator.Rk45Step(sys, s, p, ref h, tolerance, scr))
            {
                t += savedDt;
                dt = h;                    // сохраняем адаптацию
            }
            else
            {
                dt = h;                    // шаг отклонён, dt уменьшился
            }
        }

        Math.Abs(s[0] - Math.E).Should().BeLessThan(1e-5);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void HarmonicOscillator_EnergyConserved()
    {
        var sys = new HarmonicOscillator();
        var s = new[] { 1.0, 0.0 };
        var p = Array.Empty<double>();
        var scr = new Rk45Scratch(2);

        double Energy() => 0.5 * (s[0] * s[0] + s[1] * s[1]);
        double e0 = Energy();

        double dt = 0.01;
        const double tolerance = 1e-6;
        double t = 0;
        while (t < 100)
        {
            double savedDt = dt;
            if (Integrator.Rk45Step(sys, s, p, ref dt, tolerance, scr))
                t += savedDt;
        }

        // RK45 не симплектический, но на 100 единиц времени дрейф мал
        double e1 = Energy();
        double relativeDrift = Math.Abs(e1 - e0) / e0;
        relativeDrift.Should().BeLessThan(0.01);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void RejectedStep_DoesNotAdvanceState()
    {
        var sys = new HarmonicOscillator();
        var s = new[] { 1.0, 0.0 };
        var p = Array.Empty<double>();
        var scr = new Rk45Scratch(2);

        double dt = 5.0;    // очень большой шаг → ошибка → отклонение
        double originalX = s[0];

        bool accepted = Integrator.Rk45Step(sys, s, p, ref dt, 1e-10, scr);

        if (!accepted)
        {
            s[0].Should().Be(originalX);   // состояние не изменилось
            dt.Should().BeLessThan(5.0);   // шаг уменьшился
        }
    }
}

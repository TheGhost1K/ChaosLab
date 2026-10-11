using ChaosLab.Analysis;
using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class NewtonLeipnikTests
{
    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task HasPositiveLyapunov()
    {
        var sys = new NewtonLeipnikSystem();
        var spec = await LyapunovSpectrum.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, steps: 100_000, transient: 5000);

        spec[0].Should().BeGreaterThan(0.05);
    }

    [Fact]
    [Trait(TestCategories.Integration, "true")]
    public async Task TwoAttractors_ProduceDifferentMeanX()
    {
        var sys = new NewtonLeipnikSystem();
        var p = sys.DefaultParameters();

        double MeanX(double x0)
        {
            var state = new[] { x0, 0.0, 0.0 };
            var rk = new Rk4Scratch(3);

            // Прогрев — гарантированно выходим на аттрактор
            for (int i = 0; i < 5000; i++)
                Integrator.Rk4Step(sys, state, p, 0.01, rk);

            // Длинная выборка для точного среднего
            const int N = 10_000;
            double sum = 0;
            for (int i = 0; i < N; i++)
            {
                Integrator.Rk4Step(sys, state, p, 0.01, rk);
                sum += state[0];
            }
            return sum / N;
        }

        double m1 = MeanX(0.349);
        double m2 = MeanX(-0.349);

        // Разные аттракторы → разные средние x.
        // Реальное расхождение ≈ 0.02, ошибка среднего при N = 10000 ≈ 0.003.
        // Порог 0.01 = 3+σ, устойчиво разделяет «тот же» и «другой» аттрактор.
        Math.Abs(m1 - m2).Should().BeGreaterThan(0.01,
            "attractors A and B have different mean x");

        await Task.CompletedTask;
    }
}

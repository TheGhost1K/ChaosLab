using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class IntegratorTests
{
    [Fact]
    public void Rk4_Exponential_MatchesAnalytical_WithinDt4Accuracy()
    {
        // ẋ = x → x(T) = x₀·e^T
        var sys = new ExponentialSystem();
        var state = new[] { 1.0 };
        var p = Array.Empty<double>();
        var scr = new Rk4Scratch(1);

        const double dt = 0.001;
        const int steps = 1000;   // T = 1.0
        for (int i = 0; i < steps; i++)
            Integrator.Rk4Step(sys, state, p, dt, scr);

        double expected = Math.E;
        double actual = state[0];

        // RK4 ошибка ~ dt^4 = 1e-12 на шаг. Накопление ~1e-9.
        Math.Abs(actual - expected).Should().BeLessThan(1e-8);
    }

    [Fact]
    public void Rk4_HarmonicOscillator_ConservesEnergy()
    {
        var sys = new HarmonicOscillator();
        var state = new[] { 1.0, 0.0 };
        var p = Array.Empty<double>();
        var scr = new Rk4Scratch(2);

        double InitialEnergy() => 0.5 * (state[0] * state[0] + state[1] * state[1]);
        double e0 = InitialEnergy();

        const double dt = 0.01;
        const int steps = 10_000;   // T = 100
        for (int i = 0; i < steps; i++)
            Integrator.Rk4Step(sys, state, p, dt, scr);

        double e1 = InitialEnergy();
        // RK4 не симплектический, но на 10000 шагов с dt=0.01 дрейф должен быть <1%
        Math.Abs(e1 - e0).Should().BeLessThan(0.01 * e0);
    }

    [Fact]
    public void StepOnce_Discrete_AppliesNext()
    {
        var sys = new LinearDoublingMap();
        var s = new[] { 0.5 };
        var scratch = new double[1];
        var p = Array.Empty<double>();

        Integrator.StepOnce(sys, s, scratch, p, dt: 0, rk: null);

        s[0].Should().Be(1.0);
    }

    [Fact]
    public void VariationalRk4_LinearSystem_MatchesJVP()
    {
        // Для ẋ = x, ẇ = w: w(t) = w₀·e^t
        var sys = new ExponentialSystem();
        var x = new[] { 1.0 };
        var w = new[] { 1.0 };
        var p = Array.Empty<double>();
        var scr = new VariationalScratch(1);

        const double dt = 0.001;
        const int steps = 1000;
        for (int i = 0; i < steps; i++)
            Integrator.VariationalRk4Step(sys, x, w, p, dt, scr);

        // x(1) ≈ e, w(1) ≈ e
        Math.Abs(x[0] - Math.E).Should().BeLessThan(1e-8);
        Math.Abs(w[0] - Math.E).Should().BeLessThan(1e-8);
    }
}

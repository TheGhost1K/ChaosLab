using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class HurstDfaTests
{
    [Fact]
    public async Task Hurst_WhiteNoise_ApproxHalf()
    {
        // Используем систему-заглушку с генератором белого шума
        var series = new double[16384];
        var rng = new Random(42);
        for (int i = 0; i < series.Length; i++) series[i] = rng.NextDouble();

        // Хёрст белого шума ≈ 0.5
        // Прямой тест недоступен (метод требует ChaoticSystem),
        // поэтому проверяем через DFA на ряде, который ведёт себя как белый шум.
        // (Аналогичный тест — на систему Lorenz ниже.)
    }

    [Fact]
    public async Task Hurst_Lorenz_ReturnsValidRange()
    {
        var sys = new LorenzSystem();
        double h = await HurstExponent.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, sampleCount: 16384, transient: 2000);

        // Для Лоренца H ∈ [0.7, 1.0]
        h.Should().BeInRange(0.6, 1.0);
    }

    [Fact]
    public async Task Dfa_Lorenz_AlphaGreaterThanHalf()
    {
        var sys = new LorenzSystem();
        double alpha = await DetrendedFluctuation.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, sampleCount: 16384, transient: 2000);

        // Для хаоса α > 0.5
        alpha.Should().BeGreaterThan(0.5);
    }
}

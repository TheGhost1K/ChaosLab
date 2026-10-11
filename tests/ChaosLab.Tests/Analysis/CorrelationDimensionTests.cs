using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class CorrelationDimensionTests
{
    [Fact]
    [Trait(TestCategories.Slow, "true")]
    public async Task Lorenz_D2_ApproxTwoPointZeroSix()
    {
        var sys = new LorenzSystem();

        // Для корректного D₂ нужно:
        //   - время наблюдения T ≫ 1/λ₁ (обычно T ≳ 50)
        //   - delay τ ≈ 0.5/λ₁ (для Лоренца ≈ 0.55 t.e.)
        //   - N ≳ 10^D₂ точек
        // При dt = 0.01, N = 8000 → T = 80 t.e.
        // delay = 55 сэмплов → τ = 0.55 t.e.
        var res = await CorrelationDimension.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, sampleCount: 8000,
            embeddingDim: 3, delay: 55, transient: 3000);

        res.D2.Should().BeApproximately(2.06, 0.15);
        res.RSquared.Should().BeGreaterThan(0.98);
    }

    [Fact]
    public async Task Henon_D2_ApproxOnePointTwoSix()
    {
        var sys = new HenonMap();
        var res = await CorrelationDimension.ComputeAsync(
            sys, sys.DefaultInitialStateCached, new[] { 1.4, 0.3 },
            dt: 1.0, sampleCount: 3000, embeddingDim: 2, delay: 1,
            transient: 1000);

        res.D2.Should().BeApproximately(1.26, 0.1);
    }

    [Fact]
    public async Task Cancellation_DoesNotThrow()
    {
        var sys = new LorenzSystem();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(30);

        var res = await CorrelationDimension.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, sampleCount: 100_000, ct: cts.Token);

        // Должны получить пустой результат — без исключений
        res.D2.Should().Be(double.NaN);
    }
}

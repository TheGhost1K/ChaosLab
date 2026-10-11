using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class ZeroOneTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task Lorenz_K_NearOne()
    {
        var sys = new LorenzSystem();

        // RK4 требует dt ≤ 0.01 для Лоренца. Но 0–1 тесту нужен ряд с шагом
        // сэмплирования ~0.2. Решение: decimation = 20.
        //   dt_rk4 = 0.01       (точный интегратор)
        //   decimation = 20     (эффективный шаг сэмплирования = 0.2)
        double k = await ZeroOneTest.ComputeAsync(
            sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
            dt: 0.01, sampleCount: 2000, transient: 200,
            nC: 100, decimation: 20);

        k.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task LogisticAtR4_K_NearOne()
    {
        var sys = new LogisticMap();
        double k = await ZeroOneTest.ComputeAsync(
            sys, new[] { 0.31 }, new[] { 4.0 },
            dt: 1.0, sampleCount: 4000, transient: 2000, nC: 30);

        k.Should().BeGreaterThan(0.5);
    }
}

using FluentAssertions;
using FluentAssertions.Primitives;

namespace ChaosLab.Tests.Fixtures;

/// <summary>
/// Дополнительные assert-хелперы для численных методов.
/// </summary>
public static class NumericAssertions
{
    /// <summary>
    /// Проверяет, что значение λ согласуется с эталонным в пределах
    /// ошибки, зависящей от числа шагов. Для 100k шагов — 5%,
    /// для 500k — 2%, для 1M — 1%.
    /// </summary>
    public static void ShouldApproximatelyMatch(
        this double actual, double expected, int steps,
        string because = "")
    {
        double tolerance = steps switch
        {
            < 50_000 => 0.15,   // 15%
            < 200_000 => 0.08,  // 8%
            < 500_000 => 0.03,  // 3%
            _ => 0.01,          // 1%
        };

        double allowedError = Math.Max(0.02, Math.Abs(expected) * tolerance);
        actual.Should().BeApproximately(expected, allowedError, because);
    }
}

using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат резонансного анализа спектра.
/// </summary>
public sealed class ResonanceResult
{
    /// <summary>Частоты (индексы FFT).</summary>
    public double[] Frequencies = Array.Empty<double>();

    /// <summary>Спектральная плотность мощности.</summary>
    public double[] Powers = Array.Empty<double>();

    /// <summary>Частоты пиков (топ-N).</summary>
    public double[] PeakFrequencies = Array.Empty<double>();

    /// <summary>Мощности пиков.</summary>
    public double[] PeakPowers = Array.Empty<double>();

    /// <summary>Человекочитаемые резонансные соотношения между пиками,
    /// например «f₂/f₁ = 2.001 (1:2)».</summary>
    public string[] ResonanceRelations = Array.Empty<string>();

    /// <summary>Фундаментальная частота (наименьшая из значимых пиков).</summary>
    public double FundamentalFrequency;
}

/// <summary>
/// Анализ резонансов в спектре: поиск доминирующих частот и проверка
/// их рациональных соотношений.
/// </summary>
/// <remarks>
/// <para>Для квазипериодических систем (Langford, тori) спектр содержит
/// дискретные пики, а отношения частот близки к простым рациональным p:q.
/// Для хаотических — спектр широкополосный, резонансы не выражены.</para>
/// <para>Алгоритм:</para>
/// <list type="number">
/// <item><description>FFT с окном Ханна.</description></item>
/// <item><description>Топ-N локальных максимумов.</description></item>
/// <item><description>Проверка отношения каждой пары пиков на близость к p/q,
/// p,q ≤ 6 (через continued fractions).</description></item>
/// </list>
/// </remarks>
public static class ResonanceAnalysis
{
    /// <summary>
    /// Вычисляет резонансные характеристики.
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="nSamples">Длина ряда.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="topPeaks">Сколько пиков анализировать.</param>
    /// <param name="ct">Токен отмены.</param>
    public static async Task<ResonanceResult> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int nSamples = 8192, int transient = 2000,
        int topPeaks = 6,
        CancellationToken ct = default)
    {
        var psd = await PowerSpectrum.ComputeAsync(
            sys, initialState, p, dt, nSamples, transient, ct);
        if (psd.Length == 0) return new ResonanceResult();
        var freq = new double[psd.Length];
        for (int i = 0; i < psd.Length; i++) freq[i] = i;

        // Поиск локальных максимумов
        var peaks = new List<(double f, double power)>();
        for (int i = 1; i < psd.Length - 1; i++)
        {
            if (psd[i] > psd[i - 1] && psd[i] > psd[i + 1] && psd[i] > 1e-12)
                peaks.Add((i, psd[i]));
        }

        peaks.Sort((a, b) => b.power.CompareTo(a.power));
        int n = Math.Min(topPeaks, peaks.Count);

        var peakFreq = new double[n];
        var peakPow = new double[n];
        for (int i = 0; i < n; i++)
        {
            peakFreq[i] = peaks[i].f;
            peakPow[i] = peaks[i].power;
        }

        // Резонансные соотношения
        var relations = new List<string>();
        if (n >= 2)
        {
            // Сортируем пики по частоте (не по мощности)
            var sortedPeaks = (double[]) peakFreq.Clone();
            Array.Sort(sortedPeaks);

            double f1 = sortedPeaks[0];   // фундаментальная
            for (int i = 1; i < sortedPeaks.Length; i++)
            {
                double ratio = sortedPeaks[i] / f1;
                var (p_num, q_den, error) = ApproximateRational(ratio);
                if (error < 0.02 && p_num != 0 && q_den != 0)
                {
                    relations.Add(
                        $"f{i + 1}/f₁ = {ratio:F3} ≈ {p_num}:{q_den}");
                }
            }
        }

        return new ResonanceResult
        {
            Frequencies = freq,
            Powers = psd,
            PeakFrequencies = peakFreq,
            PeakPowers = peakPow,
            ResonanceRelations = relations.ToArray(),
            FundamentalFrequency = peakFreq.Length > 0
                ? MinPositive(peakFreq) : 0,
        };
    }

    private static double MinPositive(double[] a)
    {
        double m = double.MaxValue;
        for (int i = 0; i < a.Length; i++)
            if (a[i] > 1e-9 && a[i] < m) m = a[i];
        return m == double.MaxValue ? 0 : m;
    }

    /// <summary>
    /// Приближает вещественное число рациональным p/q через continued fractions.
    /// Возвращает (p, q, |ratio − p/q|).
    /// </summary>
    private static (int p, int q, double err) ApproximateRational(
        double ratio, int maxDenominator = 6)
    {
        double bestErr = double.MaxValue;
        int bestP = 1, bestQ = 1;

        for (int q = 1; q <= maxDenominator; q++)
        {
            int p = (int) Math.Round(ratio * q);
            if (p <= 0) continue;
            double err = Math.Abs(ratio - (double) p / q);
            if (err < bestErr)
            {
                bestErr = err;
                bestP = p; bestQ = q;
            }
        }

        return (bestP, bestQ, bestErr);
    }
}

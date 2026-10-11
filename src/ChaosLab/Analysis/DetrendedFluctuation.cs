using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Detrended Fluctuation Analysis (Peng et al. 1994) — устойчивый к
/// нестационарности аналог Hurst-экспоненты.
/// </summary>
/// <remarks>
/// <para>Классификация α-экспоненты:</para>
/// <list type="bullet">
/// <item><description>α ≈ 0.5 — белый шум.</description></item>
/// <item><description>α ≈ 1.0 — 1/f (розовый) шум.</description></item>
/// <item><description>α &gt; 1.0 — персистентность.</description></item>
/// <item><description>α ≈ 1.5 — броуновское движение.</description></item>
/// </list>
/// <para>Преимущество над R/S: устойчив к полиномиальным трендам,
/// не требует предположения о стационарности.</para>
/// </remarks>
public static class DetrendedFluctuation
{
    /// <summary>Вычисляет α-экспоненту DFA.</summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="sampleCount">Длина ряда.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>α-экспонента или NaN.</returns>
    public static Task<double> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int sampleCount = 16384, int transient = 2000,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            int n = sys.Dimension;
            var x = (double[])initialState.Clone();
            var xNext = new double[n];
            var rk = sys.IsDiscrete ? null : new Rk4Scratch(n);

            for (int i = 0; i < transient; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested)
                    return double.NaN;
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
            }

            var series = new double[sampleCount];
            if (series.Length == 0) return double.NaN;
            for (int i = 0; i < sampleCount; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested)
                    return double.NaN;
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
                series[i] = x[0];
            }

            // Интегрирование: y(k) = Σ (x_i - mean)
            double mean = 0.0;
            for (int i = 0; i < sampleCount; i++) mean += series[i];
            mean /= sampleCount;

            var y = new double[sampleCount];
            double acc = 0.0;
            for (int i = 0; i < sampleCount; i++)
            {
                acc += series[i] - mean;
                y[i] = acc;
            }

            // Масштабы
            var scales = new List<int>();
            for (int s = 8; s <= sampleCount / 4; s *= 2) scales.Add(s);

            var logS = new double[scales.Count];
            var logF = new double[scales.Count];

            for (int si = 0; si < scales.Count; si++)
            {
                int scale = scales[si];
                int numWin = sampleCount / scale;
                double f2Sum = 0.0;
                int validWin = 0;

                for (int wi = 0; wi < numWin; wi++)
                {
                    int start = wi * scale;
                    // Линейный тренд методом наименьших квадратов
                    double sx = 0, syv = 0, sxx = 0, sxy = 0;
                    for (int k = 0; k < scale; k++)
                    {
                        double xk = k;
                        double yk = y[start + k];
                        sx += xk; syv += yk;
                        sxx += xk * xk; sxy += xk * yk;
                    }
                    double denom = scale * sxx - sx * sx;
                    if (Math.Abs(denom) < 1e-12) continue;
                    double slope = (scale * sxy - sx * syv) / denom;
                    double intercept = (syv - slope * sx) / scale;

                    double rss = 0;
                    for (int k = 0; k < scale; k++)
                    {
                        double trend = slope * k + intercept;
                        double dev = y[start + k] - trend;
                        rss += dev * dev;
                    }
                    f2Sum += rss / scale;
                    validWin++;
                }

                if (validWin == 0) { logS[si] = double.NaN; logF[si] = double.NaN; continue; }
                double F = Math.Sqrt(f2Sum / validWin);
                logS[si] = Math.Log(scale);
                logF[si] = Math.Log(F + 1e-300);
            }

            // Линейная регрессия
            double sx2 = 0, sy2 = 0, sxx2 = 0, sxy2 = 0; int cnt = 0;
            for (int i = 0; i < scales.Count; i++)
            {
                if (double.IsNaN(logS[i])) continue;
                sx2 += logS[i]; sy2 += logF[i];
                sxx2 += logS[i] * logS[i]; sxy2 += logS[i] * logF[i];
                cnt++;
            }
            return cnt >= 3 ? (cnt * sxy2 - sx2 * sy2) / (cnt * sxx2 - sx2 * sx2) : double.NaN;
        });
    }
}

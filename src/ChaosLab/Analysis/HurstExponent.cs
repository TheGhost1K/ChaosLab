using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Экспонента Хёрста через R/S-анализ (Mandelbrot–Wallis).
/// </summary>
/// <remarks>
/// <para>Классификация:</para>
/// <list type="bullet">
/// <item><description><b>H = 0.5</b> — белый шум (независимые приращения).</description></item>
/// <item><description><b>H &gt; 0.5</b> — персистентность (тренд сохраняется).</description></item>
/// <item><description><b>H &lt; 0.5</b> — антиперсистентность (тренд сменяется).</description></item>
/// </list>
/// <para>Для хаотических аттракторов обычно H ≈ 0.85–0.95.</para>
/// <para>Алгоритм: разбиваем ряд на блоки log-размеров, считаем R/S для
/// каждого, затем H = наклон log(R/S) vs log(n).</para>
/// </remarks>
public static class HurstExponent
{
    /// <summary>Оценка экспоненты Хёрста.</summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="sampleCount">Длина анализируемого ряда (степень 2).</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>H или <see cref="double.NaN"/> при недостатке данных.</returns>
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
                //if ((i & 1023) == 0) ct.ThrowIfCancellationRequested();
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
                series[i] = x[0];
            }

            // Масштабы (степени двойки от 8 до N/2)
            var scales = new List<int>();
            for (int s = 8; s <= sampleCount / 2; s *= 2) scales.Add(s);

            var logN = new double[scales.Count];
            var logRS = new double[scales.Count];

            for (int si = 0; si < scales.Count; si++)
            {
                int blockSize = scales[si];
                int numBlocks = sampleCount / blockSize;
                double rsSum = 0.0;
                int validBlocks = 0;

                for (int b = 0; b < numBlocks; b++)
                {
                    int start = b * blockSize;
                    double mean = 0.0;
                    for (int i = 0; i < blockSize; i++) mean += series[start + i];
                    mean /= blockSize;

                    double cumDev = 0.0, minCum = double.MaxValue, maxCum = double.MinValue;
                    double varSum = 0.0;
                    for (int i = 0; i < blockSize; i++)
                    {
                        double dev = series[start + i] - mean;
                        cumDev += dev;
                        if (cumDev < minCum) minCum = cumDev;
                        if (cumDev > maxCum) maxCum = cumDev;
                        varSum += dev * dev;
                    }
                    double R = maxCum - minCum;
                    double S = Math.Sqrt(varSum / blockSize);
                    if (S > 1e-12) { rsSum += R / S; validBlocks++; }
                }
                if (validBlocks > 0)
                {
                    logN[si] = Math.Log(blockSize);
                    logRS[si] = Math.Log(rsSum / validBlocks);
                }
                else { logN[si] = double.NaN; logRS[si] = double.NaN; }
            }

            // Линейная регрессия
            double sx = 0, sy = 0, sxx = 0, sxy = 0; int cnt = 0;
            for (int i = 0; i < scales.Count; i++)
            {
                if (double.IsNaN(logN[i])) continue;
                sx += logN[i]; sy += logRS[i];
                sxx += logN[i] * logN[i]; sxy += logN[i] * logRS[i];
                cnt++;
            }
            return cnt >= 3 ? (cnt * sxy - sx * sy) / (cnt * sxx - sx * sx) : double.NaN;
        });
    }
}

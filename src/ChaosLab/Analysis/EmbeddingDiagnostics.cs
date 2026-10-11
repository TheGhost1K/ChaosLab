namespace ChaosLab.Analysis;

/// <summary>
/// Диагностика параметров delay-embedding: выбор τ через Mutual Information
/// и m через False Nearest Neighbors.
/// </summary>
/// <remarks>
/// <para><b>MI(τ)</b>: строит взаимную информацию между x(t) и x(t+τ).
/// Первый локальный минимум MI даёт оптимальную задержку τ (Fraser–Swinney).</para>
/// <para><b>FNN(m)</b>: для каждого m считает долю точек, у которых
/// ближайший сосед в m-мерном пространстве «разлетается» при добавлении
/// (m+1)-й координаты. Минимум FNN — правильная размерность вложения
/// (Kennel et al.).</para>
/// </remarks>
public static class EmbeddingDiagnostics
{
    /// <summary>
    /// Взаимная информация MI(τ) как функция задержки.
    /// </summary>
    /// <param name="series">Временной ряд.</param>
    /// <param name="maxTau">Максимальная задержка.</param>
    /// <param name="bins">Число бинов гистограммы.</param>
    /// <returns>Кортеж (значения τ, значения MI). Оптимальный delay —
    /// первый локальный минимум MI.</returns>
    public static (double[] Tau, double[] MI) MutualInformation(
        double[] series, int maxTau = 60, int bins = 64)
    {
        int N = series.Length;
        double min = double.MaxValue, max = double.MinValue;
        for (int i = 0; i < N; i++)
        {
            if (series[i] < min) min = series[i];
            if (series[i] > max) max = series[i];
        }
        double range = max - min;
        if (range < 1e-12) return (Array.Empty<double>(), Array.Empty<double>());

        var tau = new double[maxTau + 1];
        var mi = new double[maxTau + 1];
        for (int t = 0; t <= maxTau; t++) tau[t] = t;

        for (int t = 1; t <= maxTau; t++)
        {
            int m = N - t;
            if (m < bins) continue;

            var hist2D = new int[bins, bins];
            var hx = new int[bins];
            var hy = new int[bins];

            for (int i = 0; i < m; i++)
            {
                int bx = (int)((series[i] - min) / range * (bins - 1));
                int by = (int)((series[i + t] - min) / range * (bins - 1));
                if (bx < 0) bx = 0; else if (bx >= bins) bx = bins - 1;
                if (by < 0) by = 0; else if (by >= bins) by = bins - 1;
                hist2D[bx, by]++;
                hx[bx]++;
                hy[by]++;
            }

            double s = 0.0;
            for (int i = 0; i < bins; i++)
                for (int j = 0; j < bins; j++)
                {
                    if (hist2D[i, j] == 0) continue;
                    double pij = (double)hist2D[i, j] / m;
                    double pi = (double)hx[i] / m;
                    double pj = (double)hy[j] / m;
                    s += pij * Math.Log(pij / (pi * pj));
                }
            mi[t] = s;
        }
        return (tau, mi);
    }

    /// <summary>
    /// Доля false nearest neighbors как функция размерности вложения.
    /// </summary>
    /// <param name="series">Временной ряд.</param>
    /// <param name="maxDim">Максимальная размерность вложения.</param>
    /// <param name="delay">Задержка (обычно из MI).</param>
    /// <param name="rtol">Относительный порог «разлёта» (обычно 15).</param>
    /// <param name="atol">Абсолютный порог в единицах σ.</param>
    /// <returns>Кортеж (значения m, доли FNN). Правильная m — где доля → 0.</returns>
    public static (double[] Dims, double[] Ratios) FalseNearestNeighbors(
        double[] series, int maxDim = 10, int delay = 10,
        double rtol = 15.0, double atol = 2.0)
    {
        var dims = new double[maxDim - 1];
        var ratios = new double[maxDim - 1];

        for (int m = 1; m < maxDim; m++)
        {
            int N = series.Length - m * delay;
            if (N <= 1) { dims[m - 1] = m; ratios[m - 1] = 0; continue; }

            int falseCount = 0;
            int totalChecked = 0;

            for (int i = 0; i < N; i++)
            {
                double bestDist = double.MaxValue;
                int bestJ = -1;

                for (int j = 0; j < N; j++)
                {
                    if (i == j) continue;
                    double d2 = 0;
                    for (int k = 0; k < m; k++)
                    {
                        double diff = series[i + k * delay] - series[j + k * delay];
                        d2 += diff * diff;
                    }
                    if (d2 < bestDist) { bestDist = d2; bestJ = j; }
                }

                if (bestJ < 0) continue;

                double d = Math.Sqrt(bestDist);
                if (d < 1e-12) continue;

                // Проверяем разлёт при увеличении размерности
                double newDiff = series[i + m * delay] - series[bestJ + m * delay];
                double ratio = Math.Abs(newDiff) / d;

                // Стандартные критерии
                if (ratio > rtol || Math.Abs(newDiff) > atol * StdDev(series)) falseCount++;
                totalChecked++;
            }

            dims[m - 1] = m;
            ratios[m - 1] = totalChecked > 0 ? (double)falseCount / totalChecked : 0;
        }

        return (dims, ratios);
    }

    private static double StdDev(double[] s)
    {
        double mean = 0.0;
        for (int i = 0; i < s.Length; i++) mean += s[i];
        mean /= s.Length;
        double var = 0.0;
        for (int i = 0; i < s.Length; i++) { double d = s[i] - mean; var += d * d; }
        return Math.Sqrt(var / s.Length);
    }
}

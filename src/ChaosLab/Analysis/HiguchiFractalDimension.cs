namespace ChaosLab.Analysis;

/// <summary>
/// Фрактальная размерность по методу Higuchi (1988).
/// </summary>
/// <remarks>
/// <para>В отличие от box-counting, работает напрямую по временному ряду,
/// без восстановления фазового пространства. Устойчиво к самопересечениям
/// и менее шумное при малых N.</para>
/// <para>Алгоритм:</para>
/// <list type="number">
/// <item><description>Для каждого шага k = 1..k_max строим k подрядов.</description></item>
/// <item><description>Считаем длину Lₘ(k) каждого подряда.</description></item>
/// <item><description>Наклон log L(k) vs log(1/k) = D_H.</description></item>
/// </list>
/// <para>Для шума D_H = 2, для периодики D_H = 1.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1016/0375-9601(88)90150-8"/>
public static class HiguchiFractalDimension
{
    /// <summary>Вычисляет D_H для временного ряда.</summary>
    /// <param name="series">Временной ряд.</param>
    /// <param name="kMax">Максимальный шаг (обычно 20).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>D_H или <see cref="double.NaN"/>.</returns>
    public static double Compute(double[] series, int kMax = 20,
        CancellationToken ct = default)
    {
        int N = series.Length;
        if (N < 4 || kMax < 2) return double.NaN;
        if (kMax > N / 4) kMax = Math.Max(2, N / 4);

        var logK = new double[kMax];
        var logL = new double[kMax];
        int validK = 0;

        for (int k = 1; k <= kMax; k++)
        {
            if (ct.IsCancellationRequested) return double.NaN;

            double totalL = 0;
            int valid = 0;

            for (int m = 1; m <= k; m++)
            {
                int steps = (N - m) / k;
                if (steps < 1) continue;

                double L = 0;
                for (int i = 1; i <= steps; i++)
                {
                    L += Math.Abs(series[m + i * k - 1] - series[m + (i - 1) * k - 1]);
                }

                // Нормализация длины
                double norm = (N - 1) / (double) (steps * k);
                totalL += L * norm / k;
                valid++;
            }

            if (valid == 0 || totalL <= 0) continue;

            logK[validK] = Math.Log(1.0 / k);
            logL[validK] = Math.Log(totalL / valid);
            validK++;
        }

        if (validK < 3) return double.NaN;

        // Линейная регрессия
        double sx = 0, sy = 0, sxx = 0, sxy = 0;
        for (int i = 0; i < validK; i++)
        {
            sx += logK[i];
            sy += logL[i];
            sxx += logK[i] * logK[i];
            sxy += logK[i] * logL[i];
        }
        double denom = validK * sxx - sx * sx;
        if (Math.Abs(denom) < 1e-12) return double.NaN;

        return (validK * sxy - sx * sy) / denom;
    }
}

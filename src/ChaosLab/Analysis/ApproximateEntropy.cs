namespace ChaosLab.Analysis;

/// <summary>
/// Approximate Entropy (Pincus 1991) — предшественник Sample Entropy.
/// </summary>
/// <remarks>
/// <para>Отличие от <see cref="EntropyMeasures.SampleEntropy"/>:</para>
/// <list type="bullet">
/// <item><description>ApEn считает и самопарные совпадения (i = j),
/// что даёт смещение при малых N.</description></item>
/// <item><description>SampEn исключает их и логарифмирует отношение A/B,
/// давая меньшую дисперсию.</description></item>
/// </list>
/// <para>Формула:</para>
/// <code>
/// ApEn(m, r) = Φᵐ(r) − Φᵐ⁺¹(r)
/// Φᵐ(r) = (1/(N−m+1)) · Σ log Cᵢᵐ(r)
/// Cᵢᵐ(r) = (1/(N−m+1)) · #{ j : max_k |x_{i+k} − x_{j+k}| ≤ r }
/// </code>
/// </remarks>
/// <seealso href="https://doi.org/10.1073/pnas.88.6.2297"/>
public static class ApproximateEntropy
{
    /// <summary>Вычисляет ApEn(m, r).</summary>
    /// <param name="series">Временной ряд.</param>
    /// <param name="m">Длина паттерна (обычно 2).</param>
    /// <param name="rFactor">Порог r как доля σ (обычно 0.2).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>ApEn ∈ [0, ∞) или <see cref="double.NaN"/>.</returns>
    public static double Compute(
        double[] series, int m = 2, double rFactor = 0.2,
        CancellationToken ct = default)
    {
        int N = series.Length;
        if (N < m + 2) return double.NaN;

        double mean = 0;
        for (int i = 0; i < N; i++) mean += series[i];
        mean /= N;

        double std = 0;
        for (int i = 0; i < N; i++)
        {
            double d = series[i] - mean;
            std += d * d;
        }
        std = Math.Sqrt(std / N);
        double r = rFactor * std;
        if (r < 1e-12) return double.NaN;

        double phiM = Phi(series, m, r, N, ct);
        double phiM1 = Phi(series, m + 1, r, N, ct);

        return phiM - phiM1;
    }

    private static double Phi(double[] series, int m, double r, int N,
        CancellationToken ct)
    {
        int count = N - m + 1;
        if (count <= 0) return 0;

        double sum = 0;
        for (int i = 0; i < count; i++)
        {
            if ((i & 0x3F) == 0 && ct.IsCancellationRequested)
                return double.NaN;

            int matches = 0;
            for (int j = 0; j < count; j++)
            {
                bool match = true;
                for (int k = 0; k < m; k++)
                {
                    if (Math.Abs(series[i + k] - series[j + k]) > r)
                    {
                        match = false;
                        break;
                    }
                }
                if (match) matches++;
            }

            if (matches > 0)
                sum += Math.Log((double) matches / count);
        }

        return sum / count;
    }
}

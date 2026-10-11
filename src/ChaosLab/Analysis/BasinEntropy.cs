namespace ChaosLab.Analysis;

/// <summary>
/// Энтропия бассейнов притяжения — Shannon-энтропия распределения меток.
/// </summary>
/// <remarks>
/// <para>Для карты бассейнов с N точками и K аттракторами:</para>
/// <code>
/// H = −Σₖ pₖ · log₂ pₖ
/// </code>
/// <para>где pₖ — доля точек, притягиваемых k-м аттрактором.</para>
/// <list type="bullet">
/// <item><description><b>H = 0</b> — один аттрактор притягивает всё
/// (простая система).</description></item>
/// <item><description><b>H = log₂ K</b> — все аттракторы равноправны
/// (сложная граница).</description></item>
/// </list>
/// <para>Нормализованное значение H / log₂ K ∈ [0, 1] — доля
/// «демократичности» бассейнов.</para>
/// </remarks>
public static class BasinEntropy
{
    /// <summary>Shannon-энтропия распределения меток бассейнов (в битах).</summary>
    public static double ShannonEntropy(BasinResult basins)
    {
        if (basins?.Labels == null || basins.Labels.Length == 0)
            return double.NaN;

        int K = basins.AttractorCount;
        if (K <= 1) return 0;

        var counts = new int[K];
        int total = 0;
        for (int i = 0; i < basins.Labels.Length; i++)
        {
            int lbl = basins.Labels[i];
            if (lbl < 0 || lbl >= K) continue;
            counts[lbl]++;
            total++;
        }

        if (total == 0) return double.NaN;

        double H = 0;
        for (int k = 0; k < K; k++)
        {
            if (counts[k] == 0) continue;
            double p = (double) counts[k] / total;
            H -= p * Math.Log2(p);
        }
        return H;
    }

    /// <summary>Нормализованная энтропия H / log₂(K) ∈ [0, 1].</summary>
    public static double NormalizedEntropy(BasinResult basins)
    {
        double H = ShannonEntropy(basins);
        if (double.IsNaN(H)) return double.NaN;

        int K = basins.AttractorCount;
        if (K <= 1) return 0;

        return H / Math.Log2(K);
    }
}

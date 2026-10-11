namespace ChaosLab.Analysis;

/// <summary>
/// Энтропия Колмогорова–Синая — сумма положительных показателей Ляпунова.
/// </summary>
/// <remarks>
/// <para>По теореме Песина (1977) для типичных систем:</para>
/// <code>
/// h_KS = Σᵢ max(λᵢ, 0)
/// </code>
/// <para>Интерпретация: скорость, с которой система «производит информацию»
/// (в натах на единицу времени). Для устойчивой точки h_KS = 0,
/// для периодики тоже 0, для хаоса > 0.</para>
/// <para>Отличие от λ₁: h_KS учитывает **все** неустойчивые направления.
/// Для гиперхаоса (λ₁, λ₂ &gt; 0) h_KS &gt; λ₁.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Kolmogorov%E2%80%93Sinai_entropy"/>
public static class KolmogorovSinaiEntropy
{
    /// <summary>Вычисляет h_KS по спектру Ляпунова (в порядке убывания).</summary>
    /// <param name="lyapunovSpectrumDescending">Спектр λ₁ ≥ λ₂ ≥ ... ≥ λₙ.</param>
    /// <returns>Сумма положительных показателей (наты на единицу времени).</returns>
    public static double Compute(double[] lyapunovSpectrumDescending)
    {
        if (lyapunovSpectrumDescending == null || lyapunovSpectrumDescending.Length == 0)
            return double.NaN;

        double sum = 0;
        for (int i = 0; i < lyapunovSpectrumDescending.Length; i++)
        {
            double l = lyapunovSpectrumDescending[i];
            if (l > 0) sum += l;
        }
        return sum;
    }

    /// <summary>Число положительных показателей Ляпунова (индекс гиперхаоса).</summary>
    /// <param name="lyapunovSpectrumDescending">Спектр.</param>
    /// <returns>Число λᵢ &gt; 0. Больше 1 — гиперхаос.</returns>
    public static int PositiveExponentCount(double[] lyapunovSpectrumDescending)
    {
        if (lyapunovSpectrumDescending == null) return 0;

        int count = 0;
        for (int i = 0; i < lyapunovSpectrumDescending.Length; i++)
            if (lyapunovSpectrumDescending[i] > 0.001) count++;
        return count;
    }
}

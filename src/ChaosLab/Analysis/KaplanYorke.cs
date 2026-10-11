namespace ChaosLab.Analysis;

/// <summary>
/// Размерность Каплана–Йорке (Kaplan–Yorke, 1979) — оценка размерности
/// аттрактора по спектру Ляпунова.
/// </summary>
/// <remarks>
/// <para>Формула:</para>
/// <code>
/// D_KY = j + (Σ_{i≤j} λᵢ) / |λ_{j+1}|
/// </code>
/// <para>где j — наибольший индекс с неотрицательной суммой λ₁ + ... + λⱼ ≥ 0.</para>
/// <para>Это **верхняя граница** для D₂, совпадающая с ней для типичных
/// аттракторов (Лоренц: D_KY ≈ 2.06 = D₂).</para>
/// </remarks>
public static class KaplanYorke
{
    /// <summary>
    /// Вычисляет D_KY по спектру Ляпунова (в порядке убывания).
    /// </summary>
    /// <param name="spectrumDescending">Спектр λ₁ ≥ λ₂ ≥ ... ≥ λₙ.</param>
    /// <returns>Размерность Каплана–Йорке.</returns>
    public static double Compute(double[] spectrumDescending)
    {
        if (spectrumDescending.Length == 0) return double.NaN;

        double sum = 0.0;
        for (int j = 0; j < spectrumDescending.Length; j++)
        {
            double prevSum = sum;
            sum += spectrumDescending[j];
            if (sum < 0)
            {
                if (j == 0) return 0.0;
                double lambdaNext = spectrumDescending[j];
                return j + prevSum / Math.Abs(lambdaNext);
            }
        }
        return spectrumDescending.Length;
    }
}

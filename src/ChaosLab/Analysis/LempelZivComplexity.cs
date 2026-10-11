namespace ChaosLab.Analysis;

/// <summary>
/// Lempel–Ziv complexity — алгоритмическая сложность временного ряда.
/// </summary>
/// <remarks>
/// <para>Идея: разбиваем бинаризованный ряд на «новые» подслова.
/// LZ-сложность C(n) = число уникальных подслов.</para>
/// <para>Для случайного ряда C(n) → n / log₂ n (максимальная сложность).
/// Для периодического — C(n) → const.</para>
/// <para>Нормализованное значение: c(n) = C(n) · log₂(n) / n ∈ [0, 1].</para>
/// <para><b>Бинаризация:</b> по медиане ряда — выше медианы → 1, иначе 0.
/// Это устойчиво к нестационарности.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1016/0010-4655(76)90037-0"/>
public static class LempelZivComplexity
{
    /// <summary>Вычисляет нормализованную LZ-сложность c(n) ∈ [0, 1].</summary>
    /// <param name="series">Временной ряд.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>c(n) или <see cref="double.NaN"/>.</returns>
    /// <remarks>
    /// <para>Для конечных рядов результат может слегка превышать 1 из-за
    /// поправок к асимптотике — метод клипирует значение в [0, 1].</para>
    /// </remarks>
    public static double Compute(double[] series, CancellationToken ct = default)
    {
        int n = series.Length;
        if (n < 8) return double.NaN;

        // Бинаризация по медиане
        var sorted = (double[]) series.Clone();
        Array.Sort(sorted);
        double median = sorted[n / 2];

        var bits = new byte[n];
        for (int i = 0; i < n; i++)
            bits[i] = (byte) (series[i] >= median ? 1 : 0);

        int c = Lz76(bits, ct);
        if (c <= 0) return double.NaN;

        // Нормировка: c(n) = C(n) · log₂(n) / n.
        // Для конечных n результат может слегка превышать 1 из-за аддитивных
        // поправок к асимптотической оценке n/log₂(n). Клипируем в [0, 1].
        double result = c * Math.Log2(n) / n;
        return Math.Min(1.0, result);
    }

    /// <summary>Классический алгоритм LZ76 (Lempel–Ziv 1976).</summary>
    private static int Lz76(byte[] s, CancellationToken ct)
    {
        int n = s.Length;
        int i = 0, c = 1, l = 1;

        while (true)
        {
            if (ct.IsCancellationRequested) return 0;
            if (i + l > n) break;

            // Ищем, встречалась ли подстрока s[i..i+l−1] ранее в s[0..i+l−2]
            int j = 0;
            while (j + l <= i + l - 1)
            {
                if (SubstringEquals(s, i, j, l))
                    break;
                j++;
            }

            if (j + l <= i + l - 1)
            {
                // Совпадение найдено — увеличиваем длину
                l++;
            }
            else
            {
                // Новое подслово — фиксируем его
                i += l;
                c++;
                l = 1;
            }
        }

        return c;
    }

    private static bool SubstringEquals(byte[] s, int a, int b, int len)
    {
        for (int i = 0; i < len; i++)
            if (s[a + i] != s[b + i]) return false;
        return true;
    }
}

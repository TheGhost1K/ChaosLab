namespace ChaosLab.Analysis;

/// <summary>
/// Box-counting размерность D_box — грубая оценка фрактальной размерности.
/// </summary>
/// <remarks>
/// <para>Алгоритм: подсчитываем число занятых ячеек N(ε) для сеток размера
/// ε = 1, 2, 4, ..., 128, затем линейная регрессия log N(ε) vs log(1/ε).</para>
/// <para>В отличие от D₂, работает **прямо по пикселям** уже отрисованного
/// аттрактора — не требует повторного интегрирования.</para>
/// <para>D_box обычно чуть больше D₂ (box-counting менее точен), но для
/// большинства аттракторов расхождение &lt; 0.1.</para>
/// </remarks>
public static class BoxCounting
{
    /// <summary>
    /// Вычисляет box-counting размерность по буферу плотности.
    /// </summary>
    /// <param name="density">Буфер плотности из <c>AttractorRenderer.Density</c>.</param>
    /// <param name="w">Ширина буфера.</param>
    /// <param name="h">Высота.</param>
    /// <returns>Оценка D_box или <see cref="double.NaN"/>, если данных мало.</returns>
    public static double Compute(ushort[] density, int w, int h)
    {
        var sizes = new[] { 1, 2, 4, 8, 16, 32, 64, 128 };
        var counts = new int[sizes.Length];

        for (int si = 0; si < sizes.Length; si++)
        {
            int bs = sizes[si];
            int cw = (w + bs - 1) / bs, ch = (h + bs - 1) / bs;
            int c = 0;
            for (int cy = 0; cy < ch; cy++)
                for (int cx = 0; cx < cw; cx++)
                {
                    bool any = false;
                    int x0 = cx * bs, y0 = cy * bs;
                    int x1 = Math.Min(x0 + bs, w), y1 = Math.Min(y0 + bs, h);
                    for (int y = y0; y < y1 && !any; y++)
                        for (int x = x0; x < x1; x++)
                            if (density[y * w + x] > 0) { any = true; break; }
                    if (any) c++;
                }
            counts[si] = c;
        }

        double sx = 0, sy = 0, sxx = 0, sxy = 0;
        int k = 0;
        for (int i = 0; i < sizes.Length; i++)
        {
            if (counts[i] == 0) continue;
            double x = Math.Log(1.0 / sizes[i]);
            double y = Math.Log(counts[i]);
            sx += x; sy += y; sxx += x * x; sxy += x * y; k++;
        }
        if (k < 2) return double.NaN;
        return (k * sxy - sx * sy) / (k * sxx - sx * sx);
    }
}

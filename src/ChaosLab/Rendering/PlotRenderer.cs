namespace ChaosLab.Rendering;

/// <summary>
/// Универсальный рендерер 1D-графиков: спектры, MI, FNN, D_q, f(α).
/// </summary>
/// <remarks>
/// Строит сетку 4×4, оси, линию через точки и опциональные маркеры.
/// Автомасштаб по min/max входных данных с 5% отступом.
/// </remarks>
public static class PlotRenderer
{
    /// <summary>Рисует график y(x).</summary>
    /// <param name="pixels">Целевой Bgr32-буфер.</param>
    /// <param name="w">Ширина.</param>
    /// <param name="h">Высота.</param>
    /// <param name="xValues">Значения по X.</param>
    /// <param name="yValues">Значения по Y (NaN = разрыв линии).</param>
    /// <param name="background">Цвет фона.</param>
    /// <param name="lineColor">Цвет линии.</param>
    /// <param name="gridColor">Цвет сетки.</param>
    /// <param name="markerSize">Размер маркеров (0 = без).</param>
    /// <param name="drawMarkers">Рисовать ли маркеры в точках.</param>
    public static void Render(
        int[] pixels, int w, int h,
        double[] xValues, double[] yValues,
        int background, int lineColor, int gridColor,
        int markerSize = 1, bool drawMarkers = false)
    {
        Array.Fill(pixels, background);
        int n = Math.Min(xValues.Length, yValues.Length);
        if (n < 2) return;

        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        for (int i = 0; i < n; i++)
        {
            if (double.IsNaN(yValues[i])) continue;
            if (xValues[i] < xMin) xMin = xValues[i];
            if (xValues[i] > xMax) xMax = xValues[i];
            if (yValues[i] < yMin) yMin = yValues[i];
            if (yValues[i] > yMax) yMax = yValues[i];
        }
        if (xMin >= xMax || yMin >= yMax) return;

        // Отступы
        double xPad = (xMax - xMin) * 0.05;
        double yPad = (yMax - yMin) * 0.05;
        xMin -= xPad; xMax += xPad;
        yMin -= yPad; yMax += yPad;

        // Сетка 4×4
        for (int i = 0; i <= 4; i++)
        {
            int gx = i * (w - 1) / 4;
            for (int y = 0; y < h; y++) pixels[y * w + gx] = gridColor;
            int gy = i * (h - 1) / 4;
            for (int x = 0; x < w; x++) pixels[gy * w + x] = gridColor;
        }

        int prevPx = -1, prevPy = -1;
        for (int i = 0; i < n; i++)
        {
            if (double.IsNaN(yValues[i])) { prevPx = -1; continue; }
            int px = (int)((xValues[i] - xMin) / (xMax - xMin) * (w - 1));
            int py = (int)((yMax - yValues[i]) / (yMax - yMin) * (h - 1));

            if (prevPx >= 0)
                DrawLine(pixels, w, h, prevPx, prevPy, px, py, lineColor);

            if (drawMarkers)
            {
                int m = Math.Max(1, markerSize);
                for (int dy = -m; dy <= m; dy++)
                    for (int dx = -m; dx <= m; dx++)
                    {
                        int xx = px + dx, yy = py + dy;
                        if ((uint)xx < (uint)w && (uint)yy < (uint)h)
                            pixels[yy * w + xx] = lineColor;
                    }
            }

            prevPx = px; prevPy = py;
        }
    }

    private static void DrawLine(int[] px, int w, int h, int x1, int y1, int x2, int y2, int color)
    {
        int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            if ((uint)x1 < (uint)w && (uint)y1 < (uint)h) px[y1 * w + x1] = color;
            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x1 += sx; }
            if (e2 <= dx) { err += dx; y1 += sy; }
        }
    }
}

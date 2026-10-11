using ChaosLab.Core;

namespace ChaosLab.Rendering;

/// <summary>
/// Рендерер cobweb-диаграмм (паутина итераций) для 1D-отображений.
/// </summary>
/// <remarks>
/// Строит: диагональ y = x, график y = f(x), ломаную итераций
/// x₀ → f(x₀) → f(f(x₀)) → ...
/// </remarks>
public static class CobwebRenderer
{
    /// <summary>Рисует cobweb-диаграмму.</summary>
    /// <param name="pixels">Целевой Bgr32-буфер.</param>
    /// <param name="w">Ширина.</param>
    /// <param name="h">Высота.</param>
    /// <param name="map1D">1D-отображение.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="xMin">Минимум диапазона по X и Y.</param>
    /// <param name="xMax">Максимум диапазона.</param>
    /// <param name="x0">Начальная точка итераций.</param>
    /// <param name="iterations">Число шагов.</param>
    /// <param name="palette">Палитра (используются фиксированные цвета).</param>
    public static void Render(
        int[] pixels, int w, int h,
        ChaoticSystem map1D, double[] p,
        double xMin, double xMax, double x0,
        int iterations, Palette palette)
    {
        Array.Clear(pixels);
        if (map1D.Dimension != 1) return;

        // Цвета
        const int colDiagonal = 0x3F5570;    // приглушённый сине-серый
        const int colFunction = 0x4DA3FF;    // акцент
        const int colTrajectory = 0xF8B755;  // тёплый жёлтый

        // y диапазон = x диапазон (для f(x) = x диагонали)
        double yMin = xMin, yMax = xMax;

        // Диагональ y = x
        for (int x = 0; x < w; x++)
        {
            double xv = xMin + (xMax - xMin) * x / (w - 1);
            int py = (int)((yMax - xv) / (yMax - yMin) * (h - 1));
            if ((uint)py < (uint)h) pixels[py * w + x] = colDiagonal;
        }

        // Функция y = f(x)
        var s = new double[1];
        var n = new double[1];
        for (int x = 0; x < w; x++)
        {
            double xv = xMin + (xMax - xMin) * x / (w - 1);
            s[0] = xv;
            map1D.Next(s, n, p);
            double fv = n[0];
            if (fv < yMin || fv > yMax) continue;
            int py = (int)((yMax - fv) / (yMax - yMin) * (h - 1));
            if ((uint)py < (uint)h) pixels[py * w + x] = colFunction;
        }

        // Ломаная итераций (cobweb)
        double cur = x0;
        for (int i = 0; i < iterations; i++)
        {
            s[0] = cur;
            map1D.Next(s, n, p);
            double next = n[0];

            DrawLine(pixels, w, h, xMin, xMax, yMin, yMax, cur, cur, cur, next, colTrajectory);
            DrawLine(pixels, w, h, xMin, xMax, yMin, yMax, cur, next, next, next, colTrajectory);
            cur = next;
        }
    }

    private static void DrawLine(int[] px, int w, int h,
        double xMin, double xMax, double yMin, double yMax,
        double x1v, double y1v, double x2v, double y2v, int color)
    {
        int x1 = (int)((x1v - xMin) / (xMax - xMin) * (w - 1));
        int y1 = (int)((yMax - y1v) / (yMax - yMin) * (h - 1));
        int x2 = (int)((x2v - xMin) / (xMax - xMin) * (w - 1));
        int y2 = (int)((yMax - y2v) / (yMax - yMin) * (h - 1));

        int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            if ((uint)x1 < (uint)w && (uint)y1 < (uint)h)
                px[y1 * w + x1] = color;
            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x1 += sx; }
            if (e2 <= dx) { err += dx; y1 += sy; }
        }
    }
}

using ChaosLab.Core;

namespace ChaosLab.Rendering;

/// <summary>
/// Рендерер слоя векторного поля над аттрактором.
/// </summary>
/// <remarks>
/// Для 2D отображений рисует стрелки в узлах сетки, показывающие
/// направление f(x, y) − (x, y). Полезно для понимания топологии потока
/// одновременно с аттрактором.
/// </remarks>
public static class VectorFieldRenderer
{
    /// <summary>Рисует слой векторного поля поверх существующих пикселей.</summary>
    /// <param name="pixels">Целевой буфер (не очищается).</param>
    /// <param name="w">Ширина.</param>
    /// <param name="h">Высота.</param>
    /// <param name="sys2D">2D отображение.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="cam">Камера.</param>
    /// <param name="xMin">Минимум окна по X.</param>
    /// <param name="xMax">Максимум.</param>
    /// <param name="yMin">Минимум по Y.</param>
    /// <param name="yMax">Максимум.</param>
    /// <param name="arrowsX">Стрелок по X.</param>
    /// <param name="arrowsY">Стрелок по Y.</param>
    /// <param name="arrowLen">Длина стрелки.</param>
    /// <param name="color">Цвет стрелок.</param>
    public static void RenderOver(
        int[] pixels, int w, int h,
        ChaoticSystem sys2D, double[] p, Camera3D cam,
        double xMin, double xMax, double yMin, double yMax,
        int arrowsX = 32, int arrowsY = 32,
        double arrowLen = 0.4,
        int color = 0x4DA3FF)
    {
        if (sys2D.Dimension != 2 || !sys2D.IsDiscrete) return;

        var s = new double[2];
        var n = new double[2];

        for (int j = 0; j < arrowsY; j++)
        {
            double yv = yMin + (yMax - yMin) * j / (arrowsY - 1);
            for (int i = 0; i < arrowsX; i++)
            {
                double xv = xMin + (xMax - xMin) * i / (arrowsX - 1);
                s[0] = xv; s[1] = yv;
                sys2D.Next(s, n, p);

                double dxv = n[0] - xv;
                double dyv = n[1] - yv;
                double mag = Math.Sqrt(dxv * dxv + dyv * dyv);
                if (mag < 1e-9) continue;

                double scale = arrowLen / (xMax - xMin) * (w * 0.02);
                double x2v = xv + dxv / mag * scale * 30;
                double y2v = yv + dyv / mag * scale * 30;

                var (px1, py1) = cam.Project(xv, yv, 0);
                var (px2, py2) = cam.Project(x2v, y2v, 0);
                DrawLine(pixels, w, h, px1, py1, px2, py2, color);

                // Наконечник
                double ang = Math.Atan2(py2 - py1, px2 - px1);
                int hs = 4;
                for (int k = -1; k <= 1; k += 2)
                {
                    double a = ang + k * 2.5;
                    int tx = px2 + (int)(hs * Math.Cos(a));
                    int ty = py2 + (int)(hs * Math.Sin(a));
                    DrawLine(pixels, w, h, px2, py2, tx, ty, color);
                }
            }
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

using ChaosLab.Analysis;
using ChaosLab.Core;

namespace ChaosLab.Rendering;

/// <summary>
/// Рендерер фазового портрета аттрактора с накоплением плотности.
/// </summary>
/// <remarks>
/// <para>Каждая точка траектории проецируется камерой на холст, инкрементирует
/// пиксель в буфере плотности (ushort). По завершении — colorize с лог-масштабированием
/// через палитру.</para>
/// <para>Лог-шкала критична: без неё плотное ядро аттрактора «пережигает» цвет
/// и теряет детали.</para>
/// </remarks>
public sealed class AttractorRenderer
{
    /// <summary>Ширина холста в пикселях.</summary>
    public int Width { get; }

    /// <summary>Высота холста в пикселях.</summary>
    public int Height { get; }

    /// <summary>Bgr32-буфер пикселей размером <c>Width × Height</c>.</summary>
    public int[] Pixels { get; }

    private readonly ushort[] _density;

    /// <summary>Создаёт рендерер с указанными размерами.</summary>
    /// <param name="w">Ширина.</param>
    /// <param name="h">Высота.</param>
    public AttractorRenderer(int w, int h)
    {
        Width = w; Height = h;
        Pixels = new int[w * h];
        _density = new ushort[w * h];
    }

    /// <summary>Буфер плотности для box-counting и других анализов.</summary>
    public ushort[] Density => _density;

    /// <summary>Обнуляет оба буфера (плотность и пиксели).</summary>
    public void Clear() { Array.Clear(_density); Array.Clear(Pixels); }

    /// <summary>
    /// Продвигает систему на указанное число шагов, рисуя проекции в буфер плотности.
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="state">Текущее состояние (изменяется in-place).</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования (для потоков).</param>
    /// <param name="steps">Число шагов для отрисовки.</param>
    /// <param name="cam">Камера для проекции.</param>
    /// <param name="warmup">Шагов прогрева (не рисуются).</param>
    /// <param name="ct">Токен отмены.</param>
    public void Accumulate(ChaoticSystem sys, double[] state, double[] p,
        double dt, int steps, Camera3D cam, int warmup = 0,
        CancellationToken ct = default)
    {
        int dim = sys.Dimension;
        var next = new double[dim];
        var rk = sys.IsDiscrete ? null : new Rk4Scratch(dim);
        var xyz = new double[3];

        for (int i = 0; i < warmup; i++)
        {
            if ((i & 0x3FFF) == 0 && ct.IsCancellationRequested) return;
            Integrator.StepOnce(sys, state, next, p, dt, rk);
        }

        int w = Width, h = Height;
        var dens = _density;

        if (dim == 1 && sys.IsDiscrete)
        {
            for (int i = 0; i < steps; i++)
            {
                if ((i & 0x3FFF) == 0 && ct.IsCancellationRequested) return;
                double prev = state[0];
                sys.Next(state, next, p);
                state[0] = next[0];
                var (px, py) = cam.Project(prev, state[0], 0);
                Plot(dens, w, h, px, py);
            }
        }
        else if (dim == 2)
        {
            for (int i = 0; i < steps; i++)
            {
                if ((i & 0x3FFF) == 0 && ct.IsCancellationRequested) return;
                sys.Next(state, next, p);
                state[0] = next[0]; state[1] = next[1];
                var (px, py) = cam.Project(state[0], state[1], 0);
                Plot(dens, w, h, px, py);
            }
        }
        else
        {
            for (int i = 0; i < steps; i++)
            {
                if ((i & 0x3FFF) == 0 && ct.IsCancellationRequested) return;
                Integrator.Rk4Step(sys, state, p, dt, rk!);
                sys.Project3D(state, xyz);
                var (px, py) = cam.Project(xyz[0], xyz[1], xyz[2]);
                Plot(dens, w, h, px, py);
            }
        }
    }

    /// <summary>
    /// Проецирует одну точку фазового пространства и инкрементирует её
    /// пиксель в буфере плотности. Используется в режимах Trail, Adaptive RK45
    /// и Verlet, где нельзя применить пакетный <see cref="Accumulate"/>.
    /// </summary>
    /// <param name="x">X-координата.</param>
    /// <param name="y">Y-координата.</param>
    /// <param name="z">Z-координата (для 2D передаётся 0).</param>
    /// <param name="cam">Камера для проекции.</param>
    public void PlotPoint(double x, double y, double z, Camera3D cam)
    {
        var (px, py) = cam.Project(x, y, z);
        if ((uint) px < (uint) Width && (uint) py < (uint) Height)
        {
            int idx = py * Width + px;
            if (_density[idx] < ushort.MaxValue) _density[idx]++;
        }
    }

    private static void Plot(ushort[] dens, int w, int h, int x, int y)
    {
        if ((uint)x < (uint)w && (uint)y < (uint)h)
        {
            int idx = y * w + x;
            if (dens[idx] < ushort.MaxValue) dens[idx]++;
        }
    }

    private const int LogLutSize = 4096;
    private static readonly double[] _logLut = BuildLogLut(LogLutSize);

    private static double[] BuildLogLut(int n)
    {
        var lut = new double[n];
        for (int i = 0; i < n; i++)
            lut[i] = Math.Log(1.0 + i * (65535.0 / (n - 1)));
        return lut;
    }

    /// <summary>
    /// Преобразует накопленную плотность в Bgr32-пиксели через палитру.
    /// </summary>
    /// <param name="palette">Палитра.</param>
    /// <param name="gamma">Гамма-коррекция (по умолчанию 1.0).</param>
    /// <param name="background">Цвет фона для пустых пикселей.</param>
    public void Colorize(Palette palette, double gamma = 1.0, int background = 0x000000)
    {
        int n = _density.Length;
        ushort maxD = 0;
        for (int i = 0; i < n; i++) if (_density[i] > maxD) maxD = _density[i];

        if (maxD == 0) { Array.Fill(Pixels, background); return; }

        // LUT вместо Math.Log в цикле
        int maxIdx = maxD * (LogLutSize - 1) / 65535;
        double logMax = _logLut[maxIdx];
        if (logMax < 1e-12) { Array.Fill(Pixels, background); return; }

        bool applyGamma = Math.Abs(gamma - 1.0) > 1e-6;
        var dens = _density;
        var px = Pixels;

        for (int i = 0; i < n; i++)
        {
            ushort d = dens[i];
            if (d == 0) { px[i] = background; continue; }

            int lutIdx = d * (LogLutSize - 1) / 65535;
            double norm = _logLut[lutIdx] / logMax;
            if (applyGamma) norm = Math.Pow(norm, gamma);
            px[i] = palette.Sample(norm);
        }
    }

    /// <summary>Отрисовать сечение Пуанкаре поверх чистого холста.</summary>
    public void DrawPoincare(PoincareResult poincare, Camera3D cam, Palette palette)
    {
        Clear();
        if (poincare.Count == 0) return;
        int w = Width, h = Height;
        var dens = _density;
        for (int i = 0; i < poincare.Count; i++)
        {
            var (px, py) = cam.Project(poincare.X[i], poincare.Y[i], 0);
            Plot(dens, w, h, px, py);
        }
        Colorize(palette);
    }

    /// <summary>Отрисовать матрицу возвратов (RQA plot).</summary>
    public void DrawRqaMatrix(int[,] matrix, int size, Palette palette)
    {
        Array.Clear(Pixels);
        if (size == 0) return;
        int w = Width, h = Height;
        for (int y = 0; y < h; y++)
        {
            int my = y * size / h;
            for (int x = 0; x < w; x++)
            {
                int mx = x * size / w;
                if (mx < size && my < size && matrix[my, mx] == 1)
                    Pixels[y * w + x] = 0x7DDBFF;
                else
                    Pixels[y * w + x] = 0x000000;
            }
        }
    }

    /// <summary>Отрисовать бассейны притяжения.</summary>
    public void DrawBasins(BasinResult basins, Palette palette)
    {
        Array.Clear(Pixels);
        if (basins.Labels.Length == 0) return;
        int w = Width, h = Height;
        for (int y = 0; y < h; y++)
        {
            int by = y * basins.Height / h;
            for (int x = 0; x < w; x++)
            {
                int bx = x * basins.Width / w;
                int lbl = basins.Labels[by * basins.Width + bx];
                double t = basins.AttractorCount > 1
                    ? (double)lbl / (basins.AttractorCount - 1) : 0;
                Pixels[y * w + x] = palette.Sample(t);
            }
        }
    }
}

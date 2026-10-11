using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Реакционно-диффузионная система Грея–Скотта.
/// </summary>
/// <remarks>
/// <code>
/// ∂u/∂t = Du·∇²u − uv² + F(1 − u)
/// ∂v/∂t = Dv·∇²v + uv² − (F + k)v
/// </code>
/// <para>2D-поле на сетке 256×256 с периодическими границами. Явная схема
/// Эйлера. Не хаотична в строгом смысле, но демонстрирует спонтанное
/// формирование паттернов (Pearson 1993).</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1126/science.261.5118.189"/>
public sealed class GrayScottField : ISimulation
{
    /// <summary>Ширина сетки.</summary>
    public int GridW { get; } = 256;

    /// <summary>Высота сетки.</summary>
    public int GridH { get; } = 256;

    /// <summary>Поле концентрации u.</summary>
    public double[] U = Array.Empty<double>();

    /// <summary>Поле концентрации v.</summary>
    public double[] V = Array.Empty<double>();

    private double[] _u2 = Array.Empty<double>();

    private double[] _v2 = Array.Empty<double>();

    private readonly Random _rng = new(12345);

    /// <summary>Шаг интегрирования по времени.</summary>
    public double Dt { get; set; } = 1.0;

    /// <inheritdoc/>
    public string Name => "Gray-Scott RD";

    /// <inheritdoc/>
    public string Category => "Field";

    /// <inheritdoc/>
    public ParameterDef[] Parameters =>
    [
        new("F", 0.010, 0.080, 0.035,
        "Feed rate: скорость поступления «пищи» u. " +
        "Вместе с k определяет режим паттерна."),
        new("k", 0.040, 0.070, 0.060,
            "Kill rate: скорость удаления автокатализатора v. " +
            "Классические режимы: 0.06 — коралл, 0.062 — черви, 0.051 — лабиринт."),
        new("Du", 0.100, 0.250, 0.160,
            "Коэффициент диффузии u. Должен быть больше Dv в 1.5–3×."),
        new("Dv", 0.040, 0.120, 0.080,
            "Коэффициент диффузии v. Слишком близко к Du — система вырождается " +
            "в однородное состояние."),
    ];

    /// <summary>Создаёт поле и инициализирует его.</summary>
    public GrayScottField()
    {
        Allocate();
        Reset();
    }

    private void Allocate()
    {
        int n = GridW * GridH;
        U = new double[n]; V = new double[n];
        _u2 = new double[n]; _v2 = new double[n];
    }

    /// <summary>
    /// Сбрасывает поле в начальное состояние: u = 1, v = 0 плюс 24 случайных
    /// «затравки» для инициализации паттернов.
    /// </summary>
    public void Reset()
    {
        Array.Fill(U, 1.0);
        Array.Clear(V);

        for (int s = 0; s < 24; s++)
        {
            int cx = _rng.Next(15, GridW - 15);
            int cy = _rng.Next(15, GridH - 15);
            int r = _rng.Next(4, 12);
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if ((uint) x >= GridW || (uint) y >= GridH) continue;
                    double dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r * r)
                    {
                        int i = y * GridW + x;
                        U[i] = 0.5;
                        V[i] = 0.25;
                    }
                }
        }
    }

    /// <summary>Продвигает поле на <paramref name="n"/> шагов.</summary>
    /// <param name="n">Число шагов.</param>
    /// <param name="p">Параметры [F, k, Du, Dv].</param>
    /// <param name="ct">Токен отмены.</param>
    public void Step(int n, double[] p, CancellationToken ct = default)
    {
        double F = p[0], K = p[1], Du = p[2], Dv = p[3];
        int w = GridW, h = GridH;
        double dt = Dt;

        for (int s = 0; s < n; s++)
        {
            if ((s & 0xF) == 0 && ct.IsCancellationRequested) return;

            for (int y = 0; y < h; y++)
            {
                int yp = (y == 0) ? h - 1 : y - 1;
                int yn = (y == h - 1) ? 0 : y + 1;
                int row = y * w;
                int rowp = yp * w;
                int rown = yn * w;

                for (int x = 0; x < w; x++)
                {
                    int xp = (x == 0) ? w - 1 : x - 1;
                    int xn = (x == w - 1) ? 0 : x + 1;

                    double u = U[row + x];
                    double v = V[row + x];

                    double lapU = U[rowp + x] + U[rown + x] + U[row + xp] + U[row + xn] - 4.0 * u;
                    double lapV = V[rowp + x] + V[rown + x] + V[row + xp] + V[row + xn] - 4.0 * v;

                    double uvv = u * v * v;
                    _u2[row + x] = u + dt * (Du * lapU - uvv + F * (1.0 - u));
                    _v2[row + x] = v + dt * (Dv * lapV + uvv - (F + K) * v);
                }
            }

            (U, _u2) = (_u2, U);
            (V, _v2) = (_v2, V);
        }
    }

    /// <summary>Отрисовывает V-поле в Bgr32-буфер с nearest-neighbor апскейлом.</summary>
    public void RenderTo(int[] pixels, int outW, int outH, Palette palette)
    {
        double vmin = double.MaxValue, vmax = double.MinValue;
        for (int i = 0; i < V.Length; i++)
        {
            if (V[i] < vmin) vmin = V[i];
            if (V[i] > vmax) vmax = V[i];
        }
        double range = vmax - vmin;
        if (range < 1e-10) range = 1.0;

        double sx = (double) GridW / outW;
        double sy = (double) GridH / outH;

        for (int y = 0; y < outH; y++)
        {
            int gy = (int) (y * sy);
            if (gy >= GridH) gy = GridH - 1;
            int rowOut = y * outW;
            int rowIn = gy * GridW;
            for (int x = 0; x < outW; x++)
            {
                int gx = (int) (x * sx);
                if (gx >= GridW) gx = GridW - 1;
                double t = (V[rowIn + gx] - vmin) / range;
                pixels[rowOut + x] = palette.Sample(t);
            }
        }
    }
}

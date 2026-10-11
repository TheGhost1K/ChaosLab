using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат построения карты бассейнов притяжения.
/// </summary>
public sealed class BasinResult
{
    /// <summary>Ширина карты.</summary>
    public int Width;
    /// <summary>Высота карты.</summary>
    public int Height;
    /// <summary>Метки аттракторов для каждого пикселя (0..k−1).</summary>
    public int[] Labels = Array.Empty<int>();
    /// <summary>Левая граница окна начальных условий.</summary>
    public double XLo;
    /// <summary>Правая граница окна.</summary>
    public double XHi;
    /// <summary>Нижняя граница окна.</summary>
    public double YLo;
    /// <summary>Верхняя граница окна.</summary>
    public double YHi;
    /// <summary>Число найденных аттракторов.</summary>
    public int AttractorCount;
}

/// <summary>
/// Карта бассейнов притяжения — какой аттрактор притягивает каждую
/// точку начальных условий.
/// </summary>
/// <remarks>
/// <para>Для каждой точки (x₀, y₀) в окне:</para>
/// <list type="number">
/// <item><description>Продвигаем систему на transient шагов.</description></item>
/// <item><description>Усредняем координаты за minSamples шагов — получаем
/// «центр» аттрактора.</description></item>
/// <item><description>Кластеризуем центры по порогу tol.</description></item>
/// </list>
/// <para>Границы бассейнов хаотических систем **фрактальны** — при
/// увеличении видна бесконечная структура.</para>
/// <para>Параллелится по строкам; кластеризация выполняется последовательно
/// после вычисления средних.</para>
/// </remarks>
public static class BasinOfAttraction
{
    /// <summary>
    /// Строит карту бассейнов.
    /// </summary>
    /// <param name="sys">Система (поток размерности ≥ 2).</param>
    /// <param name="baseParams">Параметры (не варьируются).</param>
    /// <param name="xLo">Левая граница окна начальных условий.</param>
    /// <param name="xHi">Правая граница.</param>
    /// <param name="yLo">Нижняя граница.</param>
    /// <param name="yHi">Верхняя граница.</param>
    /// <param name="width">Разрешение по X.</param>
    /// <param name="height">Разрешение по Y.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="transient">Шагов до усреднения.</param>
    /// <param name="minSamples">Шагов усреднения для нахождения центра.</param>
    /// <param name="ct">Токен отмены.</param>
    public static Task<BasinResult> ComputeAsync(
        ChaoticSystem sys, double[] baseParams,
        double xLo, double xHi, double yLo, double yHi,
        int width, int height, double dt,
        int transient = 3000, int minSamples = 200,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            if (sys.IsDiscrete || sys.Dimension < 2)
                return new BasinResult { Labels = Array.Empty<int>(), AttractorCount = 0 };

            int total = width * height;
            // Фаза 1 (параллельно): вычислить средние точки аттракторов для каждого пикселя
            var means = new double[total * 3];
            int dim = sys.Dimension;

            Parallel.For(0, height,
                () => new LocalScratch(dim),
                (py, state, local) =>
                {
                    if (ct.IsCancellationRequested) { state.Stop(); return local; }

                    for (int px = 0; px < width; px++)
                    {
                        double x0 = xLo + (xHi - xLo) * px / (width - 1);
                        double y0 = yLo + (yHi - yLo) * py / (height - 1);

                        Array.Copy(sys.DefaultInitialStateCached, local.S, dim);
                        local.S[0] = x0; local.S[1] = y0;

                        for (int i = 0; i < transient; i++)
                            Integrator.StepOnce(sys, local.S, local.Next, baseParams, dt, local.Rk);

                        double sx = 0, sy = 0, sz = 0;
                        for (int i = 0; i < minSamples; i++)
                        {
                            Integrator.StepOnce(sys, local.S, local.Next, baseParams, dt, local.Rk);
                            sx += local.S[0]; sy += local.S[1]; sz += local.S[2];
                        }

                        int idx = (py * width + px) * 3;
                        means[idx] = sx / minSamples;
                        means[idx + 1] = sy / minSamples;
                        means[idx + 2] = sz / minSamples;
                    }
                    return local;
                },
                _ => { });

            // Фаза 2 (последовательно, дёшево): кластеризация по средним точкам
            var labels = new int[total];
            var centers = new List<double[]>();
            const double tol = 1.0;
            const double tol2 = tol * tol * 10;

            for (int i = 0; i < total; i++)
            {
                if (ct.IsCancellationRequested) return new BasinResult { AttractorCount = 0 };
                int mi = i * 3;
                double mx = means[mi], my = means[mi + 1], mz = means[mi + 2];
                int found = -1;
                for (int c = 0; c < centers.Count; c++)
                {
                    double dx = centers[c][0] - mx;
                    double dy = centers[c][1] - my;
                    double dz = centers[c][2] - mz;
                    if (dx * dx + dy * dy + dz * dz < tol2) { found = c; break; }
                }
                if (found < 0)
                {
                    centers.Add(new[] { mx, my, mz });
                    found = centers.Count - 1;
                }
                labels[i] = found;
            }

            return new BasinResult
            {
                Width = width,
                Height = height,
                Labels = labels,
                XLo = xLo,
                XHi = xHi,
                YLo = yLo,
                YHi = yHi,
                AttractorCount = centers.Count
            };
        });
    }

    private sealed class LocalScratch
    {
        public double[] S, Next;
        public Rk4Scratch Rk;
        public LocalScratch(int dim)
        {
            S = new double[dim];
            Next = new double[dim];
            Rk = new Rk4Scratch(dim);
        }
    }
}

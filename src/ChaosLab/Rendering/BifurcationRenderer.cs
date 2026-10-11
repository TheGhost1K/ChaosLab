using ChaosLab.Core;

namespace ChaosLab.Rendering;

/// <summary>
/// Рендерер бифуркационных диаграмм для отображений и потоков.
/// </summary>
/// <remarks>
/// <para>Для отображений: ось X — параметр, ось Y — значение координаты.
/// Классическое «дерево Фейгенбаума».</para>
/// <para>Для потоков: использует метод локальных максимумов x(t) —
/// эквивалент сечения Пуанкаре.</para>
/// <para>Параллелится по столбцам через <see cref="System.Threading.Tasks.Parallel"/>.</para>
/// </remarks>
public static class BifurcationRenderer
{
    // ================================================================
    // ОТОБРАЖЕНИЯ (параллельно по столбцам)
    // ================================================================

    /// <summary>
    /// Строит бифуркационную диаграмму для дискретного отображения: ось X —
    /// параметр, ось Y — значение координаты.
    /// </summary>
    /// <param name="pixels">Целевой Bgr32-буфер.</param>
    /// <param name="w">Ширина.</param>
    /// <param name="h">Высота.</param>
    /// <param name="sys">Отображение (<c>IsDiscrete = true</c>).</param>
    /// <param name="paramIndex">Индекс варьируемого параметра.</param>
    /// <param name="pMin">Минимум параметра.</param>
    /// <param name="pMax">Максимум параметра.</param>
    /// <param name="baseParams">Значения остальных параметров.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="transient">Шагов прогрева на каждое значение параметра.</param>
    /// <param name="iterations">Шагов отрисовки на каждое значение.</param>
    /// <param name="coordIndex">Индекс координаты для оси Y.</param>
    /// <param name="yLo">Минимум по Y.</param>
    /// <param name="yHi">Максимум по Y.</param>
    /// <param name="palette">Палитра.</param>
    /// <param name="ct">Токен отмены.</param>
    public static void Render(
        int[] pixels, int w, int h,
        ChaoticSystem sys,
        int paramIndex, double pMin, double pMax,
        double[] baseParams, double[] initialState,
        int transient, int iterations, int coordIndex,
        double yLo, double yHi,
        Palette palette,
        CancellationToken ct = default)
    {
        int n = pixels.Length;
        var globalDensity = new int[n];

        Parallel.For(0, w,
            () => new MapLocalState(n, sys.Dimension),
            (x, state, local) =>
            {
                if (ct.IsCancellationRequested) { state.Stop(); return local; }

                double pv = pMin + (pMax - pMin) * x / (w - 1);
                local.P[paramIndex] = pv;

                var s = local.S;
                Array.Copy(initialState, s, s.Length);

                for (int i = 0; i < transient; i++)
                {
                    sys.Next(s, local.Next, local.P);
                    Array.Copy(local.Next, s, s.Length);
                }
                for (int i = 0; i < iterations; i++)
                {
                    sys.Next(s, local.Next, local.P);
                    Array.Copy(local.Next, s, s.Length);
                    double v = s[coordIndex];
                    int py = (int)((yHi - v) / (yHi - yLo) * (h - 1));
                    if ((uint)py < (uint)h)
                    {
                        int idx = py * w + x;
                        if (local.Density[idx] < int.MaxValue) local.Density[idx]++;
                    }
                }
                return local;
            },
            local =>
            {
                lock (globalDensity)
                    for (int i = 0; i < n; i++)
                        globalDensity[i] += local.Density[i];
            });

        ColorizeInt(globalDensity, pixels, palette);
    }

    // ================================================================
    // ПОТОКИ (параллельно по столбцам)
    // ================================================================

    /// <summary>
    /// Строит бифуркационную диаграмму для потока через локальные максимумы
    /// координаты вдоль траектории (эквивалент сечения Пуанкаре).
    /// </summary>
    /// <param name="pixels">Целевой Bgr32-буфер размером <paramref name="w"/> × <paramref name="h"/>.</param>
    /// <param name="w">Ширина холста в пикселях.</param>
    /// <param name="h">Высота холста в пикселях.</param>
    /// <param name="sys">Система (поток, <c>IsDiscrete = false</c>).</param>
    /// <param name="paramIndex">Индекс параметра, разворачиваемого по оси X (0..n−1).</param>
    /// <param name="pMin">Минимальное значение варьируемого параметра (левая граница).</param>
    /// <param name="pMax">Максимальное значение (правая граница).</param>
    /// <param name="baseParams">Значения остальных параметров (не варьируются).</param>
    /// <param name="initialState">Начальное состояние для каждого значения параметра.</param>
    /// <param name="transientSteps">Число шагов ОДУ для прогрева до сбора максимумов.</param>
    /// <param name="captureSteps">Число шагов ОДУ для сбора локальных максимумов.</param>
    /// <param name="coordIndex">Индекс координаты, по которой ищутся максимумы (обычно 0).</param>
    /// <param name="dt">Шаг интегрирования RK4.</param>
    /// <param name="yLo">Минимум диапазона по вертикальной оси (низ диаграммы).</param>
    /// <param name="yHi">Максимум диапазона по вертикальной оси (верх диаграммы).</param>
    /// <param name="palette">Палитра для раскраски плотности.</param>
    /// <param name="ct">Токен отмены. Проверяется каждые 1024 шага.</param>
    public static void RenderFlow(
        int[] pixels, int w, int h,
        ChaoticSystem sys,
        int paramIndex, double pMin, double pMax,
        double[] baseParams, double[] initialState,
        int transientSteps, int captureSteps,
        int coordIndex, double dt,
        double yLo, double yHi,
        Palette palette,
        CancellationToken ct = default)
    {
        int n = pixels.Length;
        var globalDensity = new int[n];

        Parallel.For(0, w,
            () => new FlowLocalState(n, sys.Dimension),
            (x, state, local) =>
            {
                if (ct.IsCancellationRequested) { state.Stop(); return local; }

                double pv = pMin + (pMax - pMin) * x / (w - 1);
                local.P[paramIndex] = pv;

                var s = local.S;
                Array.Copy(initialState, s, s.Length);

                for (int i = 0; i < transientSteps; i++)
                {
                    if ((i & 0x3FF) == 0 && ct.IsCancellationRequested) { state.Stop(); return local; }
                    Integrator.Rk4Step(sys, s, local.P, dt, local.Rk);
                }

                double risingMax = s[coordIndex];
                bool rising = true;

                for (int i = 0; i < captureSteps; i++)
                {
                    if ((i & 0x3FF) == 0 && ct.IsCancellationRequested) { state.Stop(); return local; }

                    Integrator.Rk4Step(sys, s, local.P, dt, local.Rk);
                    double v = s[coordIndex];

                    if (v > risingMax) { risingMax = v; rising = true; }
                    else if (rising)
                    {
                        int py = (int)((yHi - risingMax) / (yHi - yLo) * (h - 1));
                        if ((uint)py < (uint)h)
                        {
                            int idx = py * w + x;
                            if (local.Density[idx] < int.MaxValue) local.Density[idx]++;
                        }
                        rising = false;
                        risingMax = v;
                    }
                }
                return local;
            },
            local =>
            {
                lock (globalDensity)
                    for (int i = 0; i < n; i++)
                        globalDensity[i] += local.Density[i];
            });

        ColorizeInt(globalDensity, pixels, palette);
    }

    // ================================================================
    private sealed class MapLocalState
    {
        public int[] Density;
        public double[] P;
        public double[] S;
        public double[] Next;
        public MapLocalState(int bufSize, int dim)
        {
            Density = new int[bufSize];
            P = new double[8];
            S = new double[dim];
            Next = new double[dim];
        }
    }

    private sealed class FlowLocalState
    {
        public int[] Density;
        public double[] P;
        public double[] S;
        public Rk4Scratch Rk;
        public FlowLocalState(int bufSize, int dim)
        {
            Density = new int[bufSize];
            P = new double[8];
            S = new double[dim];
            Rk = new Rk4Scratch(dim);
        }
    }

    private static void ColorizeInt(int[] density, int[] pixels, Palette palette)
    {
        int n = pixels.Length;
        int maxD = 0;
        for (int i = 0; i < n; i++) if (density[i] > maxD) maxD = density[i];
        if (maxD == 0) { Array.Clear(pixels); return; }
        double logMax = Math.Log(1.0 + maxD);
        for (int i = 0; i < n; i++)
        {
            int d = density[i];
            if (d == 0) { pixels[i] = 0; continue; }
            double t = Math.Log(1.0 + d) / logMax;
            pixels[i] = palette.Sample(t);
        }
    }
}

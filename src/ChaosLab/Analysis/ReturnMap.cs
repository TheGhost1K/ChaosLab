using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат построения возвратного отображения.
/// </summary>
public sealed class ReturnMapResult
{
    /// <summary>Значения xₙ (локальные максимумы).</summary>
    public double[] X = Array.Empty<double>();
    /// <summary>Значения xₙ₊₁ (следующие локальные максимумы).</summary>
    public double[] Y = Array.Empty<double>();
    /// <summary>Число точек.</summary>
    public int Count;
    /// <summary>Минимум по X.</summary>
    public double MinX;
    /// <summary>Максимум по X.</summary>
    public double MaxX;
    /// <summary>Минимум по Y.</summary>
    public double MinY;
    /// <summary>Максимум по Y.</summary>
    public double MaxY;
}

/// <summary>
/// Возвратное отображение xₙ → xₙ₊₁ — редукция потока к 1D-отображению.
/// </summary>
/// <remarks>
/// <para>Для потоков: собираем локальные максимумы координаты вдоль
/// траектории. Для отображений: подряд идущие значения.</para>
/// <para>Для Лоренца даёт **тентоподобную кривую**, топологически похожую
/// на логистическую параболу — это фундаментальное свойство системы.</para>
/// </remarks>
public static class ReturnMap
{
    /// <summary>Строит возвратное отображение.</summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="coordIndex">Индекс координаты для анализа.</param>
    /// <param name="maxPoints">Максимум точек.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="ct">Токен отмены.</param>
    public static Task<ReturnMapResult> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int coordIndex = 0,
        int maxPoints = 3000, int transient = 5000,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var xs = new List<double>();
            var ys = new List<double>();

            if (sys.IsDiscrete)
            {
                var s = (double[])initialState.Clone();
                var next = new double[s.Length];
                double prev = s[coordIndex];
                for (int i = 0; i < maxPoints + 1; i++)
                {
                    if ((i & 0x3FF) == 0 && ct.IsCancellationRequested) break;
                    sys.Next(s, next, p);
                    Array.Copy(next, s, s.Length);
                    if (i > 0) { xs.Add(prev); ys.Add(s[coordIndex]); }
                    prev = s[coordIndex];
                }
            }
            else
            {
                int dim = sys.Dimension;
                var s = (double[])initialState.Clone();
                var rk = new Rk4Scratch(dim);

                for (int i = 0; i < transient; i++)
                    Integrator.Rk4Step(sys, s, p, dt, rk);

                // Трёхточечный детектор локального максимума:
                //   x[i-1] > x[i-2] && x[i-1] >= x[i]  → x[i-1] это локальный максимум.
                // O(1) на шаг, без состояния — не может «застрять».
                double prevPrev = s[coordIndex];
                double prev = s[coordIndex];
                double prevPeak = 0;
                bool hasPrevPeak = false;

                for (int i = 0; i < 20_000_000 && xs.Count < maxPoints; i++)
                {
                    if ((i & 0xFFF) == 0 && ct.IsCancellationRequested) break;
                    Integrator.Rk4Step(sys, s, p, dt, rk);
                    double v = s[coordIndex];

                    if (prev > prevPrev && prev >= v)
                    {
                        if (hasPrevPeak)
                        {
                            xs.Add(prevPeak);
                            ys.Add(prev);
                        }
                        prevPeak = prev;
                        hasPrevPeak = true;
                    }

                    prevPrev = prev;
                    prev = v;
                }
            }

            if (xs.Count == 0) return new ReturnMapResult();

            double minx = double.MaxValue, maxx = double.MinValue;
            double miny = double.MaxValue, maxy = double.MinValue;
            for (int i = 0; i < xs.Count; i++)
            {
                if (xs[i] < minx) minx = xs[i];
                if (xs[i] > maxx) maxx = xs[i];
                if (ys[i] < miny) miny = ys[i];
                if (ys[i] > maxy) maxy = ys[i];
            }

            return new ReturnMapResult
            {
                X = xs.ToArray(),
                Y = ys.ToArray(),
                Count = xs.Count,
                MinX = minx,
                MaxX = maxx,
                MinY = miny,
                MaxY = maxy
            };
        });
    }
}

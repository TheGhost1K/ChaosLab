using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат построения сечения Пуанкаре — координаты точек пересечения.
/// </summary>
public sealed class PoincareResult
{
    /// <summary>X-координаты точек пересечения.</summary>
    public double[] X = Array.Empty<double>();
    /// <summary>Y-координаты точек пересечения.</summary>
    public double[] Y = Array.Empty<double>();
    /// <summary>Число собранных точек.</summary>
    public int Count;
}

/// <summary>
/// Сечение Пуанкаре — пересечение траектории с плоскостью z = z₀.
/// </summary>
/// <remarks>
/// <para>Плоскость задаётся значением zPlane и направлением sign (±1).
/// Учитываются только пересечения в правильном направлении.</para>
/// <para>Между соседними шагами интегрирования используется **линейная
/// интерполяция** для точки пересечения — критично для точности.</para>
/// <para>Для Лоренца сечение z = 27 даёт фрактальное множество «перьев»,
/// визуально напоминающее логистическую параболу.</para>
/// </remarks>
public static class PoincareSection
{
    /// <summary>
    /// Строит сечение Пуанкаре.
    /// </summary>
    /// <param name="sys">Система (поток размерности ≥ 3).</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="zPlane">Значение z плоскости сечения.</param>
    /// <param name="sign">Направление пересечения (+1 или −1).</param>
    /// <param name="maxPoints">Максимум собранных точек.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><see cref="PoincareResult"/>.</returns>
    public static Task<PoincareResult> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, double zPlane, int sign = +1,
        int maxPoints = 5000, int transient = 5000,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            if (sys.IsDiscrete || sys.Dimension < 3)
                return new PoincareResult();

            int n = sys.Dimension;
            var x = (double[])initialState.Clone();
            var prev = new double[n];
            var rk = new Rk4Scratch(n);

            for (int i = 0; i < transient; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested)
                    return new PoincareResult();
                Integrator.Rk4Step(sys, x, p, dt, rk);
            }

            var xs = new List<double>();
            var ys = new List<double>();

            Array.Copy(x, prev, n);

            for (int i = 0; i < 10_000_000 && xs.Count < maxPoints; i++)
            {
                if ((i & 4095) == 0 && ct.IsCancellationRequested)
                    return new PoincareResult();
                Integrator.Rk4Step(sys, x, p, dt, rk);

                double prevZ = prev[2];
                double z = x[2];
                bool crosses = (prevZ - zPlane) * (z - zPlane) < 0;
                bool rightDir = Math.Sign(z - prevZ) == sign;

                if (crosses && rightDir)
                {
                    double t = (zPlane - prevZ) / (z - prevZ);
                    double xc = prev[0] + t * (x[0] - prev[0]);
                    double yc = prev[1] + t * (x[1] - prev[1]);
                    xs.Add(xc);
                    ys.Add(yc);
                }

                Array.Copy(x, prev, n);
            }

            return new PoincareResult { X = xs.ToArray(), Y = ys.ToArray(), Count = xs.Count };
        });
    }
}

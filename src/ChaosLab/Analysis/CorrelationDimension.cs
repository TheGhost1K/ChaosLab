using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат вычисления корреляционной размерности.
/// </summary>
public struct CorrelationResult
{
    /// <summary>Корреляционная размерность D₂. <see cref="double.NaN"/>, если данных мало.</summary>
    public double D2;
    /// <summary>Коэффициент детерминации R² линейной регрессии log C vs log r.</summary>
    public double RSquared;

    /// <summary>Число точек, использованных в регрессии.</summary>
    public int PointsUsed;

    /// <summary>Радиусы, для которых вычислена C(r).</summary>
    public double[] R;

    /// <summary>Корреляционные суммы C(r).</summary>
    public double[] C;

    /// <summary>Пустой результат — при недостатке данных.</summary>
    public static readonly CorrelationResult Empty = new()
    {
        D2 = double.NaN,
        RSquared = 0,
        PointsUsed = 0,
        R = Array.Empty<double>(),
        C = Array.Empty<double>()
    };
}

/// <summary>
/// Корреляционная размерность D₂ по алгоритму Грассбергера–Прокаччиа (1983).
/// </summary>
/// <remarks>
/// <para>Формула:</para>
/// <code>
/// C(r) = 2/(N(N−1)) · #{ (i,j) : |xᵢ − xⱼ| &lt; r }
/// D₂ = lim_{r→0} d log C(r) / d log r
/// </code>
/// <para>Восстановление фазового пространства — через delay-embedding (Тейкенс).
/// Гистограмма попарных расстояний в 64 лог-бинах, линейная регрессия в области
/// 0.005 &lt; C &lt; 0.5.</para>
/// <para>Проверка: Лоренц → D₂ ≈ 2.06, Hénon → D₂ ≈ 1.26.</para>
/// <para>Параллелится по строкам через <c>Parallel.For</c> с локальными
/// гистограммами на поток.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1103/PhysRevLett.50.346"/>
public static class CorrelationDimension
{
    /// <summary>
    /// Вычисляет корреляционную размерность D₂.
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования (для потоков).</param>
    /// <param name="sampleCount">Число точек траектории для анализа.</param>
    /// <param name="embeddingDim">Размерность вложения m.</param>
    /// <param name="delay">Задержка вложения τ.</param>
    /// <param name="transient">Шагов прогрева до сбора точек.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>
    /// <see cref="CorrelationResult"/> с полями D₂, R², R[], C[].
    /// </returns>
    public static Task<CorrelationResult> ComputeAsync(
    ChaoticSystem sys, double[] initialState, double[] p,
    double dt, int sampleCount = 3000,
    int embeddingDim = 3, int delay = 10, int transient = 2000,
    CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            int dim = sys.Dimension;
            var x = (double[])initialState.Clone();
            var xNext = new double[dim];
            var rk = sys.IsDiscrete ? null : new Rk4Scratch(dim);

            for (int i = 0; i < transient; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested)
                    return CorrelationResult.Empty;
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
            }

            int total = sampleCount + (embeddingDim - 1) * delay;
            var series = new double[total];
            for (int i = 0; i < total; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested)
                    return CorrelationResult.Empty;
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
                series[i] = x[0];
            }

            int N = sampleCount;
            var pts = new double[N * embeddingDim];
            for (int i = 0; i < N; i++)
                for (int d = 0; d < embeddingDim; d++)
                    pts[i * embeddingDim + d] = series[i + d * delay];

            // Параллельный проход 1: min/max расстояний
            double globalMin = double.MaxValue, globalMax = 0.0;
            var minLock = new object();

            Parallel.For(0, N,
                () => (double.MaxValue, 0.0),
                (i, state, local) =>
                {
                    if (ct.IsCancellationRequested) { state.Stop(); return local; }

                    double lmin = local.MaxValue, lmax = local.Item2;
                    int ii = i * embeddingDim;
                    for (int j = i + 1; j < N; j++)
                    {
                        int jj = j * embeddingDim;
                        double d2 = 0.0;
                        for (int k = 0; k < embeddingDim; k++)
                        {
                            double diff = pts[ii + k] - pts[jj + k];
                            d2 += diff * diff;
                        }
                        double d = Math.Sqrt(d2);
                        if (d > 1e-12)
                        {
                            if (d < lmin) lmin = d;
                            if (d > lmax) lmax = d;
                        }
                    }
                    return (lmin, lmax);
                },
                local =>
                {
                    lock (minLock)
                    {
                        if (local.MaxValue < globalMin) globalMin = local.MaxValue;
                        if (local.Item2 > globalMax) globalMax = local.Item2;
                    }
                });

            double dmin = globalMin, dmax = globalMax;
            if (dmin >= dmax || dmax <= 0) return CorrelationResult.Empty;

            const int nBins = 64;
            double logMin = Math.Log(dmin);
            double logMax = Math.Log(dmax);
            double binWidth = (logMax - logMin) / nBins;

            // Параллельный проход 2: локальные гистограммы
            var globalHist = new int[nBins];

            Parallel.For(0, N,
                () => new int[nBins],
                (i, state, localHist) =>
                {
                    if (ct.IsCancellationRequested) { state.Stop(); return localHist; }

                    int ii = i * embeddingDim;
                    for (int j = i + 1; j < N; j++)
                    {
                        int jj = j * embeddingDim;
                        double d2 = 0.0;
                        for (int k = 0; k < embeddingDim; k++)
                        {
                            double diff = pts[ii + k] - pts[jj + k];
                            d2 += diff * diff;
                        }
                        double d = Math.Sqrt(d2);
                        if (d < dmin || d > dmax) continue;
                        int bin = (int)((Math.Log(d) - logMin) / binWidth);
                        if (bin < 0) bin = 0;
                        if (bin >= nBins) bin = nBins - 1;
                        localHist[bin]++;
                    }
                    return localHist;
                },
                localHist =>
                {
                    lock (globalHist)
                        for (int b = 0; b < nBins; b++)
                            globalHist[b] += localHist[b];
                });

            long totalPairs = (long)N * (N - 1) / 2;
            var C = new double[nBins];
            var R = new double[nBins];
            long cum = 0;
            for (int b = 0; b < nBins; b++)
            {
                cum += globalHist[b];
                C[b] = (double)cum / totalPairs;
                R[b] = Math.Exp(logMin + (b + 0.5) * binWidth);
            }

            double sx = 0, sy = 0, sxx = 0, sxy = 0;
            int cnt = 0;
            for (int b = 0; b < nBins; b++)
            {
                if (C[b] < 0.005 || C[b] > 0.5) continue;
                double logR = logMin + (b + 0.5) * binWidth;
                double logC = Math.Log(C[b]);
                sx += logR; sy += logC;
                sxx += logR * logR; sxy += logR * logC;
                cnt++;
            }

            double slope = cnt >= 3 ? (cnt * sxy - sx * sy) / (cnt * sxx - sx * sx) : double.NaN;

            double r2 = 0.0;
            if (cnt >= 3 && !double.IsNaN(slope))
            {
                double meanY = sy / cnt;
                double intercept = (sy - slope * sx) / cnt;
                double ssTot = 0, ssRes = 0;
                for (int b = 0; b < nBins; b++)
                {
                    if (C[b] < 0.005 || C[b] > 0.5) continue;
                    double logR = logMin + (b + 0.5) * binWidth;
                    double logC = Math.Log(C[b]);
                    double pred = slope * logR + intercept;
                    ssTot += (logC - meanY) * (logC - meanY);
                    ssRes += (logC - pred) * (logC - pred);
                }
                if (ssTot > 1e-12) r2 = 1.0 - ssRes / ssTot;
            }

            return new CorrelationResult
            {
                D2 = slope,
                RSquared = r2,
                PointsUsed = cnt,
                R = R,
                C = C
            };
        });
    }
}

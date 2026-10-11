using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат мультифрактального анализа.
/// </summary>
public sealed class MultifractalResult
{
    /// <summary>Значения параметра q.</summary>
    public double[] Q = Array.Empty<double>();
    /// <summary>Обобщённые размерности D_q.</summary>
    public double[] Dq = Array.Empty<double>();
    /// <summary>Спектр сингулярностей α.</summary>
    public double[] Alpha = Array.Empty<double>();
    /// <summary>Спектр f(α).</summary>
    public double[] FAlpha = Array.Empty<double>();
}

/// <summary>
/// Мультифрактальный анализ: обобщённые размерности D_q и спектр f(α).
/// </summary>
/// <remarks>
/// <para>Обобщённые корреляционные суммы:</para>
/// <code>
/// C_q(r) = [1/(N−1) · Σ pᵢ^(q−1)]^(1/(q−1))
/// D_q = lim_{r→0} log C_q(r) / log r
/// </code>
/// <para>Спектр сингулярностей f(α) через преобразование Лежандра:</para>
/// <code>
/// τ(q) = (q−1)·D_q
/// α = dτ/dq
/// f(α) = q·α − τ(q)
/// </code>
/// <para><b>Ширина f(α)</b> — мера мультифрактальности:</para>
/// <list type="bullet">
/// <item><description>Одна точка — монофрактал.</description></item>
/// <item><description>Широкий спектр — мультифрактал.</description></item>
/// </list>
/// <para>D₀ = box-counting, D₁ = информационная, D₂ = корреляционная.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1103/PhysRevA.33.1141"/>
public static class Multifractal
{
    /// <summary>
    /// Вычисляет обобщённые размерности и спектр мультифрактала.
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="sampleCount">Число точек для анализа.</param>
    /// <param name="embeddingDim">Размерность вложения.</param>
    /// <param name="delay">Задержка.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="ct">Токен отмены.</param>
    public static Task<MultifractalResult> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int sampleCount = 2000,
        int embeddingDim = 3, int delay = 10,
        int transient = 2000,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            // Собираем точки через delay-embedding
            int dim = sys.Dimension;
            var x = (double[])initialState.Clone();
            var xNext = new double[dim];
            var rk = sys.IsDiscrete ? null : new Rk4Scratch(dim);

            for (int i = 0; i < transient; i++)
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);

            int total = sampleCount + (embeddingDim - 1) * delay;
            var series = new double[total];
            if (series.Length == 0) return new MultifractalResult();
            for (int i = 0; i < total; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested)
                    return new MultifractalResult();
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
                series[i] = x[0];
            }

            int N = sampleCount;
            var pts = new double[N * embeddingDim];
            for (int i = 0; i < N; i++)
                for (int d = 0; d < embeddingDim; d++)
                    pts[i * embeddingDim + d] = series[i + d * delay];

            // Диаметр аттрактора: используем медиану расстояний от НЕСКОЛЬКИХ
            // опорных точек, а не от одной. Иначе при удачной центральной точке
            // получаем диаметр вдвое меньше истинного.
            double diameter = 0;
            {
                int nProbe = Math.Min(20, N);
                var probeDists = new double[nProbe];
                for (int probe = 0; probe < nProbe; probe++)
                {
                    int i0 = probe * N / nProbe;
                    double maxD2 = 0;
                    for (int j = 0; j < N; j++)
                    {
                        double d2 = 0;
                        for (int k = 0; k < embeddingDim; k++)
                        {
                            double df = pts[i0 * embeddingDim + k] - pts[j * embeddingDim + k];
                            d2 += df * df;
                        }
                        if (d2 > maxD2) maxD2 = d2;
                    }
                    probeDists[probe] = Math.Sqrt(maxD2);
                }
                Array.Sort(probeDists);
                diameter = probeDists[nProbe / 2];   // медиана
            }

            const int nR = 24;
            var rValues = new double[nR];
            double rMin = diameter * 0.02;
            double rMax = diameter * 0.6;
            for (int i = 0; i < nR; i++)
                rValues[i] = rMin * Math.Pow(rMax / rMin, (double)i / (nR - 1));

            var qValues = new double[21];
            for (int i = 0; i < 21; i++) qValues[i] = -5.0 + 0.5 * i;

            var Cq = new double[21, nR];   // корреляционные суммы C_q(r)

            // Для каждого q и r — параллельно по r-индексу
            Parallel.For(0, nR, ri =>
            {
                double r2 = rValues[ri] * rValues[ri];
                // Накапливаем гистограмму расстояний
                var hist = new long[N];
                for (int i = 0; i < N; i++)
                {
                    if ((i & 0x1F) == 0 && ct.IsCancellationRequested) return;
                    int ii = i * embeddingDim;
                    int cnt = 0;
                    for (int j = 0; j < N; j++)
                    {
                        if (i == j) continue;
                        int jj = j * embeddingDim;
                        double d2 = 0;
                        for (int k = 0; k < embeddingDim; k++)
                        {
                            double df = pts[ii + k] - pts[jj + k];
                            d2 += df * df;
                        }
                        if (d2 < r2) cnt++;
                    }
                    hist[i] = cnt;
                }

                for (int qi = 0; qi < 21; qi++)
                {
                    double q = qValues[qi];
                    double sum = 0.0;
                    if (Math.Abs(q - 1.0) < 1e-6)
                    {
                        // q = 1: C_1 = exp( (1/N) Σ log p_i )  — геометрическое среднее
                        double sumLog = 0;
                        int validCount = 0;
                        for (int i = 0; i < N; i++)
                        {
                            double p = hist[i] / (double) (N - 1);
                            if (p > 0) { sumLog += Math.Log(p); validCount++; }
                        }
                        sum = validCount > 0
                            ? Math.Exp(sumLog / validCount)
                            : 0;
                    }
                    else
                    {
                        // Общая формула: C_q = [ (1/N) Σ p_i^(q−1) ]^(1/(q−1))
                        // ← ключевое: q−1, не q
                        double invQ = 1.0 / (q - 1.0);
                        for (int i = 0; i < N; i++)
                        {
                            double p = hist[i] / (double) (N - 1);
                            if (p > 0)
                                sum += Math.Pow(p, q - 1.0);   // ← было p^q
                        }
                        sum = Math.Pow(sum / N, invQ);
                    }
                    Cq[qi, ri] = sum;
                }
            });

            // D_q через линейную регрессию log C_q vs log r
            var Dq = new double[21];
            for (int qi = 0; qi < 21; qi++)
            {
                double sx = 0, sy = 0, sxx = 0, sxy = 0;
                int cnt = 0;
                for (int ri = 0; ri < nR; ri++)
                {
                    if (Cq[qi, ri] <= 0) continue;
                    double lx = Math.Log(rValues[ri]);
                    double ly = Math.Log(Cq[qi, ri]);
                    sx += lx; sy += ly; sxx += lx * lx; sxy += lx * ly;
                    cnt++;
                }
                Dq[qi] = cnt >= 3 ? (cnt * sxy - sx * sy) / (cnt * sxx - sx * sx) : double.NaN;
            }

            // f(α): Legendre transform τ(q) = (q-1) D_q → α = dτ/dq, f = qα - τ
            var tau = new double[21];
            for (int qi = 0; qi < 21; qi++) tau[qi] = (qValues[qi] - 1.0) * Dq[qi];

            var alpha = new double[21];
            var falpha = new double[21];
            for (int qi = 0; qi < 21; qi++)
            {
                int lo = Math.Max(0, qi - 1), hi = Math.Min(20, qi + 1);
                double dq = qValues[hi] - qValues[lo];
                double dt = tau[hi] - tau[lo];
                alpha[qi] = dq != 0 ? dt / dq : double.NaN;
                falpha[qi] = qValues[qi] * alpha[qi] - tau[qi];
            }

            return new MultifractalResult
            {
                Q = qValues,
                Dq = Dq,
                Alpha = alpha,
                FAlpha = falpha
            };
        });
    }
}

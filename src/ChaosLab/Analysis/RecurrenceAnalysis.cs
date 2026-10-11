using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат Recurrence Quantification Analysis.
/// </summary>
public sealed class RqaResult
{
    /// <summary>Recurrence rate — плотность точек в матрице.</summary>
    public double RecurrenceRate;

    /// <summary>Determinism — доля точек в диагональных линиях (≥2).</summary>
    public double Determinism;

    /// <summary>Laminarity — доля точек в вертикальных линиях (≥2).</summary>
    public double Laminarity;

    /// <summary>Средняя длина диагональной линии.</summary>
    public double MeanDiagonal;

    /// <summary>Максимальная длина диагональной линии.</summary>
    public double MaxDiagonal;

    /// <summary>Shannon-энтропия распределения длин диагоналей.</summary>
    public double ShannonEntropyDiag;

    /// <summary>Сама матрица возвратов (0/1) для визуализации.</summary>
    public int[,] Matrix = new int[0, 0];
}

/// <summary>
/// Recurrence Quantification Analysis — количественный анализ матрицы возвратов.
/// </summary>
/// <remarks>
/// <para>Матрица возвратов: R(i,j) = Θ(ε − ||xᵢ − xⱼ||). Порог ε выбирается
/// как 10-й процентиль медианного расстояния между случайными парами.</para>
/// <para>Метрики:</para>
/// <list type="bullet">
/// <item><description><b>DET → 1</b>: регулярная динамика.</description></item>
/// <item><description><b>DET → 0</b>: хаос или шум.</description></item>
/// <item><description><b>LAM → 1</b>: «ламинарные» фазы.</description></item>
/// <item><description><b>Большой Lmax</b>: периодические окна в хаосе.</description></item>
/// </list>
/// </remarks>
/// <seealso href="https://doi.org/10.1209/0295-5075/4/9/004"/>
public static class RecurrenceAnalysis
{
    /// <summary>
    /// Вычисляет RQA-метрики и строит матрицу возвратов.
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="N">Размер матрицы (число точек).</param>
    /// <param name="epsFactor">Процентиль для ε (по умолчанию 0.1).</param>
    /// <param name="embeddingDim">Размерность вложения.</param>
    /// <param name="delay">Задержка вложения.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><see cref="RqaResult"/> со всеми метриками и матрицей.</returns>
    public static Task<RqaResult> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int N = 800, double epsFactor = 0.1,
        int embeddingDim = 3, int delay = 8,
        int transient = 2000, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            int n = sys.Dimension;
            var x = (double[])initialState.Clone();
            var xNext = new double[n];
            var rk = sys.IsDiscrete ? null : new Rk4Scratch(n);

            for (int i = 0; i < transient; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested)
                    return new RqaResult { Matrix = new int[0, 0] };
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
            }

            int total = N + (embeddingDim - 1) * delay;
            var series = new double[total];
            if (series.Length == 0)
                return new RqaResult { Matrix = new int[0, 0] };
            for (int i = 0; i < total; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested)
                    return new RqaResult { Matrix = new int[0, 0] };
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
                series[i] = x[0];
            }

            var pts = new double[N * embeddingDim];
            for (int i = 0; i < N; i++)
                for (int d = 0; d < embeddingDim; d++)
                    pts[i * embeddingDim + d] = series[i + d * delay];

            // Медианное расстояние для ε
            var rng = new Random(7);
            var sampleDist = new double[5000];
            for (int s = 0; s < 5000; s++)
            {
                int i = rng.Next(N), j = rng.Next(N);
                double d2 = 0;
                for (int k = 0; k < embeddingDim; k++)
                {
                    double diff = pts[i * embeddingDim + k] - pts[j * embeddingDim + k];
                    d2 += diff * diff;
                }
                sampleDist[s] = Math.Sqrt(d2);
            }
            Array.Sort(sampleDist);
            double eps = sampleDist[(int)(sampleDist.Length * epsFactor)];

            var matrix = new int[N, N];
            int recurrences = 0;
            for (int i = 0; i < N; i++)
            {
                if ((i & 63) == 0 && ct.IsCancellationRequested)
                    break;
                for (int j = 0; j < N; j++)
                {
                    double d2 = 0;
                    for (int k = 0; k < embeddingDim; k++)
                    {
                        double diff = pts[i * embeddingDim + k] - pts[j * embeddingDim + k];
                        d2 += diff * diff;
                    }
                    if (d2 <= eps * eps) { matrix[i, j] = 1; recurrences++; }
                }
            }

            // Линии, параллельные диагонали
            var diagLengths = new System.Collections.Generic.List<int>();
            for (int d = -(N - 1); d <= N - 1; d++)
            {
                int len = 0;
                for (int i = 0; i < N; i++)
                {
                    int j = i + d;
                    if (j < 0 || j >= N) continue;
                    if (matrix[i, j] == 1) len++;
                    else { if (len >= 2) diagLengths.Add(len); len = 0; }
                }
                if (len >= 2) diagLengths.Add(len);
            }

            // Линии, параллельные вертикали
            var vertLengths = new System.Collections.Generic.List<int>();
            for (int j = 0; j < N; j++)
            {
                int len = 0;
                for (int i = 0; i < N; i++)
                {
                    if (matrix[i, j] == 1) len++;
                    else { if (len >= 2) vertLengths.Add(len); len = 0; }
                }
                if (len >= 2) vertLengths.Add(len);
            }

            double rr = (double) recurrences / (N * N);

            // Один проход по спискам: считаем сумму, среднее и максимум одновременно.
            // List<int>.Sum()/Average()/Max() — это LINQ extension methods, каждая
            // проходит список заново и аллоцирует делегат.
            long diagSum = 0;
            int diagMax = 0;
            for (int i = 0; i < diagLengths.Count; i++)
            {
                int v = diagLengths[i];
                diagSum += v;
                if (v > diagMax) diagMax = v;
            }

            long vertSum = 0;
            for (int i = 0; i < vertLengths.Count; i++)
                vertSum += vertLengths[i];

            double det = recurrences > 0 ? (double) diagSum / recurrences : 0;
            double lam = recurrences > 0 ? (double) vertSum / recurrences : 0;
            double meanDiag = diagLengths.Count > 0
                ? (double) diagSum / diagLengths.Count : 0;
            double maxDiag = diagMax;

            // Энтропия распределения длин диагоналей.
            // Dictionary iteration — не LINQ, оставляем.
            double entr = 0;
            if (diagLengths.Count > 0)
            {
                var hist = new Dictionary<int, int>();
                for (int i = 0; i < diagLengths.Count; i++)
                {
                    int l = diagLengths[i];
                    hist.TryGetValue(l, out int cnt);
                    hist[l] = cnt + 1;
                }

                // Считаем энтропию прямо в проходе по dictionary
                double invTotal = 1.0 / diagLengths.Count;
                foreach (var kv in hist)
                {
                    double pr = kv.Value * invTotal;
                    entr -= pr * Math.Log(pr);
                }
            }

            return new RqaResult
            {
                RecurrenceRate = rr,
                Determinism = det,
                Laminarity = lam,
                MeanDiagonal = meanDiag,
                MaxDiagonal = maxDiag,
                ShannonEntropyDiag = entr,
                Matrix = matrix
            };
        });
    }
}

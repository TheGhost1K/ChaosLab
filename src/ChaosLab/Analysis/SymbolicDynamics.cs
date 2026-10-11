using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат символьно-динамического анализа.
/// </summary>
public sealed class SymbolicResult
{
    /// <summary>Число символов (областей разбиения).</summary>
    public int NumSymbols;

    /// <summary>Вероятности символов.</summary>
    public double[] SymbolProbabilities = Array.Empty<double>();

    /// <summary>Матрица переходов (условные вероятности p(j|i)).</summary>
    public double[,] TransitionMatrix = new double[0, 0];

    /// <summary>Shannon-энтропия распределения символов (в битах).</summary>
    public double ShannonEntropy;

    /// <summary>Топологическая энтропия (натуральный логарифм максимального
    /// собственного значения матрицы переходов).</summary>
    public double TopologicalEntropy;

    /// <summary>Число использованных переходов.</summary>
    public int SampleCount;
}

/// <summary>
/// Символьно-динамический анализ: разбиение фазового пространства на M областей
/// и построение графа переходов по траектории.
/// </summary>
/// <remarks>
/// <para>Метод даёт комбинаторную меру хаоса:</para>
/// <list type="bullet">
/// <item><description><b>Shannon entropy</b> H = −Σ pᵢ log pᵢ — разнообразие
/// посещаемых областей.</description></item>
/// <item><description><b>Topological entropy</b> h_top = log λ_max — экспоненциальная
/// скорость роста числа допустимых последовательностей символов.
/// Для хаоса h_top &gt; 0, для периодики = 0.</description></item>
/// </list>
/// <para>Разбиение — по квантилям вдоль координаты (равновероятные области).</para>
/// </remarks>
public static class SymbolicDynamics
{
    /// <summary>
    /// Вычисляет символьно-динамические характеристики системы.
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="numSymbols">Число символов (областей). Обычно 2–8.</param>
    /// <param name="sampleCount">Длина траектории для сбора статистики.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="coordIndex">Индекс координаты для разбиения.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><see cref="SymbolicResult"/> или <c>null</c> при недостатке данных.</returns>
    public static Task<SymbolicResult?> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int numSymbols = 4, int sampleCount = 20000,
        int transient = 2000, int coordIndex = 0,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            // 1. Собираем ряд
            var series = EntropyMeasures.CollectSeriesAsync(
                sys, initialState, p, dt, sampleCount, transient, decimation: 1, ct)
                .GetAwaiter().GetResult();

            if (series.Length < numSymbols * 10) return null;

            // 2. Определяем границы символов по квантилям
            var sorted = (double[]) series.Clone();
            Array.Sort(sorted);

            var boundaries = new double[numSymbols - 1];
            for (int s = 1; s < numSymbols; s++)
            {
                int idx = s * sorted.Length / numSymbols;
                boundaries[s - 1] = sorted[idx];
            }

            // 3. Преобразуем ряд в последовательность символов
            var symbols = new int[series.Length];
            for (int i = 0; i < series.Length; i++)
                symbols[i] = SymbolOf(series[i], boundaries);

            // 4. Матрица переходов — счётчики И binary adjacency
            var counts = new int[numSymbols, numSymbols];
            var rowSums = new int[numSymbols];
            var symbolCounts = new int[numSymbols];
            var adjacency = new double[numSymbols, numSymbols];

            for (int i = 0; i < symbols.Length - 1; i++)
            {
                int a = symbols[i], b = symbols[i + 1];
                counts[a, b]++;
                rowSums[a]++;
                symbolCounts[a]++;
                adjacency[a, b] = 1.0;      // ← ключевое: 0/1, а не вероятность
            }
            symbolCounts[symbols[^1]]++;

            // 5. Условные вероятности (для справки, не для h_top)
            var transition = new double[numSymbols, numSymbols];
            for (int i = 0; i < numSymbols; i++)
            {
                if (rowSums[i] == 0) continue;
                for (int j = 0; j < numSymbols; j++)
                    transition[i, j] = (double) counts[i, j] / rowSums[i];
            }

            // 6. Маргинальные вероятности
            var probs = new double[numSymbols];
            int totalSym = 0;
            for (int i = 0; i < numSymbols; i++) totalSym += symbolCounts[i];
            if (totalSym == 0) return null;
            for (int i = 0; i < numSymbols; i++)
                probs[i] = (double) symbolCounts[i] / totalSym;

            // 7. Shannon entropy
            double shannon = 0;
            for (int i = 0; i < numSymbols; i++)
                if (probs[i] > 0) shannon -= probs[i] * Math.Log2(probs[i]);

            // 8. Topological entropy = log(λ_max) степенным методом
            double hTop = LogLargestEigenvalue(adjacency, numSymbols);

            return new SymbolicResult
            {
                NumSymbols = numSymbols,
                SymbolProbabilities = probs,
                TransitionMatrix = transition,
                ShannonEntropy = shannon,
                TopologicalEntropy = hTop,
                SampleCount = symbols.Length,
            };
        });
    }

    private static int SymbolOf(double x, double[] boundaries)
    {
        for (int i = 0; i < boundaries.Length; i++)
            if (x < boundaries[i]) return i;
        return boundaries.Length;
    }

    /// <summary>
    /// Логарифм максимального собственного значения неотрицательной матрицы
    /// через степенную итерацию. Для матриц 2–8 — быстро и устойчиво.
    /// </summary>
    private static double LogLargestEigenvalue(double[,] matrix, int n)
    {
        // Стартовое приближение — равномерное
        var v = new double[n];
        for (int i = 0; i < n; i++) v[i] = 1.0 / n;

        double lambda = 0;
        for (int iter = 0; iter < 200; iter++)
        {
            // w = Mᵀ · v  (левое собственное число = правое для неотрицательной матрицы)
            var w = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++) sum += matrix[i, j] * v[j];
                w[i] = sum;
            }

            double norm = 0;
            for (int i = 0; i < n; i++) norm += w[i] * w[i];
            norm = Math.Sqrt(norm);
            if (norm < 1e-15) return 0;

            for (int i = 0; i < n; i++) v[i] = w[i] / norm;

            if (Math.Abs(norm - lambda) < 1e-10) break;
            lambda = norm;
        }

        return lambda > 1e-15 ? Math.Log(lambda) : 0;
    }
}

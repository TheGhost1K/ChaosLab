using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Энтропийные меры: Sample Entropy, Permutation Entropy.
/// </summary>
/// <remarks>
/// <para><b>Sample Entropy</b> (Richman–Moorman 2000) — мера непредсказуемости
/// ряда. Для повторяющихся паттернов длина m с порогом r:</para>
/// <code>
/// SampEn(m, r) = −log(A / B)
/// </code>
/// <para>где A — число пар длины m+1, B — длины m с расстоянием &lt; r.</para>
/// <para><b>Permutation Entropy</b> (Bandt–Pompe 2002) — энтропия распределения
/// ординальных паттернов, нормированная в [0, 1]:</para>
/// <code>
/// H = −Σ pᵢ log pᵢ / log(m!)
/// </code>
/// </remarks>
public static class EntropyMeasures
{
    /// <summary>
    /// Sample Entropy. Возвращает NaN при недостатке данных или отмене.
    /// </summary>
    /// <param name="series">Временной ряд.</param>
    /// <param name="m">Длина паттерна (обычно 2).</param>
    /// <param name="rFactor">Порог r как доля от σ (обычно 0.2).</param>
    /// <param name="ct">Токен отмены. Проверяется каждые 64 строки.</param>
    /// <remarks>
    /// Сложность O(N²). Для N &gt; 5000 вызывайте в фоновом потоке.
    /// При отмене возвращает <see cref="double.NaN"/>.
    /// </remarks>
    public static double SampleEntropy(
        double[] series, int m = 2, double rFactor = 0.2,
        CancellationToken ct = default)
    {
        int N = series.Length;
        if (N < m + 2) return double.NaN;

        double mean = 0.0;
        for (int i = 0; i < N; i++) mean += series[i];
        mean /= N;

        double std = 0.0;
        for (int i = 0; i < N; i++)
        {
            double d = series[i] - mean;
            std += d * d;
        }
        std = Math.Sqrt(std / N);
        double r = rFactor * std;
        if (r < 1e-12) return double.NaN;

        int B = 0, A = 0;
        for (int i = 0; i < N - m; i++)
        {
            // Проверка отмены раз в 64 итерации — дешевле, чем на каждом шаге
            if ((i & 0x3F) == 0 && ct.IsCancellationRequested)
                return double.NaN;

            for (int j = i + 1; j < N - m; j++)
            {
                bool matchM = true;
                for (int k = 0; k < m; k++)
                {
                    if (Math.Abs(series[i + k] - series[j + k]) > r)
                    {
                        matchM = false;
                        break;
                    }
                }
                if (!matchM) continue;
                B++;
                if (Math.Abs(series[i + m] - series[j + m]) <= r) A++;
            }
        }

        if (B == 0 || A == 0) return double.NaN;
        return -Math.Log((double) A / B);
    }

    /// <summary>
    /// Permutation Entropy, нормированная в [0, 1]. O(N·m·log m).
    /// </summary>
    /// <param name="series">Временной ряд.</param>
    /// <param name="order">Порядок паттерна (обычно 3).</param>
    /// <param name="delay">Задержка (обычно 1).</param>
    /// <param name="ct">Токен отмены.</param>
    public static double PermutationEntropy(
        double[] series, int order = 3, int delay = 1,
        CancellationToken ct = default)
    {
        int N = series.Length;
        if (N < order * delay + 1) return double.NaN;

        int numPatterns = 1;
        for (int i = 2; i <= order; i++) numPatterns *= i;
        var counts = new int[numPatterns];

        int total = 0;
        var idx = new int[order];
        var vals = new double[order];

        // Предвычисляем факториалы: fact[k] = k!
        var fact = new int[order + 1];
        fact[0] = 1;
        for (int k = 1; k <= order; k++) fact[k] = fact[k - 1] * k;

        for (int i = 0; i + (order - 1) * delay < N; i++)
        {
            if ((i & 0x3FF) == 0 && ct.IsCancellationRequested)
                return double.NaN;

            for (int k = 0; k < order; k++)
            {
                idx[k] = k;
                vals[k] = series[i + k * delay];
            }

            // Сортировка вставками (order ≤ 7)
            for (int a = 1; a < order; a++)
            {
                int keyIdx = idx[a];
                double keyVal = vals[a];
                int b = a - 1;
                while (b >= 0 && vals[b] > keyVal)
                {
                    idx[b + 1] = idx[b];
                    vals[b + 1] = vals[b];
                    b--;
                }
                idx[b + 1] = keyIdx;
                vals[b + 1] = keyVal;
            }

            // Lehmer code: для каждой позиции k считаем, сколько меньших
            // элементов стоит правее. rank = Σ c_k · (order−1−k)!
            int rank = 0;
            for (int k = 0; k < order; k++)
            {
                int c = 0;
                for (int j = k + 1; j < order; j++)
                    if (idx[j] < idx[k]) c++;
                rank += c * fact[order - 1 - k];
            }

            counts[rank]++;
            total++;
        }

        double H = 0.0;
        for (int i = 0; i < numPatterns; i++)
        {
            if (counts[i] == 0) continue;
            double pr = (double) counts[i] / total;
            H -= pr * Math.Log(pr);
        }
        return H / Math.Log(numPatterns);
    }

    /// <summary>
    /// Собирает скалярный временной ряд значений x₀ вдоль траектории.
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования (для потоков).</param>
    /// <param name="count">Длина ряда.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="decimation">
    /// Прореживание: сколько шагов RK4 выполняется между соседними
    /// семплами ряда. Эффективный шаг сэмплирования = <c>dt · decimation</c>.
    /// <para>Для 0–1 теста и других методов, требующих «нормально» сэмплированного
    /// ряда, используйте decimation так, чтобы эффективный шаг был ~0.1–0.3
    /// для типичных хаотических потоков.</para></param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>
    /// Ряд длины <paramref name="count"/> или <b>пустой массив</b> при отмене.
    /// Вызывающий код должен проверять <c>series.Length == 0</c>.
    /// </returns>
    /// <remarks>
    /// Не бросает <see cref="OperationCanceledException"/>. При отмене
    /// возвращает пустой массив, чтобы не засорять Output отладчика.
    /// </remarks>
    public static Task<double[]> CollectSeriesAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int count, int transient = 2000,
        int decimation = 1,
        CancellationToken ct = default)
    {
        if (decimation < 1) decimation = 1;

        // Важно: НЕ передаём ct в Task.Run — иначе .NET пометит задачу
        // как Canceled и await бросит TaskCanceledException, даже если
        // делегат вернулся нормально. Проверяем отмену внутри.
        return Task.Run(() =>
        {
            int n = sys.Dimension;
            var x = (double[]) initialState.Clone();
            var xNext = new double[n];
            var rk = sys.IsDiscrete ? null : new Rk4Scratch(n);

            // Прогрев — тоже с decimation, чтобы эффективное время прогрева
            // было такое же, как при вызове без decimation.
            int warmupSteps = transient * decimation;
            for (int i = 0; i < warmupSteps; i++)
            {
                if ((i & 0xFFF) == 0 && ct.IsCancellationRequested)
                    return Array.Empty<double>();
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
            }

            var series = new double[count];
            for (int i = 0; i < count; i++)
            {
                if ((i & 0xFFF) == 0 && ct.IsCancellationRequested)
                    return Array.Empty<double>();
                for (int k = 0; k < decimation; k++)
                    Integrator.StepOnce(sys, x, xNext, p, dt, rk);
                series[i] = x[0];
            }
            return series;
        });
    }
}

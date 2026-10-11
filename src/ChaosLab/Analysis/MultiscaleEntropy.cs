using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат multiscale entropy (MSE).
/// </summary>
public sealed class MultiscaleEntropyResult
{
    /// <summary>Масштабы τ = 1, 2, 3, ..., maxScale.</summary>
    public int[] Scales = Array.Empty<int>();

    /// <summary>Sample Entropy на каждом масштабе.</summary>
    public double[] SampleEntropies = Array.Empty<double>();

    /// <summary>Permutation Entropy на каждом масштабе.</summary>
    public double[] PermutationEntropies = Array.Empty<double>();
    /// <summary><c>true</c>, если расчёт был отменён.</summary>
    public bool Cancelled;
}

/// <summary>
/// Multiscale Entropy — энтропия как функция временного масштаба.
/// </summary>
/// <remarks>
/// <para>Coarse-graining: для масштаба τ ряд усредняется блоками по τ точек,
/// получая сокращённый ряд длины N/τ. Для каждого масштаба считается энтропия.</para>
/// <para>Интерпретация MSE-кривой:</para>
/// <list type="bullet">
/// <item><description><b>Белый шум</b>: энтропия монотонно убывает с масштабом.</description></item>
/// <item><description><b>1/f шум</b>: энтропия примерно постоянна (плоская кривая).</description></item>
/// <item><description><b>Хаос</b>: пик на промежуточных масштабах.</description></item>
/// </list>
/// <para>Метод различает «сложные» системы (1/f, хаос) от «простых» (белый шум,
/// периодика), даже если их SampEn при τ = 1 похожи.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1103/PhysRevLett.89.068102"/>
public static class MultiscaleEntropy
{
    /// <summary>
    /// Вычисляет multiscale entropy.
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="maxScale">Максимальный масштаб (обычно 20).</param>
    /// <param name="sampleCount">Длина исходного ряда. Sample Entropy имеет сложность O(N²), поэтому
    /// N &gt; 5000 замедляет рендер. Рекомендуется 2000–4000.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="ct">Токен отмены.</param>
    public static async Task<MultiscaleEntropyResult> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int maxScale = 12, int sampleCount = 3000,
        int transient = 2000,
        CancellationToken ct = default)
    {
        var series = await EntropyMeasures.CollectSeriesAsync(
            sys, initialState, p, dt, sampleCount, transient, decimation: 1, ct).ConfigureAwait(false);

        if (series.Length == 0)
            return new MultiscaleEntropyResult { Cancelled = true };

        var scales = new int[maxScale];
        var sampEn = new double[maxScale];
        var permEn = new double[maxScale];

        for (int s = 0; s < maxScale; s++)
        {
            if (ct.IsCancellationRequested)
                return new MultiscaleEntropyResult { Cancelled = true };

            int tau = s + 1;
            scales[s] = tau;

            int newLen = series.Length / tau;
            if (newLen < 100)
            {
                sampEn[s] = double.NaN;
                permEn[s] = double.NaN;
                continue;
            }

            var coarse = new double[newLen];
            for (int i = 0; i < newLen; i++)
            {
                double sum = 0;
                for (int j = 0; j < tau; j++) sum += series[i * tau + j];
                coarse[i] = sum / tau;
            }

            // SampEn — самый дорогой вызов; проверяем отмену внутри
            sampEn[s] = EntropyMeasures.SampleEntropy(coarse, 2, 0.2, ct);
            permEn[s] = EntropyMeasures.PermutationEntropy(coarse, 3, 1, ct);

            if (ct.IsCancellationRequested)
                return new MultiscaleEntropyResult { Cancelled = true };
        }

        return new MultiscaleEntropyResult
        {
            Scales = scales,
            SampleEntropies = sampEn,
            PermutationEntropies = permEn,
        };
    }
}

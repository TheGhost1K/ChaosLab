using ChaosLab.Core;
using System.Numerics;

namespace ChaosLab.Analysis;

/// <summary>
/// Спектральная плотность мощности через FFT с окном Ханна.
/// </summary>
/// <remarks>
/// <para>Форма спектра различает типы динамики:</para>
/// <list type="bullet">
/// <item><description><b>Дискретные пики</b> — периодика или квазипериодика
/// (Langford).</description></item>
/// <item><description><b>Широкополосный спектр</b> — хаос (Lorenz,
/// Rössler).</description></item>
/// <item><description><b>Степенной закон 1/f^β</b> — фрактальный шум.</description></item>
/// </list>
/// <para>Используется radix-2 Cooley–Tukey FFT. Окно Ханна снижает утечку
/// спектра. Ряд обнуляется до степени 2.</para>
/// </remarks>
public static class PowerSpectrum
{
    /// <summary>
    /// Вычисляет PSD (половину спектра).
    /// </summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="nSamples">Длина ряда (будет дополнена до степени 2).</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Массив PSD длиной n/2.</returns>
    public static async Task<double[]> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int nSamples = 8192, int transient = 2000,
        CancellationToken ct = default)
    {
        var series = await EntropyMeasures.CollectSeriesAsync(
        sys, initialState, p, dt, nSamples, transient, decimation: 1, ct).ConfigureAwait(false);

        if (series.Length == 0)
            return Array.Empty<double>();   // ← защита от пустого ряда

        // Убираем постоянную составляющую и применяем окно Ханна
        double mean = 0.0;
        for (int i = 0; i < series.Length; i++) mean += series[i];
        mean /= series.Length;

        int n = 1;
        while (n < series.Length) n <<= 1;

        var buf = new Complex[n];
        for (int i = 0; i < series.Length; i++)
        {
            double w = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (series.Length - 1));
            buf[i] = new Complex((series[i] - mean) * w, 0);
        }

        FftInPlace(buf);

        int half = n / 2;
        var ps = new double[half];
        for (int i = 0; i < half; i++)
            ps[i] = buf[i].Real * buf[i].Real + buf[i].Imaginary * buf[i].Imaginary;
        return ps;
    }

    /// <summary>Iterative radix-2 Cooley-Tukey FFT, in-place.</summary>
    private static void FftInPlace(Complex[] a)
    {
        int n = a.Length;
        // Bit-reversal
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) (a[i], a[j]) = (a[j], a[i]);
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2.0 * Math.PI / len;
            var wlen = new Complex(Math.Cos(ang), Math.Sin(ang));
            for (int i = 0; i < n; i += len)
            {
                var w = Complex.One;
                for (int k = 0; k < len / 2; k++)
                {
                    var u = a[i + k];
                    var v = a[i + k + len / 2] * w;
                    a[i + k] = u + v;
                    a[i + k + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }
    }
}

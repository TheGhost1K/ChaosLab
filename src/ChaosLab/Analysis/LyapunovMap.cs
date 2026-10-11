using System.Collections.Concurrent;
using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Двумерная карта старшего показателя Ляпунова λ₁(p₁, p₂).
/// Показывает, где в пространстве параметров система хаотична.
/// </summary>
/// <remarks>
/// <para>
/// Использует вариационный метод: интегрирует <c>ẇ = J(x)·w</c> параллельно
/// с <c>ẋ = f(x)</c>. Аналитический <see cref="ChaoticSystem.JacobianVectorProduct"/>
/// даёт ×2 ускорение по сравнению с двухтраекторным методом.
/// </para>
/// <para>
/// Рендеринг прогрессивный: сначала превью 256×256 (быстро), затем полное
/// разрешение. Ранняя остановка по относительной стандартной ошибке
/// среднего (se &lt; 15% от |mean|) экономит до 4× времени.
/// </para>
/// <para>
/// Оси Y по умолчанию инвертированы — высокие значения параметра вверху,
/// как на классических графиках.
/// </para>
/// </remarks>
public static class LyapunovMap
{
    /// <summary>
    /// Вычисляет карту λ₁ для системы в указанном прямоугольнике параметров.
    /// </summary>
    /// <param name="sys">Система (поток или отображение).</param>
    /// <param name="p1Index">Индекс параметра по оси X в <see cref="ISimulation.Parameters"/>.</param>
    /// <param name="p1Min">Минимальное значение p₁.</param>
    /// <param name="p1Max">Максимальное значение p₁.</param>
    /// <param name="p2Index">Индекс параметра по оси Y.</param>
    /// <param name="p2Min">Минимальное значение p₂ (внизу графика).</param>
    /// <param name="p2Max">Максимальное значение p₂ (вверху графика).</param>
    /// <param name="width">Ширина результирующего изображения в пикселях.</param>
    /// <param name="height">Высота изображения.</param>
    /// <param name="dt">Шаг интегрирования (только для потоков).</param>
    /// <param name="iterations">Максимум итераций на пиксель (после которых считаем λ).</param>
    /// <param name="transient">Число шагов прогрева до начала накопления статистики.</param>
    /// <param name="palette">Палитра для раскраски. По умолчанию — <see cref="Palette.All"/>[0].</param>
    /// <param name="outputBuffer">
    /// Опциональный буфер для записи результата. Если размер совпадает с
    /// <c>width × height</c>, используется напрямую (без аллокации). Позволяет
    /// переиспользовать буфер рендерера между кадрами.
    /// </param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Упакованные Bgr32-пиксели размером <c>width × height</c>.</returns>
    /// <remarks>
    /// Раскраска: λ &lt; 0 → синяя половина палитры, λ &gt; 0 → оранжевая.
    /// Граница λ = 0 обводится белым контуром.
    /// </remarks>
    public static Task<int[]> ComputeAsync(
        ChaoticSystem sys,
        int p1Index, double p1Min, double p1Max,
        int p2Index, double p2Min, double p2Max,
        int width, int height, double dt,
        int iterations = 2000, int transient = 500,
        Palette? palette = null,
        int[]? outputBuffer = null,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            int[] pixels;
            if (outputBuffer != null && outputBuffer.Length == width * height)
                pixels = outputBuffer;
            else
                pixels = new int[width * height];
            var lambdas = new float[width * height];
            int dim = sys.Dimension;
            var pal = palette ?? Palette.All[0];
            bool discrete = sys.IsDiscrete;

            int chunk = Math.Max(1, height / (Environment.ProcessorCount * 4));

            Parallel.ForEach(
                Partitioner.Create(0, height, chunk),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                () =>
                {
                    var s = new LocalScratch(dim);
                    if (!discrete)
                    {
                        s.Rk = new Rk4Scratch(dim);
                        s.Var = new VariationalScratch(dim);
                    }
                    return s;
                },
                (range, state, local) =>
                {
                    for (int y = range.Item1; y < range.Item2; y++)
                    {
                        if (ct.IsCancellationRequested) { state.Stop(); return local; }

                        // ⬇ ИНВЕРСИЯ Y-ОСИ: p2Max сверху
                        double p2v = p2Max - (p2Max - p2Min) * y / (height - 1);
                        local.P[p2Index] = p2v;

                        for (int x = 0; x < width; x++)
                        {
                            if ((x & 0x1F) == 0 && ct.IsCancellationRequested)
                            { state.Stop(); return local; }

                            double p1v = p1Min + (p1Max - p1Min) * x / (width - 1);
                            local.P[p1Index] = p1v;

                            double lam = EstimateLambdaVariational(
                                sys, local, dt, iterations, transient);

                            lambdas[y * width + x] = (float) lam;
                        }
                    }
                    return local;
                },
                _ => { });

            // Раскраска + контур λ = 0
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float lam = lambdas[y * width + x];

                    bool isBoundary = false;
                    if (x > 0 && x < width - 1 && y > 0 && y < height - 1)
                    {
                        float l = lambdas[y * width + x - 1];
                        float r = lambdas[y * width + x + 1];
                        float u = lambdas[(y - 1) * width + x];
                        float d = lambdas[(y + 1) * width + x];
                        if ((lam >= 0 && (l < 0 || r < 0 || u < 0 || d < 0)) ||
                            (lam < 0 && (l >= 0 || r >= 0 || u >= 0 || d >= 0)))
                            isBoundary = true;
                    }

                    pixels[y * width + x] = isBoundary
                        ? 0xFFFFFF
                        : pal.Sample(LambdaToColor(lam));
                }
            }

            return pixels;
        });
    }

    /// <summary>
    /// Нелинейное отображение λ → [0, 1].
    /// λ &lt; 0 → синяя половина, λ > 0 → оранжево-красная. Переход резкий у нуля.
    /// </summary>
    private static double LambdaToColor(double lam)
    {
        if (lam >= 0)
        {
            double x = Math.Min(lam / 1.0, 1.0);
            return 0.55 + 0.45 * x;
        }
        else
        {
            double x = Math.Min(-lam / 0.5, 1.0);
            return 0.55 * (1.0 - x);
        }
    }

    private static double EstimateLambdaVariational(
        ChaoticSystem sys, LocalScratch s, double dt,
        int maxIterations, int transient)
    {
        var p = s.P;
        var x = s.X;
        var w = s.W;

        Array.Copy(sys.DefaultInitialStateCached, x, x.Length);
        Array.Clear(w, 0, w.Length);
        w[0] = 1.0;

        for (int i = 0; i < transient; i++)
        {
            if (sys.IsDiscrete)
            {
                sys.Next(x, s.Tmp, p);
                Array.Copy(s.Tmp, x, x.Length);
            }
            else
            {
                Integrator.Rk4Step(sys, x, p, dt, s.Rk!);
            }
        }

        const int renormEvery = 10;
        int dim = x.Length;
        int minSteps = 150;
        int maxSteps = maxIterations;

        double sum = 0, sumSq = 0;
        int count = 0;

        for (int i = 0; i < maxSteps; i++)
        {
            if (sys.IsDiscrete)
            {
                sys.JacobianVectorProduct(x, w, s.TmpW, p);
                Array.Copy(s.TmpW, w, dim);
                sys.Next(x, s.Tmp, p);
                Array.Copy(s.Tmp, x, dim);
            }
            else
            {
                Integrator.VariationalRk4Step(sys, x, w, p, dt, s.Var!);
            }

            if ((i + 1) % renormEvery != 0) continue;

            double w2 = 0;
            for (int k = 0; k < dim; k++) w2 += w[k] * w[k];

            if (w2 > 1e-300)
            {
                double logNorm = 0.5 * Math.Log(w2);
                sum += logNorm;
                sumSq += logNorm * logNorm;
                count++;

                double invNorm = 1.0 / Math.Sqrt(w2);
                for (int k = 0; k < dim; k++) w[k] *= invNorm;
            }

            // ⬇ ОСЛАБЛЕННАЯ РАННЯЯ ОСТАНОВКА (гибридный критерий)
            if (i >= minSteps && (count & 7) == 0)
            {
                double mean = sum / count;
                double var = sumSq / count - mean * mean;
                if (var < 0) var = 0;
                double se = Math.Sqrt(var / count);

                if (se < Math.Abs(mean) * 0.15 || se < 0.02)
                    break;
            }
        }

        if (count == 0) return 0;
        double totalTime = sys.IsDiscrete
            ? (double) count * renormEvery
            : count * renormEvery * dt;
        return sum / totalTime;
    }

    /// <summary>
    /// Прогрессивный вариант карты Ляпунова: сначала быстрое превью
    /// 256×256 (обычно за 0.3–0.5 с), затем полное разрешение.
    /// </summary>
    /// <remarks>
    /// <para>Пользователь сразу видит структуру (пусть и грубую) и не ждёт
    /// полного расчёта 1–3 секунды. Второй <c>Report</c> содержит полный
    /// кадр.</para>
    /// <para>Если <paramref name="ct"/> срабатывает между этапами,
    /// возвращается превью.</para>
    /// </remarks>
    /// <param name="sys">Система (поток или отображение).</param>
    /// <param name="p1Index">Индекс параметра по оси X в <see cref="Core.ISimulation.Parameters"/>.</param>
    /// <param name="p1Min">Минимум диапазона p₁ (левая граница).</param>
    /// <param name="p1Max">Максимум диапазона p₁ (правая граница).</param>
    /// <param name="p2Index">Индекс параметра по оси Y.</param>
    /// <param name="p2Min">Минимум p₂ (низ графика).</param>
    /// <param name="p2Max">Максимум p₂ (верх графика).</param>
    /// <param name="finalWidth">Ширина финального изображения в пикселях.</param>
    /// <param name="finalHeight">Высота финального изображения.</param>
    /// <param name="dt">Шаг интегрирования (только для потоков).</param>
    /// <param name="iterations">Максимум итераций на пиксель.</param>
    /// <param name="transient">Шагов прогрева до накопления статистики.</param>
    /// <param name="palette">Палитра для раскраски.</param>
    /// <param name="progress">Колбэк прогресса: вызывается дважды (превью, финал).</param>
    /// <param name="finalOutputBuffer">Опциональный буфер для записи результата. Если размер совпадает с
    /// <c>width × height</c>, используется напрямую (без аллокации). Позволяет
    /// переиспользовать буфер рендерера между кадрами.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Финальное изображение в разрешении <paramref name="finalWidth"/> × <paramref name="finalHeight"/>.</returns>
    public static async Task<int[]> ComputeProgressiveAsync(
        ChaoticSystem sys,
        int p1Index, double p1Min, double p1Max,
        int p2Index, double p2Min, double p2Max,
        int finalWidth, int finalHeight, double dt,
        int iterations, int transient,
        Palette palette,
        IProgress<(int[] pixels, int w, int h)> progress,
        int[]? finalOutputBuffer = null,
        CancellationToken ct = default)
    {
        int previewW = Math.Min(256, finalWidth);
        int previewH = Math.Min(256, finalHeight);

        var preview = await ComputeAsync(sys, p1Index, p1Min, p1Max,
            p2Index, p2Min, p2Max, previewW, previewH, dt,
            iterations: Math.Min(iterations, 500),
            transient: transient, palette: palette, ct: ct);

        progress?.Report((preview, previewW, previewH));
        if (ct.IsCancellationRequested) return preview;

        var full = await ComputeAsync(sys, p1Index, p1Min, p1Max,
            p2Index, p2Min, p2Max, finalWidth, finalHeight, dt,
            iterations, transient, palette, finalOutputBuffer, ct);

        progress?.Report((full, finalWidth, finalHeight));
        return full;
    }

    private sealed class LocalScratch
    {
        public double[] P, X, W, Tmp, TmpW;
        public Rk4Scratch? Rk;
        public VariationalScratch? Var;

        public LocalScratch(int dim)
        {
            P = new double[8];
            X = new double[dim];
            W = new double[dim];
            Tmp = new double[dim];
            TmpW = new double[dim];
            Rk = null;
            Var = null;
        }
    }
}

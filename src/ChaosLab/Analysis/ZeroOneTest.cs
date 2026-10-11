using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// 0–1 тест хаоса Готтвальда–Мельбурна (2004).
/// </summary>
/// <remarks>
/// <para>Оценивает параметр K через корреляцию log(Ms) vs log(n), где
/// Ms — среднее квадратичное смещение комплексного преобразования ряда.</para>
/// <list type="bullet">
/// <item><description>K ≈ 1 — хаос.</description></item>
/// <item><description>K ≈ 0 — регулярная динамика.</description></item>
/// <item><description>0.3 &lt; K &lt; 0.7 — переходная зона.</description></item>
/// </list>
/// <para>Преимущества: работает без delay-embedding, устойчив к шуму,
/// применим к коротким рядам.</para>
/// <para>ChaosLab использует медиану K по 50 случайным c для устойчивости.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1016/j.physd.2004.05.009"/>
public static class ZeroOneTest
{
    /// <summary>Вычисляет параметр K.</summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Параметры.</param>
    /// <param name="dt">Шаг интегрирования.</param>
    /// <param name="sampleCount">Длина ряда.</param>
    /// <param name="transient">Шагов прогрева.</param>
    /// <param name="nC">Число случайных c (медиана берётся по ним).</param>
    /// <param name="decimation">
    /// Прореживание ряда. <b>Важно:</b> для потоков RK4 требует мелкого dt,
    /// но 0–1 тесту нужен ряд с автокорреляцией ~0.7–0.9. Используйте
    /// decimation ≈ 20 при dt = 0.01, чтобы получить эффективный шаг ~0.2.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>K ∈ [0, 1] или NaN.</returns>
    public static Task<double> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int sampleCount = 4000, int transient = 2000,
        int nC = 100, int decimation = 1, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            // 1. Собираем скалярный ряд — не центрируем
            var series = EntropyMeasures.CollectSeriesAsync(
                sys, initialState, p, dt, sampleCount, transient, decimation, ct)
                .GetAwaiter().GetResult();

            if (series.Length < 500) return double.NaN;

            int N = series.Length;
            int nCut = N / 10;   // верхняя граница для n в M(n)

            var pArr = new double[N];
            var qArr = new double[N];
            var M = new double[nCut + 1];

            var rng = new Random(2024);
            var kValues = new double[nC];
            int validC = 0;

            for (int ci = 0; ci < nC; ci++)
            {
                if (ct.IsCancellationRequested) break;

                // c в (π/5, 4π/5) — рекомендация из статьи
                double c = Math.PI * (0.2 + 0.6 * rng.NextDouble());
                //double c = 0.3 + 2.2 * rng.NextDouble();
                // 2. Кумулятивные p_c(n), q_c(n)
                double pc = 0, qc = 0;
                for (int j = 0; j < N; j++)
                {
                    pc += series[j] * Math.Cos((j + 1) * c);
                    qc += series[j] * Math.Sin((j + 1) * c);
                    pArr[j] = pc;
                    qArr[j] = qc;
                }

                // 3. M_c(n) — среднее квадратичное смещение
                for (int n = 1; n <= nCut; n++)
                {
                    double sum = 0;
                    int cnt = N - n;
                    for (int j = 0; j < cnt; j++)
                    {
                        double dp = pArr[j + n] - pArr[j];
                        double dq = qArr[j + n] - qArr[j];
                        sum += dp * dp + dq * dq;
                    }
                    M[n] = sum / cnt;
                }

                // 4. K_c = corr(log n, log M(n))  — оригинальная формула 2004
                //    Chaos: M(n) ~ V·n → log M = log V + log n → corr с log n ≈ 1
                //    Regular: M(n) bounded → log M oscillates → corr ≈ 0
                double logNsum = 0, logMsum = 0, logNN = 0, logMM = 0, logNM = 0;
                int cntLog = 0;

                for (int n = 2; n <= nCut; n++)   // с 2, чтобы log(n) > 0
                {
                    if (M[n] <= 1e-15) continue;
                    double lx = Math.Log(n);
                    double ly = Math.Log(M[n]);
                    logNsum += lx;
                    logMsum += ly;
                    logNN += lx * lx;
                    logMM += ly * ly;
                    logNM += lx * ly;
                    cntLog++;
                }

                if (cntLog < 5) continue;

                double num = cntLog * logNM - logNsum * logMsum;
                double den = Math.Sqrt(
                    (cntLog * logNN - logNsum * logNsum) *
                    (cntLog * logMM - logMsum * logMsum));

                if (den < 1e-15) continue;

                double Kc = num / den;
                kValues[validC++] = Kc;
            }

            if (validC == 0) return double.NaN;

            // 5. K = median(K_c)
            Array.Sort(kValues, 0, validC);
            double median = validC % 2 == 1
                ? kValues[validC / 2]
                : 0.5 * (kValues[validC / 2 - 1] + kValues[validC / 2]);

            // Clamp в [0, 1]: отрицательные значения = 0 (устойчивость)
            return Math.Max(0.0, Math.Min(1.0, median));
        });
    }
}

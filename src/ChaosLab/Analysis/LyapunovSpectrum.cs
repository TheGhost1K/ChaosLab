using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Вычисление полного спектра показателей Ляпунова λ₁ ≥ λ₂ ≥ ... ≥ λₙ.
/// </summary>
/// <remarks>
/// <para>Использует QR-метод Бенеттина (1980): совместно с траекторией
/// эволюционирует матрица Q из n ортонормированных векторов:</para>
/// <code>
/// Q_{k+1} = QR(J(x_k) · Q_k)
/// λᵢ = (1/T) · Σ log|Rᵢᵢ|
/// </code>
/// <para>Для QR используется метод Хаусхолдера — численно устойчив на
/// миллионах шагов (Gram-Schmidt теряет ортогональность).</para>
/// <para>Проверка для Лоренца (σ=10, ρ=28, β=8/3): [0.906, 0, −14.57],
/// сумма = −(σ+1+β).</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1007/BF02128236"/>
public static class LyapunovSpectrum
{
    /// <summary>Вычисляет полный спектр показателей Ляпунова.</summary>
    /// <param name="sys">Система.</param>
    /// <param name="initialState">Начальное состояние.</param>
    /// <param name="p">Значения параметров.</param>
    /// <param name="dt">Шаг интегрирования (только для потоков).</param>
    /// <param name="steps">Число шагов накопления после прогрева.</param>
    /// <param name="transient">Число шагов прогрева (выбрасываются).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Спектр в порядке убывания: <c>[λ₁, λ₂, ..., λₙ]</c>.</returns>
    public static Task<double[]> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int steps, int transient = 5000,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            int n = sys.Dimension;
            var x = (double[])initialState.Clone();
            var xNext = new double[n];
            var rk = sys.IsDiscrete ? null : new Rk4Scratch(n);

            var jac = new double[n * n];
            var q = new double[n * n];
            var w = new double[n * n];
            var r = new double[n * n];
            var sums = new double[n];

            // Буферы для JVP: e_k и col
            var ek = new double[n];
            var col = new double[n];

            for (int i = 0; i < n; i++) q[i * n + i] = 1.0;

            for (int i = 0; i < transient; i++)
            {
                if ((i & 1023) == 0 && ct.IsCancellationRequested) return new double[n];
                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
            }

            double totalTime = 0.0;
            for (int step = 0; step < steps; step++)
            {
                if ((step & 255) == 0 && ct.IsCancellationRequested) return new double[n];

                // J через n JVP с базисными векторами — быстрее, чем n+1 Derivatives
                for (int k = 0; k < n; k++)
                {
                    Array.Clear(ek, 0, n);
                    ek[k] = 1.0;
                    sys.JacobianVectorProduct(x, ek, col, p);
                    for (int i = 0; i < n; i++) jac[i * n + k] = col[i];
                }

                Multiply(jac, q, w, n);
                if (!sys.IsDiscrete)
                    for (int k = 0; k < n * n; k++) w[k] = q[k] + dt * w[k];

                QRDecompose(w, q, r, n);

                for (int k = 0; k < n; k++)
                {
                    double diag = Math.Abs(r[k * n + k]);
                    if (diag > 1e-300) sums[k] += Math.Log(diag);
                }

                Integrator.StepOnce(sys, x, xNext, p, dt, rk);
                totalTime += sys.IsDiscrete ? 1.0 : dt;
            }

            var result = new double[n];
            for (int k = 0; k < n; k++) result[k] = sums[k] / totalTime;
            Array.Sort(result);
            Array.Reverse(result);
            return result;
        });
    }

    private static void Multiply(double[] a, double[] b, double[] c, int n)
    {
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double s = 0.0;
                int ia = i * n;
                for (int k = 0; k < n; k++) s += a[ia + k] * b[k * n + j];
                c[i * n + j] = s;
            }
    }

    /// <summary>
    /// Разложение A = Q·R методом Хаусхолдера. Численно устойчиво на миллионах шагов.
    /// A[n×n] row-major. Q получается из R и отражений.
    /// </summary>
    private static void QRDecompose(double[] a, double[] q, double[] r, int n)
    {
        // r := a (копия), q := I (для накопления)
        Array.Copy(a, r, n * n);
        Array.Clear(q, 0, n * n);
        for (int i = 0; i < n; i++) q[i * n + i] = 1.0;

        var v = new double[n];

        for (int k = 0; k < n - 1; k++)
        {
            // Строим вектор Хаусхолдера для столбца k, начиная со строки k
            double norm = 0.0;
            for (int i = k; i < n; i++) norm += r[i * n + k] * r[i * n + k];
            norm = Math.Sqrt(norm);
            if (norm < 1e-300) continue;

            double alpha = r[k * n + k] > 0 ? -norm : norm;
            v[k] = r[k * n + k] - alpha;
            for (int i = k + 1; i < n; i++) v[i] = r[i * n + k];

            double vnorm2 = v[k] * v[k];
            for (int i = k + 1; i < n; i++) vnorm2 += v[i] * v[i];
            if (vnorm2 < 1e-300) continue;

            double inv = 2.0 / vnorm2;

            // Применяем к R: R := R - inv * v * (vᵀ R)
            for (int j = k; j < n; j++)
            {
                double dot = 0.0;
                for (int i = k; i < n; i++) dot += v[i] * r[i * n + j];
                dot *= inv;
                for (int i = k; i < n; i++) r[i * n + j] -= dot * v[i];
            }

            // Применяем к Q: Q := Q - inv * (Q v) * vᵀ  (Q справа умножается на H)
            for (int i = 0; i < n; i++)
            {
                double dot = 0.0;
                for (int j = k; j < n; j++) dot += q[i * n + j] * v[j];
                dot *= inv;
                for (int j = k; j < n; j++) q[i * n + j] -= dot * v[j];
            }
        }

        // Обнуляем нижний треугольник явно (для чистоты)
        for (int i = 1; i < n; i++)
            for (int j = 0; j < i; j++)
                r[i * n + j] = 0.0;
    }
}

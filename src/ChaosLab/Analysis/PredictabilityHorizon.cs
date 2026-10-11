using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Результат оценки горизонта предсказуемости.
/// </summary>
public sealed class PredictabilityResult
{
    /// <summary>Старший показатель Ляпунова λ₁.</summary>
    public double Lambda1;
    /// <summary>Горизонт предсказуемости (единиц времени).</summary>
    public double Horizon;
    /// <summary>Стартовая ошибка ε₀ (в единицах аттрактора).</summary>
    public double Epsilon0;
    /// <summary>Размер аттрактора Δ.</summary>
    public double AttractorSize;
}

/// <summary>
/// Горизонт предсказуемости из показателя Ляпунова.
/// </summary>
/// <remarks>
/// <para>Модель: ошибка растёт экспоненциально, δ(t) = ε₀·e^(λ₁·t).
/// Предсказание «испортится», когда δ(t) достигнет размера аттрактора Δ:</para>
/// <code>
/// t_h = (1/λ₁) · log(Δ / ε₀)
/// </code>
/// <para>Для Лоренца при λ₁ = 0.906, ε₀ = 10⁻³, Δ = 40:</para>
/// <code>
/// t_h ≈ 1.10 · log(40000) ≈ 11.7 t.e.
/// </code>
/// <para>Что соответствует реальному поведению: прогноз Лоренца «работает»
/// ~10 t.e., после чего погрешность растёт до размера аттрактора.</para>
/// </remarks>
public static class PredictabilityHorizon
{
    /// <summary>
    /// Оценивает горизонт предсказуемости.
    /// </summary>
    /// <param name="lambda1">Старший показатель Ляпунова.</param>
    /// <param name="attractorSize">Характерный размер аттрактора Δ.</param>
    /// <param name="epsilon0">Начальная ошибка ε₀ (по умолчанию 10⁻³).</param>
    public static PredictabilityResult Estimate(
        double lambda1, double attractorSize, double epsilon0 = 1e-3)
    {
        if (lambda1 <= 0 || attractorSize <= 0 || epsilon0 <= 0)
            return new PredictabilityResult { Lambda1 = lambda1 };

        double horizon = (1.0 / lambda1) * Math.Log(attractorSize / epsilon0);

        return new PredictabilityResult
        {
            Lambda1 = lambda1,
            Horizon = horizon,
            Epsilon0 = epsilon0,
            AttractorSize = attractorSize,
        };
    }

    /// <summary>
    /// Оценивает характерный размер аттрактора по буферу плотности,
    /// полученному из <see cref="Rendering.AttractorRenderer"/>.
    /// </summary>
    /// <remarks>
    /// <para>Метод находит bounding box занятых пикселей (density &gt; 0),
    /// затем переводит размеры из пикселей в единицы фазового пространства
    /// через масштаб камеры: <c>Δ = pixels / cam.Zoom</c>.</para>
    /// <para>Возвращает максимум из ширины и высоты bounding box. Для систем
    /// высокой размерности проекция на экран может сжимать одну из осей —
    /// в этом случае метод даёт заниженную оценку Δ.</para>
    /// <para>Более точная альтернатива — <c>AnalysisViewModel.EstimateAttractorSizeNumeric</c>,
    /// которая работает с полным фазовым пространством, а не с проекцией.</para>
    /// </remarks>
    /// <param name="density">Буфер плотности из <c>AttractorRenderer.Density</c>.</param>
    /// <param name="w">Ширина холста в пикселях.</param>
    /// <param name="h">Высота холста в пикселях.</param>
    /// <param name="cam">Камера, использованная при рендере.</param>
    /// <returns>
    /// Характерный размер аттрактора в единицах фазового пространства.
    /// Возвращает 1.0, если буфер пуст или содержит одну точку.
    /// </returns>
    public static double EstimateAttractorSize(
        ushort[] density, int w, int h, Camera3D cam)
    {
        // Находим bounding box занятых пикселей
        int minX = w, maxX = 0, minY = h, maxY = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (density[y * w + x] > 0)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

        if (minX > maxX || minY > maxY) return 1.0;

        double pxW = (maxX - minX) / cam.Zoom;
        double pxH = (maxY - minY) / cam.Zoom;
        return Math.Max(pxW, pxH);
    }
}

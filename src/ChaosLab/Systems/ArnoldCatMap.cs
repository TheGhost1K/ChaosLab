using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Отображение «кота Арнольда» — сохраняющее площадь отображение тора.
/// </summary>
/// <remarks>
/// <code>
/// x_{n+1} = xₙ + yₙ           (mod 1)
/// y_{n+1} = xₙ + k·yₙ         (mod 1)
/// </code>
/// <para>Классика: k = 1. Матрица [[1,1],[1,2]] имеет след 3 > 2 → хаос.
/// Лямбда-инварианты: λ₁ = log((3+√5)/2) ≈ 0.962, λ₁+λ₂ = 0
/// (сохраняет площадь).</para>
/// <para>Пример необыкновенно чистого хаоса: единственный параметр k
/// управляет режимом, при k = 1 — эталон.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Arnold%27s_cat_map"/>
public sealed class ArnoldCatMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Arnold Cat";
    /// <inheritdoc/>
    public override string Category => "Map";
    /// <inheritdoc/>
    public override int Dimension => 2;
    /// <inheritdoc/>
    public override bool IsDiscrete => true;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.2 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("k", 1, 5, 1,
            "Параметр матрицы [[1,1],[1,1+k]]. При k ≥ 1 след > 2 → хаос. " +
            "При k = 1 — классический пример."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 800, 0.5, 0.5, 0);
    /// <inheritdoc/>
    public override (double lo, double hi) BifurcationRange => (0, 1);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double k = p[0];
        double x = s[0], y = s[1];

        double nx = x + y;
        double ny = x + (1 + k) * y;

        // Приведение на тор [0, 1)
        n[0] = nx - Math.Floor(nx);
        n[1] = ny - Math.Floor(ny);
    }

    /// <summary>Аналитический JVP: J = [[1, 1], [1, 1+k]] — постоянная матрица.</summary>
    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        double k = p[0];
        r[0] = w[0] + w[1];
        r[1] = w[0] + (1 + k) * w[1];
    }
}

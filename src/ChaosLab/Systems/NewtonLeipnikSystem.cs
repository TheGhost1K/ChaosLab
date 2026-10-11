using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Система Ньютона–Лейпника (1981) — динамика твёрдого тела с двумя аттракторами.
/// </summary>
/// <remarks>
/// <code>
/// ẋ = −ax + y + 10yz
/// ẏ = −x − 0.4y + 5xz
/// ż = bz − 5xy
/// </code>
/// <para>Редкий пример системы с **двумя** сосуществующими странными аттракторами:
/// выбор зависит от начальных условий. Классика: a = 0.4, b = 0.175.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1007/BF01245714"/>
public sealed class NewtonLeipnikSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Newton-Leipnik";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.349, 0.0, -0.160 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", 0.2, 0.6, 0.4, "Диссипация по x. Классика — 0.4."),
        new("b", 0.05, 0.30, 0.175, "Диссипация по z. Классика — 0.175."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.4, -0.35, 12, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0], b = p[1];
        double x = s[0], y = s[1], z = s[2];

        d[0] = -a * x + y + 10 * y * z;
        d[1] = -x - 0.4 * y + 5 * x * z;
        d[2] = b * z - 5 * x * y;
    }

    /// <summary>Аналитический JVP: J = [[−a, 1+10z, 10y], [−1+5z, −0.4, 5x], [−5y, −5x, b]].</summary>
    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1];
        double x = s[0], y = s[1], z = s[2];

        r[0] = -a * w[0] + (1 + 10 * z) * w[1] + 10 * y * w[2];
        r[1] = (-1 + 5 * z) * w[0] - 0.4 * w[1] + 5 * x * w[2];
        r[2] = -5 * y * w[0] - 5 * x * w[1] + b * w[2];
    }
}

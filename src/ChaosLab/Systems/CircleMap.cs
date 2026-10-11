using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Отображение окружности — модель фазовой синхронизации.
/// </summary>
/// <remarks>
/// <code>
/// θ_{n+1} = θₙ + Ω − (K/2π)·sin(2π·θₙ)   (mod 1)
/// </code>
/// <para>Классическая модель синхронизации (Arnold 1961, Кузнецов 1998).
/// В плоскости (Ω, K) образует **языки Арнольда** — области, где отношение
/// средних частот p/q фиксировано.</para>
/// <para>При K &gt; 1 система становится необратимой и может быть хаотичной.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Circle_map"/>
public sealed class CircleMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Circle Map";
    /// <inheritdoc/>
    public override string Category => "Map";
    /// <inheritdoc/>
    public override int Dimension => 1;
    /// <inheritdoc/>
    public override bool IsDiscrete => true;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("Ω", 0.0, 1.0, 0.6065,
            "Собственная частота. При иррациональном Ω/K — квазипериодика."),
        new("K", 0.0, 2.0, 1.5,
            "Сила связи. При K < 1 — только синхронизация (языки Арнольда). " +
            "При K = 1 — критический режим. При K > 1 — хаос."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 800, 0.5, 0.5, 0);
    /// <inheritdoc/>
    public override (double lo, double hi) BifurcationRange => (0, 1);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double omega = p[0], K = p[1];
        double theta = s[0];

        double nt = theta + omega - (K / (2 * Math.PI)) * Math.Sin(2 * Math.PI * theta);
        n[0] = nt - Math.Floor(nt);
    }

    /// <summary>
    /// Аналитический JVP: J = 1 − K·cos(2πθ). Матрица 1×1.
    /// </summary>
    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        double K = p[1];
        double theta = s[0];
        r[0] = (1 - K * Math.Cos(2 * Math.PI * theta)) * w[0];
    }
}

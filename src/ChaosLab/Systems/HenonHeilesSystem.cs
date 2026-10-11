using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Система Хенона–Хейлеса (1964) — классическая гамильтонова модель
/// движения звезды в галактическом потенциале.
/// </summary>
/// <remarks>
/// <para>Гамильтониан:</para>
/// <code>
/// H = ½(pₓ² + p_y²) + ½(x² + y²) + x²y − y³/3
/// </code>
/// <para>Уравнения движения (состояние = [x, y, pₓ, p_y]):</para>
/// <code>
/// ẋ = pₓ
/// ẏ = p_y
/// ṗₓ = −x − 2xy
/// ṗ_y = −y − x² + y²
/// </code>
/// <para>При энергии E = 1/12 — граница между регулярным и хаотическим
/// движением. При E &gt; 1/6 траектория может уйти на бесконечность.</para>
/// <para>Реализует <see cref="ISymplecticSystem"/>, поэтому может
/// интегрироваться методом Verlet — энергия сохраняется точно.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1086/147955"/>
public sealed class HenonHeilesSystem : ChaoticSystem, ISymplecticSystem
{
    /// <inheritdoc/>
    public override string Name => "Hénon-Heiles";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 4;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.0, 0.0, 0.3 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.35, 8, 0, 0, 0);

    /// <inheritdoc/>
    public int HalfDimension => 2;

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double x = s[0], y = s[1], px = s[2], py = s[3];
        d[0] = px;
        d[1] = py;
        d[2] = -x - 2 * x * y;
        d[3] = -y - x * x + y * y;
    }

    /// <inheritdoc/>
    public void GradientPotential(ReadOnlySpan<double> q, Span<double> gradV, double[] p)
    {
        double x = q[0], y = q[1];
        gradV[0] = x + 2 * x * y;
        gradV[1] = y + x * x - y * y;
    }

    /// <summary>
    /// Аналитический JVP для системы Хенона–Хейлеса.
    /// </summary>
    /// <remarks>
    /// J = [[0, 0, 1, 0], [0, 0, 0, 1], [−1−2y, −2x, 0, 0], [−2x, −1+2y, 0, 0]]
    /// </remarks>
    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        double x = s[0], y = s[1];
        r[0] = w[2];
        r[1] = w[3];
        r[2] = (-1 - 2 * y) * w[0] - 2 * x * w[1];
        r[3] = -2 * x * w[0] + (-1 + 2 * y) * w[1];
    }

    /// <inheritdoc/>
    public override void Project3D(double[] s, Span<double> xyz)
    {
        xyz[0] = s[0];
        xyz[1] = s[1];
        xyz[2] = 0;
    }
}

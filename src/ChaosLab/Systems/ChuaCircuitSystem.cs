using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Цепь Чуа (1983) — первая электронная реализация детерминированного хаоса.
/// </summary>
/// <remarks>
/// <code>
/// ẋ = α(y − x − g(x))
/// ẏ = x − y + z
/// ż = −βy − γz
/// g(x) = m₁·x + 0.5·(m₀ − m₁)·(|x+1| − |x−1|)
/// </code>
/// <para>Кусочно-линейная нелинейность g(x) описывает диод Чуа. При
/// классических параметрах α = 15.6, β = 28, γ = 0 даёт двойной свиток
/// (double scroll) с λ₁ ≈ 0.23.</para>
/// <para>Оригинальная схема собрана из четырёх элементов: конденсаторы,
/// катушка индуктивности и нелинейный резистор.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1109/TCS.1985.1085744"/>
public sealed class ChuaCircuitSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Chua's Circuit";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.0, 0.0 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("α", 5, 20, 15.6, "Ёмкостный масштаб. Определяет основной «размах»."),
        new("β", 15, 40, 28, "Индуктивный масштаб. Ширина свитка."),
        new("m₀", -2.0, -0.5, -1.143, "Крутизна диода в линейной зоне."),
        new("m₁", -1.0, 0.0, -0.714, "Крутизна диода в области насыщения."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.4, -0.3, 12, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double alpha = p[0], beta = p[1], m0 = p[2], m1 = p[3];
        double x = s[0], y = s[1], z = s[2];

        double g = ChuaNonlinearity(x, m0, m1);

        d[0] = alpha * (y - x - g);
        d[1] = x - y + z;
        d[2] = -beta * y;
    }

    /// <summary>Кусочно-линейная нелинейность g(x) — характеристика диода Чуа.</summary>
    private static double ChuaNonlinearity(double x, double m0, double m1)
        => m1 * x + 0.5 * (m0 - m1) * (Math.Abs(x + 1) - Math.Abs(x - 1));

    /// <summary>Аналитический JVP: J = [[α(−1−g'(x)), α, 0], [1, −1, 1], [0, −β, 0]].</summary>
    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        double alpha = p[0], beta = p[1], m0 = p[2], m1 = p[3];
        double x = s[0];

        // g'(x) = m₁ + 0.5(m₀ − m₁)(sign(x+1) − sign(x−1))
        double dg = m1 + 0.5 * (m0 - m1) * (Math.Sign(x + 1) - Math.Sign(x - 1));

        r[0] = alpha * (-1 - dg) * w[0] + alpha * w[1];
        r[1] = w[0] - w[1] + w[2];
        r[2] = -beta * w[1];
    }
}

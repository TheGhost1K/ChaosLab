using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Динамо Рикитаке (1958) — модель геомагнитного динамо с двумя дисками.
/// </summary>
/// <remarks>
/// <code>
/// ẋ = −μx + yz
/// ẏ = −μy + (z − a)x
/// ż = 1 − xy
/// </code>
/// <para>Простая модель обращения магнитного поля Земли. При μ = 1 и a = 5
/// даёт устойчивый странный аттрактор с λ₁ ≈ 0.3.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1017/S0305004100033547"/>
public sealed class RikitakeDynamoSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Rikitake Dynamo";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 1.0, 0.5, 0.5 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("μ", 0.5, 2.0, 1.0, "Диссипация: отношение сопротивления к индуктивности."),
        new("a", 3.0, 8.0, 5.0, "Асимметрия дисков. При a < 3 система уходит на цикл."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.4, 40, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double mu = p[0], a = p[1];
        double x = s[0], y = s[1], z = s[2];

        d[0] = -mu * x + y * z;
        d[1] = -mu * y + (z - a) * x;
        d[2] = 1 - x * y;
    }

    /// <summary>Аналитический JVP: J = [[−μ, z, y], [z−a, −μ, x], [−y, −x, 0]].</summary>
    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        double mu = p[0], a = p[1];
        double x = s[0], y = s[1], z = s[2];

        r[0] = -mu * w[0] + z * w[1] + y * w[2];
        r[1] = (z - a) * w[0] - mu * w[1] + x * w[2];
        r[2] = -y * w[0] - x * w[1];
    }
}

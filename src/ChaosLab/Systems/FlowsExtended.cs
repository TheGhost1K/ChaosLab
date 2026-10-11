using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Система Рабиновича–Фабриканта (1979) — модель модуляции волн.
/// </summary>
/// <remarks>
/// <code>
/// ẋ = y(z − 1 + x²) + γx
/// ẏ = x(3z + 1 − x²) + γy
/// ż = −2z(α + xy)
/// </code>
/// <para>Хаос при α = 1.1, γ = 0.87. Демонстрирует перемежаемость:
/// устойчивые квази-циклы сменяются хаотическими всплесками.</para>
/// </remarks>
public sealed class RabinovichFabrikantSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Rabinovich-Fabrikant";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { -1.0, 0.0, 0.5 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("α", 0.5, 2.0, 1.1, "Диссипация в ż. При α = 1.1 — устойчивый хаос."),
        new("γ", 0.5, 1.5, 0.87, "Усиление в ẋ и ẏ. Ниже 0.8 — потеря хаоса."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.6, -0.4, 35, 0, 0, 1);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double alpha = p[0], gamma = p[1];
        double x = s[0], y = s[1], z = s[2];
        d[0] = y * (z - 1 + x * x) + gamma * x;
        d[1] = x * (3 * z + 1 - x * x) + gamma * y;
        d[2] = -2 * z * (alpha + x * y);
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double alpha = p[0], gamma = p[1];
        double x = s[0], y = s[1], z = s[2];

        // ∂ẋ/∂x = 2xy+γ;  ∂ẋ/∂y = z-1+x²;  ∂ẋ/∂z = y
        // ∂ẏ/∂x = 3z+1-3x²; ∂ẏ/∂y = γ;    ∂ẏ/∂z = 3x
        // ∂ż/∂x = -2yz;     ∂ż/∂y = -2xz;  ∂ż/∂z = -2(α+xy)
        r[0] = (2 * x * y + gamma) * w[0]
             + (z - 1 + x * x) * w[1]
             + y * w[2];

        r[1] = (3 * z + 1 - 3 * x * x) * w[0]
             + gamma * w[1]
             + 3 * x * w[2];

        r[2] = -2 * y * z * w[0]
             - 2 * x * z * w[1]
             + (-2 * (alpha + x * y)) * w[2];
    }
}

/// <summary>
/// Система Шимизу–Мориоки (1980) — упрощённая форма системы Лоренца.
/// </summary>
/// <remarks>
/// <code>
/// ẋ = y
/// ẏ = x − λy − xz
/// ż = −αz + x²
/// </code>
/// <para>При λ = 0.75, α = 0.85 даёт странный аттрактор, топологически
/// похожий на Лоренца, но проще для анализа.</para>
/// </remarks>
public sealed class ShimizuMoriokaSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Shimizu-Morioka";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.5, 0.5, 0.5 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("λ", 0.4, 1.2, 0.75, "Диссипация y. Аналог σ у Лоренца."),
        new("α", 0.3, 2.0, 0.85, "Диссипация z. При α < 0.7 — периодика."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.4, 55, 0, 0, 2);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double lam = p[0], alpha = p[1];
        d[0] = s[1];
        d[1] = s[0] - lam * s[1] - s[0] * s[2];
        d[2] = -alpha * s[2] + s[0] * s[0];
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double lam = p[0], alpha = p[1];
        double x = s[0], y = s[1], z = s[2];

        // J = [[0, 1, 0], [1-z, -λ, -x], [2x, 0, -α]]
        r[0] = w[1];
        r[1] = (1 - z) * w[0] - lam * w[1] - x * w[2];
        r[2] = 2 * x * w[0] - alpha * w[2];
    }
}

/// <summary>
/// Термостат Нозе–Гувера (1985) — осциллятор с термостатом.
/// </summary>
/// <remarks>
/// Простейшая модель детерминированного термостата в молекулярной динамике.
/// Единственный параметр a ≈ 1.5 даёт хаос при некоторых начальных условиях.
/// </remarks>
public sealed class NoseHooverSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Nosé-Hoover";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.1, 0.0 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [new("a", 0.5, 3.0, 1.5,
        "Целевая кинетическая энергия термостата. " +
        "При a > 2 часто уходит на устойчивый цикл.")];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.4, 45, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0];
        d[0] = s[1];
        d[1] = -s[0] - s[1] * s[2];
        d[2] = s[1] * s[1] - a;
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double x = s[0], y = s[1], z = s[2];

        // J = [[0, 1, 0], [-1, -z, -y], [0, 2y, 0]]
        r[0] = w[1];
        r[1] = -w[0] - z * w[1] - y * w[2];
        r[2] = 2 * y * w[1];
    }
}

/// <summary>
/// Система Лэнгфорда (1983) — квазипериодический аттрактор на торе.
/// </summary>
/// <remarks>
/// <code>
/// ẋ = (ν − 1)x − y + xz
/// ẏ = x + (ν − 1)y + yz
/// ż = μz − (x² + y² + z²) − γz³
/// </code>
/// <para>Демонстрирует переходы между периодическим, квазипериодическим и
/// хаотическим режимами при изменении параметров.</para>
/// </remarks>
public sealed class LangfordSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Langford";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.5, 0.5, 0.5 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("ν", 0.7, 1.2, 0.85, "Скорость вращения в плоскости xy."),
        new("μ", 0.4, 1.2, 0.85, "Усиление по z. Определяет «толщину» тора."),
        new("γ", 0.1, 1.0, 0.5,  "Кубическая диссипация z. Стабилизирует аттрактор."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.6, -0.35, 60, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double nu = p[0], mu = p[1], gamma = p[2];
        double x = s[0], y = s[1], z = s[2];
        d[0] = (nu - 1) * x - y + x * z;
        d[1] = x + (nu - 1) * y + y * z;
        d[2] = mu * z - (x * x + y * y + z * z) - gamma * z * z * z;
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double nu = p[0], mu = p[1], gamma = p[2];
        double x = s[0], y = s[1], z = s[2];

        // J = [[ν-1, -1, x], [1, ν-1, y], [-2x, -2y, μ-2z-3γz²]]
        r[0] = (nu - 1) * w[0] - w[1] + x * w[2];
        r[1] = w[0] + (nu - 1) * w[1] + y * w[2];
        r[2] = -2 * x * w[0] - 2 * y * w[1]
             + (mu - 2 * z - 3 * gamma * z * z) * w[2];
    }
}

using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Отображение Гумовского–Миры (1973) — «морские звёзды».
/// </summary>
/// <remarks>
/// Квази-консервативное отображение. При μ = 0.01 даёт красивые
/// симметричные структуры, напоминающие морские звёзды.
/// </remarks>
public sealed class GumowskiMiraMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Gumowski-Mira";
    /// <inheritdoc/>
    public override string Category => "Map";
    /// <inheritdoc/>
    public override int Dimension => 2;
    /// <inheritdoc/>
    public override bool IsDiscrete => true;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.1 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("μ", -1.0, 1.0, 0.01,
        "Неиспользуемый (в текущей реализации). Оставлен для " +
        "совместимости с классической формулировкой."),
        new("a", -1.0, 1.0, -0.31,
            "Квадратичный коэффициент в f(x). Классика — −0.31."),
        new("b", 0.5, 1.5, 1.0,
            "Коэффициент b в x' = b·y + f(x). При b = 1 — «морские звёзды»."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 130, 0, 0, 0);
    /// <inheritdoc/>
    public override (double lo, double hi) BifurcationRange => (-20, 20);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double mu = p[0], a = p[1], b = p[2];
        double x = s[0], y = s[1];
        double f = a * x + 2 * (1 - a) * x * x / (1 + x * x);
        n[0] = b * y + f;
        n[1] = -x + f;
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double mu = p[0], a = p[1], b = p[2];
        double x = s[0], y = s[1];

        // f(x) = a x + 2(1-a) x² / (1+x²)
        // f'(x) = a + 2(1-a) · (2x(1+x²) - x²·2x) / (1+x²)²
        //       = a + 4(1-a) x / (1+x²)²
        double denom = 1 + x * x;
        double fp = a + 4 * (1 - a) * x / (denom * denom);

        // x' = b y + f(x);  y' = -x + f(x)
        // J = [[f', b], [f'-1, 0]]
        r[0] = fp * w[0] + b * w[1];
        r[1] = (fp - 1) * w[0];
    }
}

/// <summary>
/// Стандартное отображение Чирикова (1969) — консервативная гамильтонова система.
/// </summary>
/// <remarks>
/// <code>
/// pₙ₊₁ = pₙ + K·sin(qₙ)
/// qₙ₊₁ = qₙ + pₙ₊₁
/// </code>
/// <para>При K &lt; 1 сохраняются инвариантные торы (теорема KAM). При K &gt; 1
/// торы разрушаются, возникает глобальный хаос.</para>
/// </remarks>
public sealed class StandardMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Chirikov Standard";
    /// <inheritdoc/>
    public override string Category => "Map";
    /// <inheritdoc/>
    public override int Dimension => 2;
    /// <inheritdoc/>
    public override bool IsDiscrete => true;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.1 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [new("K", 0.1, 10, 2.5,
        "Параметр возмущения. При K < 1 сохраняются торы (KAM). " +
        "При K > 1 — глобальный хаос.")];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 80, 0, 0, 0);
    /// <inheritdoc/>
    public override (double lo, double hi) BifurcationRange => (-Math.PI, Math.PI);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double K = p[0];
        double q = s[0], pp = s[1];
        n[1] = pp + K * Math.Sin(q);
        n[0] = q + n[1];
        // Нормализация в [-π, π]
        n[0] = Wrap(n[0]);
        n[1] = Wrap(n[1]);
    }

    private static double Wrap(double v)
    {
        double w = v % (2 * Math.PI);
        if (w > Math.PI) w -= 2 * Math.PI;
        if (w < -Math.PI) w += 2 * Math.PI;
        return w;
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double K = p[0];
        double q = s[0], pp = s[1];

        // p' = p + K sin(q);  q' = q + p'
        // J = [[1 + K cos(q), 1], [K cos(q), 1]]  (в порядке p', q')
        // Но у нас порядок (q, p) — так что:
        // q' = q + p + K sin(q);  p' = p + K sin(q)
        // ∂q'/∂q = 1 + K cos(q);  ∂q'/∂p = 1
        // ∂p'/∂q = K cos(q);      ∂p'/∂p = 1
        double Kc = K * Math.Cos(q);
        r[0] = (1 + Kc) * w[0] + w[1];
        r[1] = Kc * w[0] + w[1];
    }
}

/// <summary>
/// Тент-отображение — кусочно-линейная хаотическая система.
/// </summary>
/// <remarks>
/// При μ = 2 топологически сопряжено с логистикой при r = 4. Аналитически
/// решаемо: λ₁ = log μ.
/// </remarks>
public sealed class TentMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Tent";
    /// <inheritdoc/>
    public override string Category => "Map";
    /// <inheritdoc/>
    public override int Dimension => 1;
    /// <inheritdoc/>
    public override bool IsDiscrete => true;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.31 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [new("μ", 1.0, 2.0, 1.99,
        "Наклон тента. При μ = 2 топологически сопряжено с логистикой " +
        "при r = 4. λ₁ = log μ.")];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 900, 0.5, 0.5, 0);
    /// <inheritdoc/>
    public override (double lo, double hi) BifurcationRange => (0, 1);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double mu = p[0], x = s[0];
        n[0] = x < 0.5 ? mu * x : mu * (1 - x);
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double mu = p[0];
        // x' = μx (x<0.5);  x' = μ(1-x) (x>=0.5)
        // J = μ (x<0.5);  J = -μ (x>=0.5)
        r[0] = (s[0] < 0.5 ? mu : -mu) * w[0];
    }
}

/// <summary>
/// Отображение Гаусса — continued fraction map.
/// </summary>
/// <remarks>
/// Возникает в теории цепных дробей. Обладает эргодической инвариантной
/// мерой Гаусса–Кузмина. λ₁ ≈ 0.86.
/// </remarks>
public sealed class GaussMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Gauss";
    /// <inheritdoc/>
    public override string Category => "Map";
    /// <inheritdoc/>
    public override int Dimension => 1;
    /// <inheritdoc/>
    public override bool IsDiscrete => true;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.6180339887 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("α", 0.5, 3.0, 1.0, "Масштаб отображения α/x."),
        new("β", 0.01, 0.5, 0.1, "Дополнительный сдвиг под дробной частью."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 700, 0.5, 0.5, 0);
    /// <inheritdoc/>
    public override (double lo, double hi) BifurcationRange => (0, 1);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double alpha = p[0], beta = p[1];
        double x = s[0];
        if (x < 1e-12) x = 1e-12;
        n[0] = alpha / x + beta;
        if (n[0] > 1.0) n[0] = n[0] - Math.Floor(n[0]);
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double alpha = p[0];
        double x = s[0];
        if (x < 1e-12) x = 1e-12;

        // x' = frac(α/x + β)
        // Пока производная от frac почти всюду равна производной от аргумента
        r[0] = -alpha / (x * x) * w[0];
    }
}

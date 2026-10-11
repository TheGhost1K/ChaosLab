using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Логистическое отображение — классический пример перехода к хаосу.
/// </summary>
/// <remarks>
/// <code>xₙ₊₁ = r·xₙ(1 − xₙ)</code>
/// <para>При r = 4 система топологически сопряжена с сдвигом Бернулли,
/// λ₁ = log 2 ≈ 0.693. Каскад удвоения периода при r → r∞ ≈ 3.5699
/// подчиняется постоянной Фейгенбаума δ ≈ 4.669.</para>
/// </remarks>
public sealed class LogisticMap : ChaoticSystem
{
    ///<inheritdoc/>
    public override string Name => "Logistic";

    ///<inheritdoc/>
    public override string Category => "Map";

    ///<inheritdoc/>
    public override int Dimension => 1;

    /// <inheritdoc/>
    public override bool IsDiscrete => true;

    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.3 };

    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [new("r", 0, 4, 3.9,
        "Скорость роста популяции. При r > 3.57 — хаос с окнами " +
        "периодичности. При r = 4 топологически сопряжено со сдвигом Бернулли.")];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 900, 0.5, 0.5, 0);

    /// <inheritdoc/>
    public override (double lo, double hi) BifurcationRange => (0, 1);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
        => n[0] = p[0] * s[0] * (1 - s[0]);

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        // x' = r x(1-x);  dx'/dx = r(1-2x)
        r[0] = p[0] * (1 - 2 * s[0]) * w[0];
    }
}

/// <summary>
/// Отображение Эно (1976) — странный аттрактор в 2D.
/// </summary>
/// <remarks>
/// <code>
/// xₙ₊₁ = 1 − a·xₙ² + yₙ
/// yₙ₊₁ = b·xₙ
/// </code>
/// <para>Классика: a = 1.4, b = 0.3. λ₁ ≈ 0.419, λ₂ ≈ −1.623,
/// сумма = log|b|. D₂ ≈ 1.26.</para>
/// </remarks>
public sealed class HenonMap : ChaoticSystem
{
    ///<inheritdoc/>
    public override string Name => "Hénon";

    ///<inheritdoc/>
    public override string Category => "Map";

    ///<inheritdoc/>
    public override int Dimension => 2;

    ///<inheritdoc/>
    public override bool IsDiscrete => true;

    ///<inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.1 };

    ///<inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", 0.8, 1.5, 1.4,
        "Нелинейность. При a = 1.4, b = 0.3 — классический странный аттрактор."),
        new("b", 0.1, 0.4, 0.3,
        "Линейный коэффициент в ẏ. Управляет сжатием: сумма λ₁ + λ₂ = log|b|."),
    ];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 220, 0, 0, 0);

    /// <inheritdoc/>
    public override (double lo, double hi) BifurcationRange => (-1.5, 1.5);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        n[0] = 1 - p[0] * s[0] * s[0] + s[1];
        n[1] = p[1] * s[0];
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1];
        double x = s[0], y = s[1];

        // x' = 1 - a x² + y;  y' = b x
        // J = [[-2ax, 1], [b, 0]]
        r[0] = -2 * a * x * w[0] + w[1];
        r[1] = b * w[0];
    }
}

/// <summary>
/// Отображение Икеды — модель оптического резонатора.
/// </summary>
/// <remarks>
/// Возникает при изучении распространения света в нелинейной среде с
/// задержкой. При u = 0.918 даёт многослойный странный аттрактор.
/// </remarks>
public sealed class IkedaMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Ikeda";

    /// <inheritdoc/>
    public override string Category => "Map";

    /// <inheritdoc/>
    public override int Dimension => 2;

    /// <inheritdoc/>
    public override bool IsDiscrete => true;

    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.1 };

    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [new("u", 0.7, 1.0, 0.918,
        "Коэффициент потерь в оптическом резонаторе. При u > 0.9 — хаос.")];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 130, 0.5, 0, 0);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double u = p[0];
        double t = 0.4 - 6.0 / (1.0 + s[0] * s[0] + s[1] * s[1]);
        double ct = Math.Cos(t), st = Math.Sin(t);
        n[0] = 1 + u * (s[0] * ct - s[1] * st);
        n[1] = u * (s[0] * st + s[1] * ct);
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double u = p[0];
        double x = s[0], y = s[1];
        double r2 = x * x + y * y;
        double denom = 1.0 + r2;
        double t = 0.4 - 6.0 / denom;
        double ct = Math.Cos(t), st = Math.Sin(t);

        // dt/dx = 12 x / denom², dt/dy = 12 y / denom²
        double dtdx = 12.0 * x / (denom * denom);
        double dtdy = 12.0 * y / (denom * denom);

        // x' = 1 + u(x ct - y st)
        // y' = u(x st + y ct)
        //
        // ∂x'/∂x = u(ct - x st·dtdx - y ct·dtdx) ... но проще выразить:
        // Пусть a = x ct - y st,  b = x st + y ct
        // da/dx = ct + x·(-st)·dtdx - y·ct·dtdx = ct - (x st + y ct)·dtdx
        // db/dx = st + x·ct·dtdx - y·st·dtdx = st + (x ct - y st)·dtdx
        double dxpdx = u * (ct - (x * st + y * ct) * dtdx);
        double dxpdy = u * (-st - (x * st + y * ct) * dtdy);
        double dypdx = u * (st + (x * ct - y * st) * dtdx);
        double dypdy = u * (ct - (x * ct - y * st) * dtdy);

        r[0] = dxpdx * w[0] + dxpdy * w[1];
        r[1] = dypdx * w[0] + dypdy * w[1];
    }
}

/// <summary>
/// Отображение Клиффорда — декоративный хаос с плавными структурами.
/// </summary>
public sealed class CliffordMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Clifford";

    /// <inheritdoc/>
    public override string Category => "Map";

    /// <inheritdoc/>
    public override int Dimension => 2;

    /// <inheritdoc/>

    public override bool IsDiscrete => true;

    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.0, 0.0 };

    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", -3, 3, -1.4, "Частота синуса и косинуса по x."),
        new("b", -3, 3, 1.6,  "Частота по y."),
        new("c", -3, 3, 1.0,  "Амплитуда косинуса в x'."),
        new("d", -3, 3, 0.7,  "Амплитуда косинуса в y'."),
    ];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 200, 0, 0, 0);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3];
        n[0] = Math.Sin(a * s[1]) + c * Math.Cos(a * s[0]);
        n[1] = Math.Sin(b * s[0]) + dd * Math.Cos(b * s[1]);
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3];
        double x = s[0], y = s[1];

        // x' = sin(a y) + c cos(a x);  y' = sin(b x) + d cos(b y)
        double dxpdx = -c * a * Math.Sin(a * x);
        double dxpdy = a * Math.Cos(a * y);
        double dypdx = b * Math.Cos(b * x);
        double dypdy = -dd * b * Math.Sin(b * y);

        r[0] = dxpdx * w[0] + dxpdy * w[1];
        r[1] = dypdx * w[0] + dypdy * w[1];
    }
}

/// <summary>
/// Отображение Де Йонга — слоистые хаотические структуры.
/// </summary>
public sealed class DeJongMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "De Jong";

    /// <inheritdoc/>
    public override string Category => "Map";

    /// <inheritdoc/>
    public override int Dimension => 2;

    /// <inheritdoc/>
    public override bool IsDiscrete => true;

    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.0, 0.0 };

    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", -3, 3, 1.4,  "Частота sin по y в x'."),
        new("b", -3, 3, -2.3, "Частота cos по x в x'."),
        new("c", -3, 3, 2.4,  "Частота sin по x в y'."),
        new("d", -3, 3, -2.1, "Частота cos по y в y'."),
    ];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 180, 0, 0, 0);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3];
        n[0] = Math.Sin(a * s[1]) - Math.Cos(b * s[0]);
        n[1] = Math.Sin(c * s[0]) - Math.Cos(dd * s[1]);
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3];
        double x = s[0], y = s[1];

        // x' = sin(a y) - cos(b x);  y' = sin(c x) - cos(d y)
        double dxpdx = b * Math.Sin(b * x);
        double dxpdy = a * Math.Cos(a * y);
        double dypdx = c * Math.Cos(c * x);
        double dypdy = dd * Math.Sin(dd * y);

        r[0] = dxpdx * w[0] + dxpdy * w[1];
        r[1] = dypdx * w[0] + dypdy * w[1];
    }
}

/// <summary>
/// Отображение Tinkerbell — «крылья бабочки» в фазовом пространстве.
/// </summary>
public sealed class TinkerbellMap : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Tinkerbell";

    /// <inheritdoc/>
    public override string Category => "Map";

    /// <inheritdoc/>
    public override int Dimension => 2;

    /// <inheritdoc/>
    public override bool IsDiscrete => true;

    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, -0.1 };

    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", 0, 1.5, 0.9,     "Линейный коэффициент x в x'."),
        new("b", -1.5, 0, -0.6013, "Линейный коэффициент y в x'. Классика — −0.6013."),
        new("c", 0, 4, 2.0,       "Линейный коэффициент x в y'."),
        new("d", 0, 1.5, 0.5,     "Линейный коэффициент y в y'."),
    ];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0, 0, 160, 0, 0, 0);

    /// <inheritdoc/>
    public override void Next(double[] s, double[] n, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3];
        double x = s[0], y = s[1];
        n[0] = x * x - y * y + a * x + b * y;
        n[1] = 2 * x * y + c * x + dd * y;
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3];
        double x = s[0], y = s[1];

        // x' = x² - y² + a x + b y;  y' = 2xy + c x + d y
        double dxpdx = 2 * x + a;
        double dxpdy = -2 * y + b;
        double dypdx = 2 * y + c;
        double dypdy = 2 * x + dd;

        r[0] = dxpdx * w[0] + dxpdy * w[1];
        r[1] = dypdx * w[0] + dypdy * w[1];
    }
}

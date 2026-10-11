using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Система Лоренца (1963) — классическая модель атмосферной конвекции.
/// </summary>
/// <remarks>
/// <para>Уравнения:</para>
/// <code>
/// ẋ = σ(y − x)
/// ẏ = x(ρ − z) − y
/// ż = xy − βz
/// </code>
/// <para>
/// Классические значения σ = 10, ρ = 28, β = 8/3 дают странный аттрактор
/// с λ₁ ≈ 0.906 и размерностью D₂ ≈ 2.06.
/// </para>
/// <para>
/// Первая система, у которой был численно обнаружен детерминированный хаос.
/// Открытие привело к формулировке «эффекта бабочки» — чувствительности
/// к начальным условиям.
/// </para>
/// </remarks>
/// <seealso href="https://doi.org/10.1175/1520-0469(1963)020%3C0130:DNF%3E2.0.CO;2"/>
public sealed class LorenzSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Lorenz";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.0, 0.0 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("σ", 1, 30, 10,
        "Число Прандтля: отношение вязкости к температуропроводности. " +
        "Классика — 10."),
        new("ρ", 1, 60, 28,
        "Число Рэлея: управляющий параметр конвекции. " +
        "При ρ > 24.74 — странный аттрактор. Классика — 28."),
        new("β", 0.1, 5, 8.0 / 3.0,
        "Геометрический фактор: отношение размеров области. " +
        "Классика — 8/3 ≈ 2.667."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.4, -0.35, 22, 0, 0, 25);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double sigma = p[0], rho = p[1], beta = p[2];
        d[0] = sigma * (s[1] - s[0]);
        d[1] = s[0] * (rho - s[2]) - s[1];
        d[2] = s[0] * s[1] - beta * s[2];
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double sigma = p[0], rho = p[1], beta = p[2];
        double x = s[0], y = s[1], z = s[2];

        // J = [[-σ, σ, 0], [ρ-z, -1, -x], [y, x, -β]]
        r[0] = -sigma * w[0] + sigma * w[1];
        r[1] = (rho - z) * w[0] - w[1] - x * w[2];
        r[2] = y * w[0] + x * w[1] - beta * w[2];
    }
}

/// <summary>
/// Система Рёсслера (1976) — простейший ленточный хаотический аттрактор.
/// </summary>
/// <remarks>
/// <para>Уравнения:</para>
/// <code>
/// ẋ = −y − z
/// ẏ = x + ay
/// ż = b + z(x − c)
/// </code>
/// <para>
/// При a = 0.2, b = 0.2, c = 5.7 образует одноленточный аттрактор, у которого
/// расстояние между полосами уменьшается экспоненциально. Один из самых
/// визуально узнаваемых странных аттракторов.
/// </para>
/// </remarks>
public sealed class RosslerSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Rössler";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 1.0, 1.0, 0.0 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", 0.05, 0.5, 0.2,
        "Нелинейность. При a < 0.1 система часто уходит на периодику. " +
        "Классика — 0.2."),
        new("b", 0.05, 2.0, 0.2,
        "Внешний форсинг: слагаемое, сдвигающее центр спирали. " +
        "Классика — 0.2."),
        new("c", 1.0, 20.0, 5.7,
        "Параметр растяжения. При c > 4.6 — странный аттрактор. " +
        "Классика — 5.7."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.4, 90, 0, 0, 4);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0], b = p[1], c = p[2];
        d[0] = -s[1] - s[2];
        d[1] = s[0] + a * s[1];
        d[2] = b + s[2] * (s[0] - c);
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1], c = p[2];
        double x = s[0], y = s[1], z = s[2];

        // J = [[0, -1, -1], [1, a, 0], [z, 0, x-c]]
        r[0] = -w[1] - w[2];
        r[1] = w[0] + a * w[1];
        r[2] = z * w[0] + (x - c) * w[2];
    }
}

/// <summary>
/// Аттрактор Аидзавы — сферическая структура с центральной осью.
/// </summary>
/// <remarks>
/// Менее известен, чем Лоренц или Рёсслер, но даёт красивую визуальную
/// структуру: сфероид с осью, вокруг которой «вращается» хаотическая
/// траектория. Хорошо подходит для 3D-визуализации.
/// </remarks>
public sealed class AizawaSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Aizawa";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0, 0 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", 0.1, 2.0, 0.95, "Вертикальный фактор кубической нелинейности."),
        new("b", 0.1, 2.0, 0.7,  "Смещение по оси z. Определяет центр спирали."),
        new("c", 0.1, 2.0, 0.6,  "Постоянный сдвиг ż. Отвечает за закрутку."),
        new("d", 1.0, 5.0, 3.5,  "Скорость вращения вокруг оси z."),
        new("e", 0.05, 0.5, 0.25, "Связь радиального сжатия с высотой z."),
        new("f", 0.05, 0.5, 0.1,  "Кубический коэффициент в ż. Даёт сфероидальность."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.3, 120, 0, 0, 0.5);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3], e = p[4], f = p[5];
        double x = s[0], y = s[1], z = s[2];
        d[0] = (z - b) * x - dd * y;
        d[1] = dd * x + (z - b) * y;
        d[2] = c + a * z - z * z * z / 3.0 - (x * x + y * y) * (1 + e * z) + f * z * x * x * x;
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3], e = p[4], f = p[5];
        double x = s[0], y = s[1], z = s[2];

        double xy2 = x * x + y * y;

        // ∂ẋ/∂x = z-b, ∂ẋ/∂y = -d, ∂ẋ/∂z = x
        // ∂ẏ/∂x = d, ∂ẏ/∂y = z-b, ∂ẏ/∂z = y
        // ∂ż/∂x = -2x(1+ez) + 3f x² z
        // ∂ż/∂y = -2y(1+ez)
        // ∂ż/∂z = a - z² - (x²+y²)e + f x³
        r[0] = (z - b) * w[0] - dd * w[1] + x * w[2];
        r[1] = dd * w[0] + (z - b) * w[1] + y * w[2];
        r[2] = (-2 * x * (1 + e * z) + 3 * f * x * x * z) * w[0]
             + (-2 * y * (1 + e * z)) * w[1]
             + (a - z * z - xy2 * e + f * x * x * x) * w[2];
    }
}

/// <summary>
/// Система Томаса (1999) — циклостатический поток с тригонометрической связью.
/// </summary>
/// <remarks>
/// <para>Уравнения:</para>
/// <code>
/// ẋ = sin(y) − bx
/// ẏ = sin(z) − by
/// ż = sin(x) − bz
/// </code>
/// <para>
/// При b = 0.19 траектория заполняет «запутанный» куб — визуально
/// напоминает лабиринт. Отличный пример хаоса, порождённого
/// взаимодействием ортогональных синусов.
/// </para>
/// </remarks>
public sealed class ThomasSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Thomas";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0, 0 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [new("b", 0.05, 0.4, 0.19,
        "Диссипация: скорость «остывания» каждой компоненты. " +
        "При b = 0.19 траектория заполняет запутанный куб.")];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.6, -0.4, 60, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double b = p[0];
        d[0] = Math.Sin(s[1]) - b * s[0];
        d[1] = Math.Sin(s[2]) - b * s[1];
        d[2] = Math.Sin(s[0]) - b * s[2];
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double b = p[0];
        double x = s[0], y = s[1], z = s[2];

        // J = [[-b, cos(y), 0], [0, -b, cos(z)], [cos(x), 0, -b]]
        r[0] = -b * w[0] + Math.Cos(y) * w[1];
        r[1] = -b * w[1] + Math.Cos(z) * w[2];
        r[2] = Math.Cos(x) * w[0] - b * w[2];
    }
}

/// <summary>
/// Аттрактор Хальворсена — циклически-симметричная система.
/// </summary>
/// <remarks>
/// Все три уравнения симметричны относительно циклической перестановки
/// переменных (x → y → z → x). Из-за этого аттрактор имеет ось симметрии
/// третьего порядка.
/// </remarks>
public sealed class HalvorsenSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Halvorsen";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { -1.48, -1.51, 2.04 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [new("a", 1.0, 3.0, 1.89,
        "Линейное сжатие. Значение 1.89 даёт классический аттрактор.")];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.4, -0.3, 55, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0];
        double x = s[0], y = s[1], z = s[2];
        d[0] = -a * x - 4 * y - 4 * z - y * y;
        d[1] = -a * y - 4 * z - 4 * x - z * z;
        d[2] = -a * z - 4 * x - 4 * y - x * x;
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0];
        double x = s[0], y = s[1], z = s[2];

        // J = [[-a, -4-2y, -4], [-4, -a, -4-2z], [-4-2x, -4, -a]]
        r[0] = -a * w[0] + (-4 - 2 * y) * w[1] - 4 * w[2];
        r[1] = -4 * w[0] - a * w[1] + (-4 - 2 * z) * w[2];
        r[2] = (-4 - 2 * x) * w[0] - 4 * w[1] - a * w[2];
    }
}

/// <summary>
/// Система Чена (1999) — двойник Лоренца, но не топологически эквивалентный.
/// </summary>
/// <remarks>
/// <para>
/// Формально похожа на Лоренца, но имеет другую структуру: аттрактор Chen
/// не может быть получен непрерывной деформацией аттрактора Lorenz.
/// </para>
/// <para>
/// При a = 35, b = 3, c = 28 даёт λ₁ ≈ 2.03 — почти вдвое быстрее Лоренца.
/// </para>
/// </remarks>
public sealed class ChenSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Chen";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { -0.1, 0.5, -0.6 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", 20, 45, 35, "Скорость подстройки x к y. Классика — 35."),
        new("b", 1, 6, 3,    "Диссипация по z. Классика — 3."),
        new("c", 20, 32, 28, "Растяжение. При c > a — странный аттрактор. Классика — 28."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.4, -0.35, 8, 0, 0, 22);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0], b = p[1], c = p[2];
        d[0] = a * (s[1] - s[0]);
        d[1] = (c - a) * s[0] - s[0] * s[2] + c * s[1];
        d[2] = s[0] * s[1] - b * s[2];
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1], c = p[2];
        double x = s[0], y = s[1], z = s[2];

        // J = [[-a, a, 0], [c-a-z, c, -x], [y, x, -b]]
        r[0] = -a * w[0] + a * w[1];
        r[1] = (c - a - z) * w[0] + c * w[1] - x * w[2];
        r[2] = y * w[0] + x * w[1] - b * w[2];
    }
}

/// <summary>
/// Система Дадраса (2009) — трёхспиральный хаотический аттрактор.
/// </summary>
/// <remarks>
/// Имеет три «крыла» вместо двух у Лоренца. Даёт более богатую структуру,
/// но требует более тонкой настройки начальных условий для визуализации.
/// </remarks>
public sealed class DadrasSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Dadras";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 1.1, 2.1, -2.0 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", 1, 5, 3,    "Линейная диссипация по x."),
        new("b", 1, 5, 2.7,  "Квадратичная связь y·z."),
        new("c", 1, 3, 1.7,  "Коэффициент линейного роста y."),
        new("d", 1, 3, 2,    "Связь x·y со скоростью z."),
        new("e", 5, 12, 9,   "Диссипация z. Определяет «сжатие» третьего крыла."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.3, 55, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3], e = p[4];
        d[0] = s[1] - a * s[0] + b * s[1] * s[2];
        d[1] = c * s[1] - s[0] * s[2] + s[2];
        d[2] = dd * s[0] * s[1] - e * s[2];
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1], c = p[2], dd = p[3], e = p[4];
        double x = s[0], y = s[1], z = s[2];

        // J = [[-a, 1+bz, b y], [-z, c, 1-x], [d y, d x, -e]]
        r[0] = -a * w[0] + (1 + b * z) * w[1] + b * y * w[2];
        r[1] = -z * w[0] + c * w[1] + (1 - x) * w[2];
        r[2] = dd * y * w[0] + dd * x * w[1] - e * w[2];
    }
}

/// <summary>
/// Четырёхкрылый аттрактор — редкий случай системы с четырьмя «крыльями».
/// </summary>
/// <remarks>
/// Большинство хаотических потоков имеют 1 или 2 «крыла». Четырёхкрылые
/// системы редки и представляют теоретический интерес для классификации
/// топологии аттракторов.
/// </remarks>
public sealed class FourWingSystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Four-Wing";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.1, 0.1 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("a", 0.1, 0.4, 0.2,   "Линейное растяжение x. Формирует «крылья»."),
        new("b", 0.001, 0.05, 0.01, "Слабая связь x в ẏ. Создаёт асимметрию."),
        new("c", -0.7, -0.2, -0.4, "Отрицательная диссипация y. Условие хаоса."),
    ];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.6, -0.4, 40, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0], b = p[1], c = p[2];
        d[0] = a * s[0] + s[1] * s[2];
        d[1] = b * s[0] + c * s[1] - s[0] * s[2];
        d[2] = -s[2] - s[0] * s[1];
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1], c = p[2];
        double x = s[0], y = s[1], z = s[2];

        // J = [[a, z, y], [b-z, c, -x], [-y, -x, -1]]
        r[0] = a * w[0] + z * w[1] + y * w[2];
        r[1] = (b - z) * w[0] + c * w[1] - x * w[2];
        r[2] = -y * w[0] - x * w[1] - w[2];
    }
}

/// <summary>
/// Система Спотта A — минимальная квадратичная хаотическая система.
/// </summary>
/// <remarks>
/// Не имеет параметров. Один из 19 простейших квадратичных потоков,
/// найденных Дж. К. Споттом при систематическом переборе (1994).
/// </remarks>
public sealed class SprottASystem : ChaoticSystem
{
    /// <inheritdoc/>
    public override string Name => "Sprott A";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 3;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => new[] { 0.1, 0.1, 0.1 };
    /// <inheritdoc/>
    public override ParameterDef[] Parameters => [];
    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.4, 55, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] _)
    {
        d[0] = s[1];
        d[1] = -s[0] + s[1] * s[2];
        d[2] = 1 - s[1] * s[1];
    }

    /// <inheritdoc/>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        double x = s[0], y = s[1], z = s[2];

        // J = [[0, 1, 0], [-1, z, y], [0, -2y, 0]]
        r[0] = w[1];
        r[1] = -w[0] + z * w[1] + y * w[2];
        r[2] = -2 * y * w[1];
    }
}

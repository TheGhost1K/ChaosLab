namespace ChaosLab.Core;

/// <summary>
/// Описание параметра хаотической системы.
/// </summary>
/// <param name="Name">Отображаемое имя (обычно греческая буква, например σ).</param>
/// <param name="Min">Минимальное значение для UI-слайдера.</param>
/// <param name="Max">Максимальное значение для UI-слайдера.</param>
/// <param name="Default">Значение по умолчанию при загрузке системы.</param>
/// <param name="Description">
/// Необязательное описание для тултипа. По умолчанию — <c>null</c>
/// (тултип не показывается). Рекомендуется кратко пояснять физический
/// или математический смысл параметра.
/// </param>
public sealed record ParameterDef(string Name, double Min, double Max, double Default, string? Description = null)
{
    /// <summary>Шаг по умолчанию для тонкой настройки слайдера (1/500 от диапазона).</summary>
    public double Step => (Max - Min) / 500.0;
}

/// <summary>
/// Общий контракт для всех вычислимых моделей: точечных систем и 2D-полей.
/// Позволяет UI работать с ними единообразно через реестр <see cref="SystemCatalog"/>.
/// </summary>
public interface ISimulation
{
    /// <summary>Отображаемое имя системы.</summary>
    string Name { get; }

    /// <summary>Категория: <c>"Flow"</c>, <c>"Map"</c> или <c>"Field"</c>.</summary>
    string Category { get; }

    /// <summary>Список параметров с диапазонами и значениями по умолчанию.</summary>
    ParameterDef[] Parameters { get; }
}

/// <summary>
/// Утилиты расширения для <see cref="ISimulation"/>.
/// </summary>
public static class SimulationExtensions
{
    /// <summary>
    /// Возвращает массив значений по умолчанию для всех параметров системы.
    /// </summary>
    /// <param name="s">Целевая система.</param>
    /// <returns>Новый массив значений по умолчанию.</returns>
    public static double[] DefaultParameters(this ISimulation s)
    {
        var p = new double[s.Parameters.Length];
        for (int i = 0; i < p.Length; i++) p[i] = s.Parameters[i].Default;
        return p;
    }
}

/// <summary>
/// Базовый класс хаотической системы: потока (<c>ẋ = f(x)</c>) или отображения
/// (<c>xₙ₊₁ = f(xₙ)</c>).
/// </summary>
/// <remarks>
/// Наследники должны переопределить либо <see cref="Derivatives"/> (для потоков),
/// либо <see cref="Next"/> (для дискретных систем). Тип системы определяется
/// свойством <see cref="IsDiscrete"/>.
/// </remarks>
public abstract class ChaoticSystem : ISimulation
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string Category { get; }

    /// <summary>Размерность фазового пространства системы.</summary>
    public abstract int Dimension { get; }

    /// <inheritdoc/>
    public abstract ParameterDef[] Parameters { get; }

    /// <summary>Начальное состояние по умолчанию (обычно близкое к аттрактору).</summary>
    public abstract double[] DefaultInitialState { get; }

    private double[]? _cachedInitialState;

    /// <summary>
    /// Кэшированная версия <see cref="DefaultInitialState"/>. Возвращает тот же
    /// массив при повторных вызовах. <b>Не мутировать</b> — использовать
    /// только для чтения. Потокобезопасна.
    /// </summary>
    /// <remarks>
    /// Используется в горячих циклах (LyapunovMap, Bifurcation, Correlation
    /// Dimension), где <see cref="DefaultInitialState"/> создавал бы новый массив
    /// на каждый вызов — сотни тысяч аллокаций за один рендер.
    /// </remarks>
    public double[] DefaultInitialStateCached
    {
        get
        {
            if (_cachedInitialState != null) return _cachedInitialState;
            var fresh = DefaultInitialState;
            System.Threading.Interlocked.CompareExchange(ref _cachedInitialState, fresh, null);
            return _cachedInitialState!;
        }
    }

    /// <summary>
    /// Сбрасывает кэш <see cref="DefaultInitialStateCached"/>. Вызывается
    /// системами, у которых начальное состояние может меняться (например,
    /// Lorenz-96 при смене N).
    /// </summary>
    protected void InvalidateCachedInitialState() => _cachedInitialState = null;

    /// <summary>
    /// <c>true</c> — дискретное отображение, <c>false</c> — непрерывный поток.
    /// </summary>
    public virtual bool IsDiscrete => false;

    /// <summary>Параметры камеры по умолчанию: (yaw, pitch, zoom, cx, cy, cz).</summary>
    public virtual (double yaw, double pitch, double zoom, double cx, double cy, double cz)
        DefaultCamera => (0.4, -0.35, 20, 0, 0, 0);

    /// <summary>Диапазон значений координаты для бифуркационной диаграммы (отображения).</summary>
    public virtual (double lo, double hi) BifurcationRange => (0, 1);

    /// <summary>
    /// Правые части ОДУ: <c>d = f(s, p)</c>. Используется интегратором RK4.
    /// </summary>
    /// <param name="s">Текущее состояние (размер = <see cref="Dimension"/>).</param>
    /// <param name="d">Результат: производная d/dt в точке s.</param>
    /// <param name="p">Значения параметров.</param>
    public virtual void Derivatives(double[] s, double[] d, double[] p) { }

    /// <summary>
    /// Один шаг дискретного отображения: <c>n = f(s, p)</c>.
    /// </summary>
    /// <param name="s">Текущее состояние.</param>
    /// <param name="n">Результат: следующее состояние.</param>
    /// <param name="p">Значения параметров.</param>
    public virtual void Next(double[] s, double[] n, double[] p) { }

    /// <summary>
    /// Проекция N-мерного состояния на 3D-подпространство для визуализации.
    /// По умолчанию возвращает первые три компоненты.
    /// </summary>
    /// <param name="s">Полное состояние.</param>
    /// <param name="xyz">Результат: три координаты.</param>
    public virtual void Project3D(double[] s, Span<double> xyz)
    {
        xyz[0] = s.Length > 0 ? s[0] : 0;
        xyz[1] = s.Length > 1 ? s[1] : 0;
        xyz[2] = s.Length > 2 ? s[2] : 0;
    }

    /// <summary>Возвращает массив значений параметров по умолчанию.</summary>
    public double[] DefaultParameters()
    {
        var p = new double[Parameters.Length];
        for (int i = 0; i < p.Length; i++) p[i] = Parameters[i].Default;
        return p;
    }

    /// <summary>
    /// Произведение Якобиана <c>J(x)</c> на вектор <c>w</c>.
    /// По умолчанию — численная directional derivative. Переопределяется
    /// в системах с известным аналитическим якобианом (в 2–3 раза быстрее).
    /// </summary>
    /// <param name="s">Точка фазового пространства.</param>
    /// <param name="w">Вектор, к которому применяется J.</param>
    /// <param name="result">Результат: <c>J(s)·w</c>.</param>
    /// <param name="p">Значения параметров.</param>
    /// <remarks>
    /// Используется вариационным методом расчёта показателей Ляпунова и карты
    /// Ляпунова. Численный fallback надёжен, но в 2–3 раза медленнее.
    /// </remarks>
    public virtual void JacobianVectorProduct(
        double[] s, double[] w, double[] result, double[] p)
    {
        int n = Dimension;
        const double eps = 1e-7;

        // Нормализуем w, чтобы eps не «съел» масштаб
        double wnorm2 = 0.0;
        for (int i = 0; i < n; i++) wnorm2 += w[i] * w[i];
        double wnorm = Math.Sqrt(wnorm2);
        if (wnorm < 1e-14) { Array.Clear(result, 0, n); return; }

        double h = eps / wnorm;

        // Аллоцируем один раз — fallback вызывается редко
        var f0 = new double[n];
        var xp = new double[n];
        var fp = new double[n];

        for (int i = 0; i < n; i++) xp[i] = s[i] + h * w[i];

        if (IsDiscrete)
        {
            Next(s, f0, p);
            Next(xp, fp, p);
        }
        else
        {
            Derivatives(s, f0, p);
            Derivatives(xp, fp, p);
        }

        for (int i = 0; i < n; i++)
            result[i] = (fp[i] - f0[i]) / h;
    }

    /// <summary>Возвращает отображаемое имя системы.</summary>
    /// <returns>Значение <see cref="Name"/>.</returns>
    public override string ToString() => Name;
}

/// <summary>
/// Переиспользуемые буферы для RK4-интегратора.
/// Исключает аллокации в горячем цикле (миллионы шагов на кадр).
/// </summary>
public sealed class Rk4Scratch
{
    /// <summary>Коэффициенты k₁ (производная в текущей точке).</summary>
    public readonly double[] K1;
    /// <summary>Коэффициенты k₂.</summary>
    public readonly double[] K2;
    /// <summary>Коэффициенты k₃.</summary>
    public readonly double[] K3;
    /// <summary>Коэффициенты k₄.</summary>
    public readonly double[] K4;
    /// <summary>Временный буфер для промежуточных состояний.</summary>
    public readonly double[] Tmp;

    /// <summary>
    /// Создаёт буферы под размерность <paramref name="n"/>.
    /// </summary>
    public Rk4Scratch(int n)
    {
        K1 = new double[n]; K2 = new double[n];
        K3 = new double[n]; K4 = new double[n];
        Tmp = new double[n];
    }
}

/// <summary>
/// Переиспользуемые буферы для вариационного RK4 (совместно x и w).
/// </summary>
public sealed class VariationalScratch
{
    /// <summary>Стадии RK4 для траектории.</summary>
    public readonly double[] K1x, K2x, K3x, K4x;
    /// <summary>Стадии RK4 для вектора отклонения.</summary>
    public readonly double[] K1w, K2w, K3w, K4w;
    /// <summary>Временные буферы.</summary>
    public readonly double[] Tmp, TmpW, ResultW;

    /// <summary>Создаёт буферы под размерность <paramref name="n"/>.</summary>
    public VariationalScratch(int n)
    {
        K1x = new double[n]; K2x = new double[n];
        K3x = new double[n]; K4x = new double[n];
        K1w = new double[n]; K2w = new double[n];
        K3w = new double[n]; K4w = new double[n];
        Tmp = new double[n]; TmpW = new double[n];
        ResultW = new double[n];
    }
}

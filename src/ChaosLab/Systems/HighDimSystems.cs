using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Lorenz-96 (1995) — N-мерная модель атмосферной динамики средних широт.
/// </summary>
/// <remarks>
/// <code>
/// ẋⱼ = (x_{j+1} − x_{j−2})·x_{j−1} − xⱼ + F
/// </code>
/// <para>Периодические границы. При F = 8 и N ≥ 8 даёт полностью развитый
/// хаос с несколькими положительными показателями Ляпунова (гиперхаос).</para>
/// <para>Широко используется как эталон в data assimilation и ML для
/// динамических систем.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Lorenz_96_model"/>
public sealed class Lorenz96System : ChaoticSystem
{
    private int _n = 8;
    private double[] _initialState = Array.Empty<double>();

    /// <summary>Размерность модели N (4–32).</summary>
    /// <remarks>
    /// При изменении значения автоматически пересоздаётся
    /// <see cref="DefaultInitialState"/> (случайное состояние с seed = 42).
    /// </remarks>
    public int N
    {
        get => _n;
        set
        {
            if (value < 1) value = 1;
            if (value == _n) return;
            _n = value;
            RegenerateInitialState();
        }
    }
    /// <inheritdoc/>
    public override string Name => "Lorenz-96";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => N;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => _initialState;

    /// <summary>Инициализирует систему со случайным начальным состоянием.</summary>
    public Lorenz96System()
    {
        RegenerateInitialState();
    }

    private void RegenerateInitialState()
    {
        var rng = new Random(42);
        var arr = new double[_n];
        for (int i = 0; i < _n; i++) arr[i] = rng.NextDouble() * 2 - 1;
        _initialState = arr;

        // Сбрасываем кэш базового класса — иначе DefaultInitialStateCached
        // вернёт старый массив.
        InvalidateCachedInitialState();
    }

    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("F", 4, 20, 8,
            "Внешний форсинг. При F < 5 — периодика, при F > 8 — гиперхаос."),
        new("N", 4, 32, 8,
            "Размерность модели. При смене пересоздаётся начальное состояние " +
            "и меняется число положительных показателей Ляпунова."),
    ];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.35, 30, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double F = p[0];
        int n = s.Length;
        for (int j = 0; j < n; j++)
        {
            int jm1 = (j - 1 + n) % n;
            int jm2 = (j - 2 + n) % n;
            int jp1 = (j + 1) % n;
            d[j] = (s[jp1] - s[jm2]) * s[jm1] - s[j] + F;
        }
    }

    /// <summary>
    /// Разреженный JVP: 4 ненулевых элемента на строку якобиана.
    /// Сложность O(N) вместо O(N²).
    /// </summary>
    public override void JacobianVectorProduct(
    double[] s, double[] w, double[] r, double[] p)
    {
        int n = s.Length;

        // ẋⱼ = (x_{j+1} - x_{j-2})·x_{j-1} - xⱼ + F
        //
        // ∂ẋⱼ/∂x_{j+1}  = x_{j-1}
        // ∂ẋⱼ/∂x_{j-1}  = x_{j+1} - x_{j-2}
        // ∂ẋⱼ/∂x_{j-2}  = -x_{j-1}
        // ∂ẋⱼ/∂xⱼ      = -1
        //
        // (J·w)ⱼ = x_{j-1}·w_{j+1} + (x_{j+1} - x_{j-2})·w_{j-1}
        //        - x_{j-1}·w_{j-2} - wⱼ
        for (int j = 0; j < n; j++)
        {
            int jm1 = (j - 1 + n) % n;
            int jm2 = (j - 2 + n) % n;
            int jp1 = (j + 1) % n;

            r[j] = s[jm1] * w[jp1]
                 + (s[jp1] - s[jm2]) * w[jm1]
                 - s[jm1] * w[jm2]
                 - w[j];
        }
    }

    /// <summary>Проекция на первые три компоненты для 3D-визуализации.</summary>
    public override void Project3D(double[] s, Span<double> xyz)
    {
        xyz[0] = s[0];
        xyz[1] = s.Length > 1 ? s[1] : 0;
        xyz[2] = s.Length > 2 ? s[2] : 0;
    }
}

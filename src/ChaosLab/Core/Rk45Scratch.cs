namespace ChaosLab.Core;

/// <summary>
/// Переиспользуемые буферы для адаптивного интегратора Dormand-Prince RK45.
/// </summary>
/// <remarks>
/// <para>7 стадий × N компонент. Массивы <c>K1..K7</c> хранят производные
/// на каждой стадии, <c>Tmp</c> — временное состояние для вычислений,
/// <c>Err</c> — оценка локальной ошибки.</para>
/// </remarks>
public sealed class Rk45Scratch
{
    /// <summary>Производная на стадии 1.</summary>
    public readonly double[] K1;
    /// <summary>Производная на стадии 2.</summary>
    public readonly double[] K2;
    /// <summary>Производная на стадии 3.</summary>
    public readonly double[] K3;
    /// <summary>Производная на стадии 4.</summary>
    public readonly double[] K4;
    /// <summary>Производная на стадии 5.</summary>
    public readonly double[] K5;
    /// <summary>Производная на стадии 6.</summary>
    public readonly double[] K6;
    /// <summary>Производная на стадии 7.</summary>
    public readonly double[] K7;
    /// <summary>Временное состояние.</summary>
    public readonly double[] Tmp;
    /// <summary>Оценка локальной ошибки.</summary>
    public readonly double[] Err;

    /// <summary>Создаёт буферы под размерность <paramref name="n"/>.</summary>
    public Rk45Scratch(int n)
    {
        K1 = new double[n]; K2 = new double[n]; K3 = new double[n];
        K4 = new double[n]; K5 = new double[n]; K6 = new double[n];
        K7 = new double[n];
        Tmp = new double[n];
        Err = new double[n];
    }
}

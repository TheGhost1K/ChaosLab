namespace ChaosLab.Core;

/// <summary>
/// Палитра на основе косинусной формулы Иниго Килеза (Inigo Quilez):
/// <c>color(t) = a + b·cos(2π(c·t + d))</c>.
/// </summary>
/// <remarks>
/// Предвычисляется в lookup-таблицу из 1024 значений при создании.
/// Формат цвета — Bgr32 (0x00RRGGBB), готов для <c>WriteableBitmap</c>.
/// </remarks>
public sealed class Palette
{
    /// <summary>Человекочитаемое имя палитры для UI.</summary>
    public string Name { get; }

    private readonly int[] _lut = new int[1024];

    /// <summary>
    /// Создаёт палитру по формуле Килеза.
    /// </summary>
    /// <param name="name">Отображаемое имя.</param>
    /// <param name="a">Смещение (по одному на R, G, B).</param>
    /// <param name="b">Амплитуда.</param>
    /// <param name="c">Частота.</param>
    /// <param name="d">Фаза.</param>
    public Palette(string name, double[] a, double[] b, double[] c, double[] d)
    {
        Name = name;
        for (int i = 0; i < 1024; i++)
        {
            double t = i / 1023.0;
            double r = a[0] + b[0] * Math.Cos(2 * Math.PI * (c[0] * t + d[0]));
            double g = a[1] + b[1] * Math.Cos(2 * Math.PI * (c[1] * t + d[1]));
            double bl = a[2] + b[2] * Math.Cos(2 * Math.PI * (c[2] * t + d[2]));
            int ri = (int)(Math.Clamp(r, 0, 1) * 255);
            int gi = (int)(Math.Clamp(g, 0, 1) * 255);
            int bi = (int)(Math.Clamp(bl, 0, 1) * 255);
            _lut[i] = (ri << 16) | (gi << 8) | bi;   // 0x00RRGGBB (Bgr32)
        }
    }

    /// <summary>
    /// Возвращает цвет для параметра <paramref name="t"/> ∈ [0, 1].
    /// </summary>
    /// <param name="t">Нормализованный параметр (0 = начало, 1 = конец).</param>
    /// <returns>Упакованный Bgr32-цвет.</returns>
    public int Sample(double t)
    {
        int i = (int)(t * 1023.0);
        if (i < 0) i = 0; else if (i > 1023) i = 1023;
        return _lut[i];
    }

    /// <summary>Все доступные палитры приложения.</summary>
    public static readonly Palette[] All =
    {
        new("Inferno", new[]{0.5,0.35,0.25}, new[]{0.5,0.4,0.35}, new[]{1.0,1.0,1.0}, new[]{0.00,0.15,0.30}),
        new("Plasma",  new[]{0.5,0.5,0.5},  new[]{0.5,0.5,0.5},  new[]{1.0,1.0,1.0}, new[]{0.00,0.25,0.50}),
        new("Sunset",  new[]{0.5,0.5,0.5},  new[]{0.5,0.5,0.5},  new[]{1.0,0.7,0.4}, new[]{0.00,0.15,0.20}),
        new("Emerald", new[]{0.4,0.6,0.5},  new[]{0.3,0.4,0.3},  new[]{1.0,1.0,1.0}, new[]{0.10,0.20,0.30}),
        new("Ice",     new[]{0.4,0.5,0.6},  new[]{0.4,0.4,0.5},  new[]{1.0,1.0,1.0}, new[]{0.20,0.40,0.60}),
        new("Rainbow", new[]{0.5,0.5,0.5},  new[]{0.5,0.5,0.5},  new[]{1.0,1.0,1.0}, new[]{0.00,0.33,0.67}),
        new("Fire",    new[]{0.5,0.4,0.3},  new[]{0.5,0.5,0.4},  new[]{1.0,1.0,1.0}, new[]{0.00,0.05,0.10}),
        new("Mono",    new[]{0.5,0.5,0.5},  new[]{0.5,0.5,0.5},  new[]{1.0,1.0,1.0}, new[]{0.00,0.00,0.00}),
        new("GS-Chem", new[]{0.5,0.6,0.5},  new[]{0.5,0.4,0.5},  new[]{1.0,1.0,1.0}, new[]{0.40,0.30,0.10}),
    };

    /// <summary>
    /// Находит палитру по имени за O(n).
    /// </summary>
    /// <param name="name">Имя палитры.</param>
    /// <returns>Найденная палитра или <c>All[0]</c>, если имя не найдено.</returns>
    public static Palette ByName(string name)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].Name == name)
                return All[i];
        return All[0];
    }
}

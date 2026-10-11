using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Живой след траектории — «хвост» из последних N точек с плавным
/// затуханием цвета от хвоста к голове.
/// </summary>
/// <remarks>
/// <para>Используется в режиме анимации для отображения **динамики**, а
/// не структуры аттрактора. В отличие от <see cref="Rendering.AttractorRenderer"/>,
/// не накапливает плотность, а показывает траекторию во времени.</para>
/// <para>Потокобезопасен: список точек защищён <c>lock</c>.</para>
/// </remarks>
public sealed class AnimatedTrail
{
    /// <summary>Длина хвоста в точках (по умолчанию 4000).</summary>
    public int TrailLength { get; set; } = 4000;
    private readonly LinkedList<(double x, double y, double z)> _points = new();
    private readonly Lock _lock = new();

    /// <summary>Очищает след.</summary>
    public void Clear() { lock (_lock) _points.Clear(); }

    /// <summary>
    /// Добавляет точку в голову следа; при превышении <see cref="TrailLength"/>
    /// удаляет самую старую.
    /// </summary>
    /// <param name="x">X-координата.</param>
    /// <param name="y">Y-координата.</param>
    /// <param name="z">Z-координата (для 2D передаётся 0).</param>
    public void Add(double x, double y, double z)
    {
        lock (_lock)
        {
            _points.AddLast((x, y, z));
            while (_points.Count > TrailLength) _points.RemoveFirst();
        }
    }

    /// <summary>
    /// Отрисовывает след с интерполяцией цвета от тёмного хвоста к яркой голове.
    /// </summary>
    /// <param name="pixels">Целевой Bgr32-буфер.</param>
    /// <param name="w">Ширина.</param>
    /// <param name="h">Высота.</param>
    /// <param name="cam">Камера для проекции.</param>
    /// <param name="headColor">Цвет головы (хвост — тёмно-серый).</param>
    public void Render(int[] pixels, int w, int h, Camera3D cam, int headColor)
    {
        lock (_lock)
        {
            int n = _points.Count;
            if (n == 0) return;

            int i = 0;
            foreach (var (x, y, z) in _points)
            {
                var (px, py) = cam.Project(x, y, z);
                if ((uint)px < (uint)w && (uint)py < (uint)h)
                {
                    double t = (double)i / n;   // 0 = хвост, 1 = голова
                    int col = LerpColor(0x1A2530, headColor, t);
                    pixels[py * w + px] = col;
                }
                i++;
            }
        }
    }

    private static int LerpColor(int a, int b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        int ar = (a >> 16) & 0xFF, ag = (a >> 8) & 0xFF, ab = a & 0xFF;
        int br = (b >> 16) & 0xFF, bg = (b >> 8) & 0xFF, bb = b & 0xFF;
        int r = (int)(ar + (br - ar) * t);
        int g = (int)(ag + (bg - ag) * t);
        int bl = (int)(ab + (bb - ab) * t);
        return (r << 16) | (g << 8) | bl;
    }
}

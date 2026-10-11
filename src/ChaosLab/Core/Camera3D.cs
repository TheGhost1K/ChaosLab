namespace ChaosLab.Core;

/// <summary>
/// Ортографическая 3D-камера с параметрами yaw/pitch/zoom.
/// Проецирует точки фазового пространства (x, y, z) на экранные координаты.
/// </summary>
/// <remarks>
/// Матрица поворота пересчитывается лениво — только когда изменяется
/// хотя бы один из углов (<see cref="Yaw"/>, <see cref="Pitch"/>).
/// Проекция — чистая функция без состояния.
/// </remarks>
public sealed class Camera3D
{
    /// <summary>Угол поворота вокруг вертикальной оси Z, радианы.</summary>
    public double Yaw { get => _yaw; set { _yaw = value; _dirty = true; } }

    /// <summary>Угол наклона, радианы. Ограничивается ±π/2.</summary>
    public double Pitch { get => _pitch; set { _pitch = value; _dirty = true; } }

    /// <summary>Масштаб: пикселей на единицу фазового пространства.</summary>
    public double Zoom { get => _zoom; set { _zoom = value; _dirty = true; } }

    /// <summary>Координата X центра взгляда в фазовом пространстве.</summary>
    public double CenterX { get => _cx; set { _cx = value; _dirty = true; } }

    /// <summary>Координата Y центра взгляда.</summary>
    public double CenterY { get => _cy2; set { _cy2 = value; _dirty = true; } }

    /// <summary>Координата Z центра взгляда.</summary>
    public double CenterZ { get => _cz; set { _cz = value; _dirty = true; } }

    /// <summary>Ширина целевого холста в пикселях.</summary>
    public double ScreenW { get; set; }

    /// <summary>Высота целевого холста в пикселях.</summary>
    public double ScreenH { get; set; }

    private double _yaw = 0.5, _pitch = -0.35, _zoom = 20;
    private double _cx, _cy2, _cz;
    private double _cy, _sy, _cp, _sp;
    private bool _dirty = true;

    /// <summary>
    /// Создаёт независимую копию камеры.
    /// Используется для передачи снимка камеры в фоновый поток рендера.
    /// </summary>
    /// <returns>Новый экземпляр <see cref="Camera3D"/> с теми же параметрами.</returns>
    public Camera3D Clone() => new()
    {
        Yaw = _yaw,
        Pitch = _pitch,
        Zoom = _zoom,
        CenterX = _cx,
        CenterY = _cy2,
        CenterZ = _cz,
        ScreenW = ScreenW,
        ScreenH = ScreenH,
    };

    /// <summary>Копирует состояние другой камеры в текущую.</summary>
    /// <param name="other">Источник данных.</param>
    public void CopyFrom(Camera3D other)
    {
        Yaw = other._yaw; Pitch = other._pitch; Zoom = other._zoom;
        CenterX = other._cx; CenterY = other._cy2; CenterZ = other._cz;
        ScreenW = other.ScreenW; ScreenH = other.ScreenH;
    }

    private void Update()
    {
        _cy = Math.Cos(_yaw); _sy = Math.Sin(_yaw);
        _cp = Math.Cos(_pitch); _sp = Math.Sin(_pitch);
        _dirty = false;
    }

    /// <summary>
    /// Проецирует точку фазового пространства на экран.
    /// </summary>
    /// <param name="x">X-координата в фазовом пространстве.</param>
    /// <param name="y">Y-координата.</param>
    /// <param name="z">Z-координата (для 2D-систем передаётся 0).</param>
    /// <returns>Экранные координаты (пиксели), целые числа.</returns>
    public (int x, int y) Project(double x, double y, double z)
    {
        if (_dirty) Update();
        double dx = x - _cx, dy = y - _cy2, dz = z - _cz;
        double x1 = _cy * dx - _sy * dy;
        double y1 = _sy * dx + _cy * dy;
        double y2 = _cp * y1 - _sp * dz;
        return ((int)(x1 * _zoom + ScreenW * 0.5),
                (int)(y2 * _zoom + ScreenH * 0.5));
    }
}

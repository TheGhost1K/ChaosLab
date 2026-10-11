using ChaosLab.Core;

namespace ChaosLab.ViewModels;

/// <summary>
/// ViewModel камеры. Оборачивает <see cref="Core.Camera3D"/> и предоставляет
/// свойства, привязанные к слайдерам UI.
/// </summary>
public sealed class CameraViewModel : BaseViewModel
{
    private double _yaw = 0.5;
    private double _pitch = -0.35;
    private double _zoom = 20;
    private double _centerX, _centerY, _centerZ;
    private bool _enabled = true;

    /// <summary>Угол поворота вокруг вертикальной оси.</summary>
    public double Yaw { get => _yaw; set { if (Set(ref _yaw, value)) OnPropertyChangedThrottled(nameof(YawText)); } }

    /// <summary>Угол наклона.</summary>
    public double Pitch { get => _pitch; set { if (Set(ref _pitch, value)) OnPropertyChangedThrottled(nameof(PitchText)); } }

    /// <summary>Масштаб (пикселей на единицу фазового пространства).</summary>
    public double Zoom { get => _zoom; set { if (Set(ref _zoom, value)) OnPropertyChangedThrottled(nameof(ZoomText)); } }

    /// <summary>Координата X центра взгляда в фазовом пространстве.</summary>
    public double CenterX { get => _centerX; set => Set(ref _centerX, value); }

    /// <summary>Координата Y центра взгляда.</summary>
    public double CenterY { get => _centerY; set => Set(ref _centerY, value); }

    /// <summary>Координата Z центра взгляда.</summary>
    public double CenterZ { get => _centerZ; set => Set(ref _centerZ, value); }

    /// <summary>Доступны ли контролы камеры (для 2D-систем Yaw/Pitch отключены).</summary>
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }

    /// <summary>Yaw в виде текста для отображения.</summary>
    public string YawText => Yaw.ToString("F2");

    /// <summary>Pitch в виде текста.</summary>
    public string PitchText => Pitch.ToString("F2");

    /// <summary>Zoom в виде текста.</summary>
    public string ZoomText => Zoom.ToString("F0");

    /// <summary>Применяет камеру по умолчанию для системы.</summary>
    public void ApplyDefault(ChaoticSystem cs)
    {
        var (yaw, pitch, zoom, cx, cy, cz) = cs.DefaultCamera;
        Yaw = yaw; Pitch = pitch; Zoom = zoom;
        CenterX = cx; CenterY = cy; CenterZ = cz;
    }

    /// <summary>Создаёт снимок камеры для передачи в фоновый поток.</summary>
    public Camera3D Snapshot(int w, int h) => new()
    {
        Yaw = Yaw,
        Pitch = Pitch,
        Zoom = Zoom,
        CenterX = CenterX,
        CenterY = CenterY,
        CenterZ = CenterZ,
        ScreenW = w,
        ScreenH = h,
    };

    /// <summary>Вращает камеру на (dx, dy) в пикселях.</summary>
    public void Rotate(double dx, double dy)
    {
        Yaw += dx * 0.01;
        Pitch = Math.Clamp(Pitch + dy * 0.01, -1.5, 1.5);
    }

    /// <summary>Панорамирует (для 2D систем).</summary>
    public void Pan(double dx, double dy)
    {
        CenterX -= dx / Zoom;
        CenterY += dy / Zoom;
    }

    /// <summary>Умножает Zoom на коэффициент.</summary>
    public void ZoomBy(double factor) => Zoom = Math.Clamp(Zoom * factor, 1, 500);
}

using ChaosLab.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChaosLab.Views;

/// <summary>
/// Главное окно приложения.
/// </summary>
/// <remarks>
/// Code-behind минимальный: только обработчики мыши и управление
/// размером холста. Вся логика — в <see cref="MainViewModel"/>.
/// </remarks>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private bool _dragging;
    private Point _lastMouse;
    private int _lastResizeW;
    private int _lastResizeH;
    private System.Windows.Threading.DispatcherTimer? _resizeTimer;
    private int _pendingW, _pendingH;

    /// <summary>Создаёт окно и инициализирует привязки.</summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += (_, _) => ApplyCanvasSize();
        CompositionTarget.Rendering += OnFrameRendering;
    }

    /// <summary>Обработчик изменения размера холста с debounce.</summary>
    private void CanvasHost_SizeChanged(object sender, SizeChangedEventArgs e)
        => ApplyCanvasSize();

    private void ApplyCanvasSize()
    {
        if (CanvasHost.ActualWidth < 1 || CanvasHost.ActualHeight < 1) return;

        var dpi = VisualTreeHelper.GetDpi(this);
        int w = (int)Math.Round(CanvasHost.ActualWidth * dpi.DpiScaleX);
        int h = (int)Math.Round(CanvasHost.ActualHeight * dpi.DpiScaleY);

        if (Math.Abs(w - _lastResizeW) < 4 && Math.Abs(h - _lastResizeH) < 4) return;
        _lastResizeW = w; _lastResizeH = h;
        _pendingW = w; _pendingH = h;

        _resizeTimer ??= new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(180)
        };
        _resizeTimer.Stop();
        _resizeTimer.Tick += (s, _) =>
        {
            ((System.Windows.Threading.DispatcherTimer)s!).Stop();
            _vm.Render.Resize(_pendingW, _pendingH);
        };
        _resizeTimer.Start();
    }

    /// <summary>Обработчик колеса мыши — приближение/отдаление.</summary>
    private void CanvasHost_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        double factor = e.Delta > 0 ? 1.1 : 1 / 1.1;
        _vm.Camera.ZoomBy(factor);
        e.Handled = true;
    }

    /// <summary>Обработчик нажатия ЛКМ — начало вращения/панорамирования.</summary>
    private void CanvasHost_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        _dragging = true;
        _lastMouse = e.GetPosition(this);
        CaptureMouse();
    }

    /// <summary>Обработчик отпускания ЛКМ.</summary>
    private void CanvasHost_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();
    }

    /// <summary>Обработчик движения мыши — вращение камеры или панорамирование.</summary>
    private void CanvasHost_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var pos = e.GetPosition(this);
        double dx = pos.X - _lastMouse.X;
        double dy = pos.Y - _lastMouse.Y;
        _lastMouse = pos;

        var cs = _vm.System.Chaotic;
        if (cs != null && cs.Dimension >= 3)
            _vm.Camera.Rotate(dx, dy);
        else
            _vm.Camera.Pan(dx, dy);
    }

    /// <summary>
    /// Обработчик ПКМ по карте Ляпунова — переход в Attractor с параметрами
    /// точки клика.
    /// </summary>
    private void CanvasHost_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var vm = _vm;
        if (!vm.Render.IsLyapunovMap) return;

        // ⬇ Гасим событие, чтобы мышь не «ушла» дальше вглубь визуального дерева
        e.Handled = true;

        var pos = e.GetPosition(CanvasHost);
        if (CanvasHost.ActualWidth < 1 || CanvasHost.ActualHeight < 1) return;
        double fx = pos.X / CanvasHost.ActualWidth;
        double fy = pos.Y / CanvasHost.ActualHeight;

        var cs = vm.System.Chaotic;
        if (cs == null || cs.Parameters.Length < 2) return;

        int xi = vm.Render.LyapXIndex;
        int yi = vm.Render.LyapYIndex;
        if (xi < 0 || xi >= cs.Parameters.Length) return;
        if (yi < 0 || yi >= cs.Parameters.Length) return;

        var pdX = cs.Parameters[xi];
        var pdY = cs.Parameters[yi];

        double pX = pdX.Min + (pdX.Max - pdX.Min) * fx;
        double pY = pdY.Max - (pdY.Max - pdY.Min) * fy;

        // ⬇ ГЛАВНОЕ: откладываем все мутации на после обработки мыши.
        // DispatcherPriority.Background гарантирует, что действие выполнится,
        // когда мышь уже отпущена и событие полностью завершено.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // Останавливаем дебаунсные таймеры от предыдущих кликов
            vm.Render.CancelPendingDebounce();

            vm.System.Params[xi].Value = pX;
            vm.System.Params[yi].Value = pY;

            // Синхронно переключаем режим — ComboBox обновится через ModeIndex
            vm.Render.Mode = RenderMode.Attractor;
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// Вызывается WPF на каждом кадре (~60 FPS). Если анимация активна,
    /// запрашивает новый рендер. Планировщик коалесцирует запросы — лавина
    /// не возникает.
    /// </summary>
    private void OnFrameRendering(object? sender, EventArgs e)
    {
        if (_vm.Render.IsAnimating)
            _vm.Render.RequestRender();
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        CompositionTarget.Rendering -= OnFrameRendering;
        base.OnClosed(e);
    }
}

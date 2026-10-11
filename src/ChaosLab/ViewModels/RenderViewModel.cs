using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ChaosLab.Core;
using ChaosLab.Rendering;
using ChaosLab.Services;

namespace ChaosLab.ViewModels;

/// <summary>
/// Режим отображения холста. Определяет, какой метод анализа или
/// визуализации применяется к текущей системе.
/// </summary>
public enum RenderMode
{
    /// <summary>Фазовый портрет аттрактора.</summary>
    Attractor,
    /// <summary>Бифуркационная диаграмма по параметру.</summary>
    Bifurcation,
    /// <summary>Тепловая карта старшего показателя Ляпунова.</summary>
    LyapunovMap,
    /// <summary>Cobweb-паутина для 1D-отображений.</summary>
    Cobweb,
    /// <summary>Спектр мощности через FFT.</summary>
    PowerSpectrum,
    /// <summary>Возвратное отображение xₙ → xₙ₊₁.</summary>
    ReturnMap,
    /// <summary>Диагностика вложения: FNN + Mutual Information.</summary>
    FnnMi,
    /// <summary>Мультифрактальный спектр D_q.</summary>
    Multifractal,
    /// <summary>Multiscale entropy: SampEn(τ).</summary>
    MultiscaleEntropyCurve,
    /// <summary>Живой след траектории (анимация).</summary>
    Trail,
}

/// <summary>
/// ViewModel рендера: хранит настройки, управляет планировщиком,
/// генерирует кадры для <see cref="WriteableBitmap"/>.
/// </summary>
/// <remarks>
/// <para>Класс разбит на несколько partial-файлов по функциональным
/// блокам:</para>
/// <list type="bullet">
/// <item><description><c>RenderViewModel.cs</c> — поля, конструктор,
/// публичный API (этот файл).</description></item>
/// <item><description><c>RenderViewModel.Modes.cs</c> — настройки,
/// режимы, оси Ляпунова, интеграторы, Vector Field.</description></item>
/// <item><description><c>RenderViewModel.Hud.cs</c> — статистика HUD,
/// управление анимацией.</description></item>
/// <item><description><c>RenderViewModel.Trail.cs</c> — режим Trail
/// (живой след траектории).</description></item>
/// <item><description><c>RenderViewModel.Frame.cs</c> — <see cref="ComputeFrame"/>
/// и его ветки для каждого режима.</description></item>
/// </list>
/// </remarks>
public sealed partial class RenderViewModel : BaseViewModel
{
    // ================================================================
    // ВНЕШНИЕ ЗАВИСИМОСТИ
    // ================================================================

    /// <summary>ViewModel системы — источник текущей системы и параметров.</summary>
    private readonly SystemViewModel _system;

    /// <summary>ViewModel камеры — параметры проекции 3D → 2D.</summary>
    private readonly CameraViewModel _camera;

    /// <summary>
    /// Асинхронный планировщик с коалесцированием запросов.
    /// Гарантирует, что выполняется только последний запрос.
    /// </summary>
    private readonly RenderScheduler _scheduler = new();

    // ================================================================
    // СОСТОЯНИЕ РЕНДЕРА
    // ================================================================

    /// <summary>Рендерер аттракторов и вспомогательных изображений.</summary>
    private AttractorRenderer _renderer;

    /// <summary>Целевой битмап для WPF-привязки.</summary>
    private WriteableBitmap _bitmap;

    /// <summary>Ширина битмапа в физических пикселях.</summary>
    private int _bmpW = 1024;

    /// <summary>Высота битмапа в физических пикселях.</summary>
    private int _bmpH = 1024;

    /// <summary>
    /// Время последнего кадра в миллисекундах. Заполняется в фоновом
    /// потоке, читается из UI-потока — безопасно, потому что это <c>long</c>.
    /// </summary>
    private long _lastFrameStopwatchMs;

    /// <summary>
    /// Таймер для debounce запросов рендера. Перезапускается при каждом
    /// новом запросе; срабатывает через указанный интервал после последнего.
    /// </summary>
    private DispatcherTimer? _debounceTimer;

    /// <summary>
    /// Переиспользуемый буфер для проекции N-мерного состояния на 3D.
    /// Избегаем аллокации на каждой итерации.
    /// </summary>
    private readonly double[] _xyzBuffer = new double[3];

    // ================================================================
    // КОНСТРУКТОР
    // ================================================================

    /// <summary>
    /// Создаёт ViewModel рендера: инициализирует битмап, подписывается
    /// на события системы и камеры.
    /// </summary>
    /// <param name="system">ViewModel системы (источник данных).</param>
    /// <param name="camera">ViewModel камеры (параметры проекции).</param>
    public RenderViewModel(SystemViewModel system, CameraViewModel camera)
    {
        _system = system;
        _camera = camera;

        _renderer = new AttractorRenderer(_bmpW, _bmpH);
        _bitmap = new WriteableBitmap(_bmpW, _bmpH, 96, 96, PixelFormats.Bgr32, null);

        _scheduler.FrameFactory = ComputeFrame;
        _scheduler.FrameReady += OnFrameReady;
        _scheduler.Failed += OnFrameFailed;

        // Подписки на изменения системы и камеры
        _system.SystemChanged += OnSystemChangedWithReset;
        _system.ParametersChanged += () => RequestRenderDeferred();
        _camera.PropertyChanged += (_, _) => RequestRenderDeferred();

        Palette = Palette.ByName(_paletteName);

        // Команда анимации
        ToggleAnimateCommand = new RelayCommand(ToggleAnimate);
    }

    // ================================================================
    // ПУБЛИЧНЫЙ API
    // ================================================================

    /// <summary>Мгновенный запрос рендера. Используется для кнопок и смены режима.</summary>
    public void RequestRender() => _scheduler.Request();

    /// <summary>
    /// Запрос рендера с задержкой. Используется для слайдеров и мыши,
    /// чтобы не запускать рендер на каждый пиксель движения.
    /// </summary>
    /// <param name="ms">Задержка в миллисекундах (по умолчанию 40).</param>
    public void RequestRenderDeferred(int ms = 40)
    {
        if (_debounceTimer == null)
        {
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ms)
            };
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer!.Stop();
                _scheduler.Request();
            };
        }
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    /// <summary>
    /// Отменяет отложенный запрос. Используется перед мгновенными
    /// действиями (смена системы, режима), чтобы не допустить
    /// устаревшего рендера.
    /// </summary>
    public void CancelPendingDebounce() => _debounceTimer?.Stop();

    /// <summary>
    /// Пересоздаёт битмап и рендерер под новый размер (в физических пикселях).
    /// Игнорирует изменения меньше 8 px — защита от мелкой дрожи layout-а.
    /// </summary>
    /// <param name="w">Ширина холста.</param>
    /// <param name="h">Высота холста.</param>
    public void Resize(int w, int h)
    {
        if (w < 256) w = 256;
        if (h < 256) h = 256;
        if (w > 2400) w = 2400;
        if (h > 2400) h = 2400;

        if (Math.Abs(w - _bmpW) < 8 && Math.Abs(h - _bmpH) < 8) return;

        _bmpW = w;
        _bmpH = h;
        _renderer = new AttractorRenderer(w, h);
        _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgr32, null);

        OnPropertyChanged(nameof(BmpW));
        OnPropertyChanged(nameof(BmpH));

        var app = Application.Current;
        if (app != null && app.Dispatcher.CheckAccess())
            Bitmap = _bitmap;
        else
            app?.Dispatcher.BeginInvoke(new Action(() => Bitmap = _bitmap));

        RequestRender();
    }

    /// <summary>Обнуляет холст (заливает фоном).</summary>
    public void Clear()
    {
        _renderer.Clear();
        var app = Application.Current;
        if (app == null) return;
        app.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                _bitmap.WritePixels(new Int32Rect(0, 0, _bmpW, _bmpH),
                    _renderer.Pixels, _bmpW * 4, 0);
                Bitmap = _bitmap;
            }
            catch { }
        }), DispatcherPriority.Background);
        TotalPoints = 0;
    }

    /// <summary>Сбрасывает камеру на значения по умолчанию и перерисовывает.</summary>
    public void ResetCameraAndRender()
    {
        if (_system.Chaotic is ChaoticSystem cs)
            _camera.ApplyDefault(cs);
        RequestRender();
    }

    /// <summary>Сохраняет текущий кадр в PNG-файл.</summary>
    /// <param name="path">Путь к файлу.</param>
    public void SavePng(string path)
    {
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(_bitmap));
        using var fs = File.Create(path);
        enc.Save(fs);
    }

    /// <summary>
    /// Отображает произвольный массив пикселей (для RQA, Пуанкаре, бассейнов).
    /// Вызывается из UI-потока через <c>Dispatcher.BeginInvoke</c>.
    /// </summary>
    /// <param name="pixels">Массив Bgr32 длиной <c>BmpW × BmpH</c>.</param>
    public void DisplayPixels(int[] pixels)
    {
        var app = Application.Current;
        if (app == null) return;
        app.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                _bitmap.WritePixels(new Int32Rect(0, 0, _bmpW, _bmpH),
                    pixels, _bmpW * 4, 0);
                Bitmap = _bitmap;
            }
            catch { }
        }), DispatcherPriority.Background);
    }

    // ================================================================
    // ПУБЛИЧНЫЕ READONLY-СВОЙСТВА
    // ================================================================

    /// <summary>Ширина текущего битмапа.</summary>
    public int BmpW => _bmpW;

    /// <summary>Высота текущего битмапа.</summary>
    public int BmpH => _bmpH;

    /// <summary>Текущий рендерер (для анализа пикселей и плотности).</summary>
    public AttractorRenderer Renderer => _renderer;

    /// <summary>Буфер плотности для box-counting и подобных методов.</summary>
    public ushort[] Density => _renderer.Density;

    /// <summary>Прямая ссылка на Bgr32-буфер текущего кадра.</summary>
    public int[] Pixels => _renderer.Pixels;

    private ImageSource? _bitmapProp;

    /// <summary>
    /// Изображение для привязки в XAML (<c>Image.Source</c>). Это ссылка
    /// на тот же объект, что и <c>_bitmap</c>, но типизированная как
    /// базовый интерфейс — чтобы WPF не пытался писать в него из другого
    /// потока.
    /// </summary>
    public ImageSource? Bitmap
    {
        get => _bitmapProp;
        private set => Set(ref _bitmapProp, value);
    }

    // ================================================================
    // ПРИВАТНЫЕ ОБРАБОТЧИКИ ПЛАНИРОВЩИКА
    // ================================================================

    /// <summary>
    /// Вызывается планировщиком в фоновом потоке, когда кадр готов.
    /// Копирует пиксели в <see cref="WriteableBitmap"/> через
    /// <c>Dispatcher.BeginInvoke</c> с приоритетом Background.
    /// </summary>
    private void OnFrameReady(int[] pixels, long pts)
    {
        var app = Application.Current;
        if (app == null) return;

        app.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                int w = _bmpW, h = _bmpH;
                if (pixels.Length != w * h) return;   // устаревший размер

                _bitmap.WritePixels(new Int32Rect(0, 0, w, h), pixels, w * 4, 0);
                Bitmap = _bitmap;

                UpdateHud(pts);   // см. RenderViewModel.Hud.cs
                LastError = null;
            }
            catch { }
        }), DispatcherPriority.Background);
    }

    /// <summary>
    /// Вызывается планировщиком при ошибке рендера. Показывает сообщение
    /// в HUD.
    /// </summary>
    private void OnFrameFailed(string message)
    {
        var app = Application.Current;
        if (app == null) return;
        app.Dispatcher.BeginInvoke(
            new Action(() => LastError = message),
            DispatcherPriority.Normal);
    }

    /// <summary>
    /// Обёртка для события смены системы: сначала сбрасывает состояние
    /// Trail, потом вызывает общую обработку.
    /// </summary>
    private void OnSystemChangedWithReset()
    {
        ResetTrailState();
        OnSystemChanged();
    }
}

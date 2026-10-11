namespace ChaosLab.ViewModels;

public sealed partial class RenderViewModel
{
    // ================================================================
    // HUD: СТАТИСТИКА
    // ================================================================

    /// <summary>
    /// Минимальный интервал между обновлениями HUD в миллисекундах.
    /// Ограничивает частоту обновления UI-текста до ~5 раз в секунду,
    /// чтобы не спамить WPF-биндинги.
    /// </summary>
    private const int HudIntervalMs = 200;

    /// <summary>Время последнего обновления HUD.</summary>
    private DateTime _lastHudUpdate = DateTime.MinValue;

    private long _totalPoints;

    /// <summary>
    /// Число точек, отрисованных в текущем кадре. Показывается в HUD.
    /// </summary>
    public long TotalPoints
    {
        get => _totalPoints;
        private set => Set(ref _totalPoints, value);
    }

    private long _lastFrameMs;

    /// <summary>Время последнего кадра в миллисекундах (для HUD).</summary>
    public long LastFrameMs
    {
        get => _lastFrameMs;
        private set => Set(ref _lastFrameMs, value);
    }

    private string? _lastError;

    /// <summary>
    /// Текст последней ошибки рендера. <c>null</c>, если ошибок нет.
    /// </summary>
    public string? LastError
    {
        get => _lastError;
        private set
        {
            if (Set(ref _lastError, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary><c>true</c>, если есть непустая ошибка.</summary>
    public bool HasError => !string.IsNullOrEmpty(_lastError);

    /// <summary>
    /// Обновляет значения HUD с троттлингом. Не чаще, чем раз в
    /// <see cref="HudIntervalMs"/> миллисекунд.
    /// </summary>
    /// <param name="pts">Число отрисованных точек.</param>
    private void UpdateHud(long pts)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastHudUpdate).TotalMilliseconds < HudIntervalMs) return;
        _lastHudUpdate = now;

        TotalPoints = pts;
        LastFrameMs = _lastFrameStopwatchMs;
    }

    // ================================================================
    // АНИМАЦИЯ
    // ================================================================

    private bool _isAnimating;

    /// <summary>
    /// Идёт ли сейчас непрерывная анимация. Пока <c>true</c>,
    /// <c>CompositionTarget.Rendering</c> вызывает <see cref="RequestRender"/>
    /// на каждом кадре (~60 FPS).
    /// </summary>
    public bool IsAnimating
    {
        get => _isAnimating;
        private set
        {
            if (Set(ref _isAnimating, value))
                OnPropertyChanged(nameof(AnimateButtonText));
        }
    }

    /// <summary>Текст кнопки анимации: «● Animate» или «■ Stop».</summary>
    public string AnimateButtonText => IsAnimating ? "■ Stop" : "● Animate";

    /// <summary>Команда запуска/остановки анимации.</summary>
    public RelayCommand ToggleAnimateCommand { get; }

    /// <summary>
    /// Переключает состояние анимации. При запуске в режиме Trail —
    /// очищает след и сбрасывает буферы, чтобы начать заново.
    /// </summary>
    private void ToggleAnimate()
    {
        if (IsAnimating)
        {
            IsAnimating = false;
            return;
        }

        // При старте анимации в режиме Trail — начинаем с чистого листа
        if (Mode == RenderMode.Trail) ResetTrailState();

        IsAnimating = true;
        RequestRender();
    }
}

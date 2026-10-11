using ChaosLab.Core;
using ChaosLab.Systems;

namespace ChaosLab.ViewModels;

public sealed partial class RenderViewModel
{
    // ================================================================
    // РЕНДЕР: STEPS / DT / GAMMA
    // ================================================================

    private double _steps = 2_000_000;

    /// <summary>
    /// Число итераций для отрисовки аттрактора. Хранится в лог-шкале
    /// (значение = 10^slider). Диапазон в UI: 1K–100M.
    /// </summary>
    public double Steps
    {
        get => _steps;
        set
        {
            if (!Set(ref _steps, value)) return;
            OnPropertyChangedThrottled(nameof(StepsLabel));
            RequestRenderDeferred();
        }
    }

    /// <summary>Число итераций в виде текста: «2.0M», «500K» и т.п.</summary>
    public string StepsLabel => Steps >= 1_000_000
        ? $"{Steps / 1_000_000.0:F1}M"
        : Steps >= 1_000
            ? $"{Steps / 1000.0:F0}K"
            : ((int) Steps).ToString();

    private double _dt = 0.005;

    /// <summary>Шаг интегрирования ОДУ (для потоков).</summary>
    public double Dt
    {
        get => _dt;
        set
        {
            if (!Set(ref _dt, value)) return;
            OnPropertyChangedThrottled(nameof(DtLabel));
            RequestRenderDeferred();
        }
    }

    /// <summary>Шаг интегрирования в виде текста.</summary>
    public string DtLabel => Dt.ToString("G3");

    private double _gamma = 0.85;

    /// <summary>Гамма-коррекция палитры (нелинейное растяжение яркости).</summary>
    public double Gamma
    {
        get => _gamma;
        set
        {
            if (!Set(ref _gamma, value)) return;
            OnPropertyChangedThrottled(nameof(GammaLabel));
            RequestRenderDeferred();
        }
    }

    /// <summary>Гамма в виде текста.</summary>
    public string GammaLabel => Gamma.ToString("F2");

    // ================================================================
    // ПАЛИТРА
    // ================================================================

    private string _paletteName = "Inferno";

    /// <summary>Текущая палитра для раскраски.</summary>
    public Palette Palette { get; private set; } = Palette.All[0];

    /// <summary>Имена всех доступных палитр.</summary>
    public string[] AllPalettes { get; } = Array.ConvertAll(Palette.All, p => p.Name);

    /// <summary>Имя выбранной палитры.</summary>
    public string PaletteName
    {
        get => _paletteName;
        set
        {
            if (!Set(ref _paletteName, value)) return;
            Palette = Palette.ByName(value);
            RequestRenderDeferred();
        }
    }

    // ================================================================
    // РЕЖИМ ОТОБРАЖЕНИЯ
    // ================================================================

    private RenderMode _mode = RenderMode.Attractor;

    /// <summary>Текущий режим отображения.</summary>
    public RenderMode Mode
    {
        get => _mode;
        set
        {
            if (!Set(ref _mode, value)) return;

            // При смене режима — сбрасываем Trail и его буферы
            ResetTrailState();

            OnPropertyChanged(nameof(ModeIndex));
            OnPropertyChanged(nameof(IsBifurcation));
            OnPropertyChanged(nameof(IsLyapunovMap));
            OnPropertyChanged(nameof(IsTrail));
            OnPropertyChanged(nameof(ModeLabel));
            OnPropertyChanged(nameof(AllParamNames));
            OnPropertyChanged(nameof(LyapXName));
            OnPropertyChanged(nameof(LyapYName));
            OnPropertyChanged(nameof(LyapXRange));
            OnPropertyChanged(nameof(LyapYRange));
            OnPropertyChanged(nameof(CanShowVectorField));
            OnPropertyChanged(nameof(CanUseVerlet));

            if (value == RenderMode.LyapunovMap) ApplyDefaultLyapunovAxes();
            if (!CanShowVectorField) ShowVectorField = false;
            if (!CanUseVerlet) UseVerlet = false;

            CancelPendingDebounce();
            RequestRender();
        }
    }

    /// <summary>Все режимы отображения для ComboBox.</summary>
    public string[] AllModes { get; } =
    {
        "Attractor", "Bifurcation", "Lyapunov Map",
        "Cobweb", "Power Spectrum", "Return Map",
        "FNN + MI", "Multifractal", "Multiscale Entropy",
        "Trail",
    };

    /// <summary>Индекс текущего режима (для привязки ComboBox).</summary>
    public int ModeIndex
    {
        get => (int) _mode;
        set
        {
            if (value < 0 || value > 9 || value == (int) _mode) return;
            Mode = (RenderMode) value;
        }
    }

    /// <summary>Человекочитаемое имя текущего режима.</summary>
    public string ModeLabel => Mode switch
    {
        RenderMode.Attractor => "Attractor",
        RenderMode.Bifurcation => "Bifurcation",
        RenderMode.LyapunovMap => "Lyapunov Map",
        RenderMode.Cobweb => "Cobweb",
        RenderMode.PowerSpectrum => "Power Spectrum",
        RenderMode.ReturnMap => "Return Map",
        RenderMode.FnnMi => "FNN + MI",
        RenderMode.Multifractal => "Multifractal",
        RenderMode.MultiscaleEntropyCurve => "Multiscale Entropy",
        RenderMode.Trail => "Trail",
        _ => Mode.ToString(),
    };

    /// <summary><c>true</c>, если активен режим Bifurcation.</summary>
    public bool IsBifurcation => Mode == RenderMode.Bifurcation;

    /// <summary><c>true</c>, если активен режим Lyapunov Map.</summary>
    public bool IsLyapunovMap => Mode == RenderMode.LyapunovMap;

    /// <summary><c>true</c>, если активен режим Trail.</summary>
    public bool IsTrail => Mode == RenderMode.Trail;

    // ================================================================
    // ПАРАМЕТР БИФУРКАЦИЙ
    // ================================================================

    private int _bifParamIndex;

    /// <summary>Индекс параметра для бифуркационной диаграммы.</summary>
    public int BifParamIndex
    {
        get => _bifParamIndex;
        set
        {
            if (!Set(ref _bifParamIndex, value)) return;
            RequestRenderDeferred();
        }
    }

    /// <summary>Имена всех параметров системы (для ComboBox бифуркаций).</summary>
    public string[] BifParamNames
    {
        get
        {
            var pars = _system.Current.Parameters;
            var arr = new string[pars.Length];
            for (int i = 0; i < pars.Length; i++) arr[i] = pars[i].Name;
            return arr;
        }
    }

    // ================================================================
    // ОСИ КАРТЫ ЛЯПУНОВА
    // ================================================================

    private int _lyapXIndex;
    private int _lyapYIndex = 1;

    /// <summary>Индекс параметра по оси X карты Ляпунова.</summary>
    public int LyapXIndex
    {
        get => _lyapXIndex;
        set
        {
            if (!Set(ref _lyapXIndex, value)) return;
            OnPropertyChanged(nameof(LyapXName));
            OnPropertyChanged(nameof(LyapXRange));
            RequestRenderDeferred();
        }
    }

    /// <summary>Индекс параметра по оси Y карты Ляпунова.</summary>
    public int LyapYIndex
    {
        get => _lyapYIndex;
        set
        {
            if (!Set(ref _lyapYIndex, value)) return;
            OnPropertyChanged(nameof(LyapYName));
            OnPropertyChanged(nameof(LyapYRange));
            RequestRenderDeferred();
        }
    }

    /// <summary>Имя параметра по оси X.</summary>
    public string LyapXName => _lyapXIndex < _system.Current.Parameters.Length
        ? _system.Current.Parameters[_lyapXIndex].Name : "—";

    /// <summary>Имя параметра по оси Y.</summary>
    public string LyapYName => _lyapYIndex < _system.Current.Parameters.Length
        ? _system.Current.Parameters[_lyapYIndex].Name : "—";

    /// <summary>Диапазон оси X в виде «1, 60».</summary>
    public string LyapXRange
    {
        get
        {
            var pars = _system.Current.Parameters;
            if (_lyapXIndex < 0 || _lyapXIndex >= pars.Length) return "—";
            return $"{pars[_lyapXIndex].Min:G3}, {pars[_lyapXIndex].Max:G3}";
        }
    }

    /// <summary>Диапазон оси Y.</summary>
    public string LyapYRange
    {
        get
        {
            var pars = _system.Current.Parameters;
            if (_lyapYIndex < 0 || _lyapYIndex >= pars.Length) return "—";
            return $"{pars[_lyapYIndex].Min:G3}, {pars[_lyapYIndex].Max:G3}";
        }
    }

    /// <summary>Имена всех параметров (для ComboBox осей).</summary>
    public string[] AllParamNames
    {
        get
        {
            var pars = _system.Current.Parameters;
            var arr = new string[pars.Length];
            for (int i = 0; i < pars.Length; i++) arr[i] = pars[i].Name;
            return arr;
        }
    }

    /// <summary>
    /// Подбирает разумные оси для карты Ляпунова по имени системы.
    /// </summary>
    private void ApplyDefaultLyapunovAxes()
    {
        var pars = _system.Current.Parameters;
        if (pars.Length < 2) return;

        var (dx, dy) = DefaultLyapunovAxes.For(_system.Current.Name);
        int maxIdx = pars.Length - 1;

        _lyapXIndex = Math.Clamp(dx, 0, maxIdx);
        _lyapYIndex = Math.Clamp(dy, 0, maxIdx);
        if (_lyapXIndex == _lyapYIndex)
            _lyapYIndex = (_lyapYIndex + 1) % (maxIdx + 1);

        OnPropertyChanged(nameof(LyapXIndex));
        OnPropertyChanged(nameof(LyapYIndex));
        OnPropertyChanged(nameof(LyapXName));
        OnPropertyChanged(nameof(LyapYName));
        OnPropertyChanged(nameof(LyapXRange));
        OnPropertyChanged(nameof(LyapYRange));
    }

    // ================================================================
    // ИНТЕГРАТОРЫ
    // ================================================================

    private bool _useVerlet;

    /// <summary>
    /// Использовать симплектический интегратор Verlet. Доступен только
    /// для систем, реализующих <see cref="ISymplecticSystem"/>.
    /// </summary>
    public bool UseVerlet
    {
        get => _useVerlet;
        set { if (Set(ref _useVerlet, value)) RequestRenderDeferred(); }
    }

    /// <summary>Доступен ли Verlet для текущей системы.</summary>
    public bool CanUseVerlet => _system.Chaotic is ISymplecticSystem;

    private bool _useAdaptiveRk45;

    /// <summary>
    /// Использовать адаптивный интегратор Dormand-Prince RK45.
    /// Работает только для потоков (не для отображений).
    /// </summary>
    public bool UseAdaptiveRk45
    {
        get => _useAdaptiveRk45;
        set { if (Set(ref _useAdaptiveRk45, value)) RequestRenderDeferred(); }
    }

    private double _rk45Tolerance = 1e-4;

    /// <summary>
    /// Допуск относительной ошибки для адаптивного RK45. Чем меньше —
    /// тем точнее, но медленнее.
    /// </summary>
    public double Rk45Tolerance
    {
        get => _rk45Tolerance;
        set
        {
            if (!Set(ref _rk45Tolerance, value)) return;
            OnPropertyChanged(nameof(Rk45ToleranceLabel));
            OnPropertyChanged(nameof(Rk45ToleranceLog));
            RequestRenderDeferred();
        }
    }

    /// <summary>Допуск в формате «1.0E-004».</summary>
    public string Rk45ToleranceLabel => Rk45Tolerance.ToString("E1");

    /// <summary>log10(допуск) для слайдера.</summary>
    public double Rk45ToleranceLog
    {
        get => Math.Log10(Rk45Tolerance);
        set => Rk45Tolerance = Math.Pow(10, value);
    }

    // ================================================================
    // VECTOR FIELD OVERLAY
    // ================================================================

    private bool _showVectorField;

    /// <summary>
    /// Показывать ли слой векторного поля поверх аттрактора.
    /// Доступно только для 2D дискретных отображений.
    /// </summary>
    public bool ShowVectorField
    {
        get => _showVectorField;
        set { if (Set(ref _showVectorField, value)) RequestRenderDeferred(); }
    }

    /// <summary>Доступен ли overlay для текущей системы.</summary>
    public bool CanShowVectorField =>
        _system.Chaotic is { IsDiscrete: true, Dimension: 2 };

    // ================================================================
    // УРАВНЕНИЯ
    // ================================================================

    /// <summary>Текст уравнений текущей системы для отображения в UI.</summary>
    public string EquationsText => EquationsCatalog.Get(_system.Current.Name);

    // ================================================================
    // СМЕНА СИСТЕМЫ
    // ================================================================

    /// <summary>
    /// Обработчик смены системы: сбрасывает несовместимые флаги,
    /// уведомляет UI об изменении зависимых свойств.
    /// </summary>
    private void OnSystemChanged()
    {
        _bifParamIndex = 0;

        OnPropertyChanged(nameof(BifParamNames));
        OnPropertyChanged(nameof(IsBifurcation));
        OnPropertyChanged(nameof(EquationsText));
        OnPropertyChanged(nameof(AllParamNames));
        OnPropertyChanged(nameof(LyapXName));
        OnPropertyChanged(nameof(LyapYName));
        OnPropertyChanged(nameof(LyapXRange));
        OnPropertyChanged(nameof(LyapYRange));
        OnPropertyChanged(nameof(CanShowVectorField));
        OnPropertyChanged(nameof(CanUseVerlet));

        if (!CanShowVectorField) ShowVectorField = false;
        if (!CanUseVerlet) UseVerlet = false;

        if (Mode == RenderMode.LyapunovMap) ApplyDefaultLyapunovAxes();

        RequestRender();
    }
}

using System.Text;
using ChaosLab.Analysis;
using ChaosLab.Core;

namespace ChaosLab.ViewModels;

/// <summary>
/// ViewModel панели анализа: содержит все команды и текстовые результаты.
/// </summary>
/// <remarks>
/// Каждая команда запускает <see cref="Services.AnalysisService"/>,
/// отменяя предыдущую задачу. Результаты — простые строки свойств,
/// привязанные к TextBlock-ам в XAML.
/// </remarks>
public sealed class AnalysisViewModel : BaseViewModel
{
    private readonly SystemViewModel _system;
    private readonly RenderViewModel _render;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Создаёт ViewModel анализа, связывая его с системой и рендером.
    /// </summary>
    /// <param name="system">ViewModel системы (источник текущей системы и параметров).</param>
    /// <param name="render">ViewModel рендера (источник dt, палитры, холста).</param>
    public AnalysisViewModel(SystemViewModel system, RenderViewModel render)
    {
        _system = system;
        _render = render;
        RunAllCommand = new RelayCommand(async () => await RunAllAsync());
        CancelCommand = new RelayCommand(Cancel);
        LyapunovCommand = new RelayCommand(async () => await RunLyapunovAsync());
        CorrelationCommand = new RelayCommand(async () => await RunCorrelationAsync());
        EntropiesCommand = new RelayCommand(async () => await RunEntropiesAsync());
        HurstCommand = new RelayCommand(async () => await RunHurstAsync());
        DfaCommand = new RelayCommand(async () => await RunDfaAsync());
        ZeroOneCommand = new RelayCommand(async () => await RunZeroOneAsync());
        RqaCommand = new RelayCommand(async () => await RunRqaAsync());
        PoincareCommand = new RelayCommand(async () => await RunPoincareAsync());
        BasinsCommand = new RelayCommand(async () => await RunBasinsAsync());
        SymbolicCommand = new RelayCommand(async () => await RunSymbolicAsync());
        ResonanceCommand = new RelayCommand(async () => await RunResonanceAsync());
        MultiscaleCommand = new RelayCommand(async () => await RunMultiscaleAsync());
        ApEnCommand = new RelayCommand(async () => await RunApEnAsync());
        LzComplexityCommand = new RelayCommand(async () => await RunLzAsync());
        HiguchiFdCommand = new RelayCommand(async () => await RunHiguchiAsync());
        KsEntropyCommand = new RelayCommand(async () => await RunKsEntropyAsync());
        BasinEntropyCommand = new RelayCommand(async () => await RunBasinEntropyAsync());
        PredictabilityCommand = new RelayCommand(async () => await RunPredictabilityAsync());
        _system.SystemChanged += ResetAllResults;
        _system.ParametersChanged += ResetAllResults;
    }

    // -------- текстовые результаты --------
    private string _lyapunovSpectrumText = "—";
    /// <summary>Спектр Ляпунова в виде многострочного текста.</summary>
    public string LyapunovSpectrumText { get => _lyapunovSpectrumText; private set => Set(ref _lyapunovSpectrumText, value); }

    private string _lyapunovStatus = "Нажмите «λ Спектр»";
    /// <summary>Статус расчёта спектра (✓ Хаос / ✗ Регулярность).</summary>
    public string LyapunovStatus { get => _lyapunovStatus; private set => Set(ref _lyapunovStatus, value); }

    private string _kaplanYorkeText = "—";
    /// <summary>Размерность Каплана–Йорка.</summary>
    public string KaplanYorkeText { get => _kaplanYorkeText; private set => Set(ref _kaplanYorkeText, value); }

    private string _correlationText = "—";
    /// <summary>Корреляционная размерность D₂.</summary>
    public string CorrelationText { get => _correlationText; private set => Set(ref _correlationText, value); }

    private string _boxCountingText = "—";
    /// <summary>Box-counting размерность.</summary>
    public string BoxCountingText { get => _boxCountingText; private set => Set(ref _boxCountingText, value); }

    private string _sampleEntropyText = "—";
    /// <summary>Sample Entropy.</summary>
    public string SampleEntropyText { get => _sampleEntropyText; private set => Set(ref _sampleEntropyText, value); }

    private string _permutationEntropyText = "—";
    /// <summary>Permutation Entropy.</summary>
    public string PermutationEntropyText { get => _permutationEntropyText; private set => Set(ref _permutationEntropyText, value); }

    private string _hurstText = "—";
    /// <summary>Экспонента Хёрста.</summary>
    public string HurstText { get => _hurstText; private set => Set(ref _hurstText, value); }

    private string _dfaText = "—";
    /// <summary>DFA-экспонента.</summary>
    public string DfaText { get => _dfaText; private set => Set(ref _dfaText, value); }

    private string _zeroOneText = "—";
    /// <summary>Параметр 0–1 теста.</summary>
    public string ZeroOneText { get => _zeroOneText; private set => Set(ref _zeroOneText, value); }

    private string _rqaText = "—";
    /// <summary>RQA-метрики в виде текста.</summary>
    public string RqaText { get => _rqaText; private set => Set(ref _rqaText, value); }

    private string _poincareText = "—";
    /// <summary>Статус построения сечения Пуанкаре.</summary>
    public string PoincareText { get => _poincareText; private set => Set(ref _poincareText, value); }

    private string _basinsText = "—";
    /// <summary>Статус построения бассейнов.</summary>
    public string BasinsText { get => _basinsText; private set => Set(ref _basinsText, value); }

    private string _progressText = "";
    /// <summary>Прогресс (пусто, если нет активной задачи).</summary>
    public string ProgressText { get => _progressText; private set => Set(ref _progressText, value); }

    /// <summary>Результаты символьной динамики.</summary>
    public string SymbolicText { get => _symbolicText; private set => Set(ref _symbolicText, value); }
    private string _symbolicText = "—";

    /// <summary>Результаты резонансного анализа.</summary>
    public string ResonanceText { get => _resonanceText; private set => Set(ref _resonanceText, value); }
    private string _resonanceText = "—";

    /// <summary>Результаты multiscale entropy.</summary>
    public string MultiscaleText { get => _multiscaleText; private set => Set(ref _multiscaleText, value); }
    private string _multiscaleText = "—";

    private string _apEnText = "—";
    /// <summary>
    /// Результат Approximate Entropy: ApEn(m, r) с параметрами по умолчанию.
    /// </summary>
    /// <remarks>
    /// Обновляется командой <see cref="ApEnCommand"/>. Формат: "ApEn(2, 0.2σ) = X.XXXX".
    /// До запуска содержит прочерк.
    /// </remarks>
    public string ApEnText { get => _apEnText; private set => Set(ref _apEnText, value); }

    private string _lzText = "—";
    /// <summary>
    /// Результат Lempel–Ziv complexity: нормализованное значение c(n) ∈ [0, 1].
    /// </summary>
    /// <remarks>
    /// Обновляется командой <see cref="LzComplexityCommand"/>. Регулярная динамика —
    /// около 0.2, хаос — около 0.7, белый шум — около 1.0.
    /// </remarks>
    public string LzText { get => _lzText; private set => Set(ref _lzText, value); }

    private string _higuchiText = "—";
    /// <summary>
    /// Результат Higuchi fractal dimension D_H временного ряда.
    /// </summary>
    /// <remarks>
    /// Обновляется командой <see cref="HiguchiFdCommand"/>. Периодика — D_H ≈ 1,
    /// белый шум — D_H ≈ 2, хаос — D_H ≈ 1.5–1.8.
    /// </remarks>
    public string HiguchiText { get => _higuchiText; private set => Set(ref _higuchiText, value); }

    private string _ksEntropyText = "—";
    /// <summary>
    /// Результат энтропии Колмогорова–Синая h_KS = Σ max(λᵢ, 0).
    /// </summary>
    /// <remarks>
    /// Обновляется командой <see cref="KsEntropyCommand"/>. Многострочный результат:
    /// значение h_KS, число положительных показателей Ляпунова, метка гиперхаоса.
    /// </remarks>
    public string KsEntropyText { get => _ksEntropyText; private set => Set(ref _ksEntropyText, value); }

    private string _basinEntropyText = "—";
    /// <summary>
    /// Результат Basin entropy: Shannon-энтропия распределения меток бассейнов.
    /// </summary>
    /// <remarks>
    /// Обновляется командой <see cref="BasinEntropyCommand"/>. Формат:
    /// число аттракторов, абсолютная энтропия в битах, нормализованное значение.
    /// </remarks>
    public string BasinEntropyText { get => _basinEntropyText; private set => Set(ref _basinEntropyText, value); }

    private string _predictabilityText = "—";
    /// <summary>
    /// Результат оценки горизонта предсказуемости t_h = (1/λ₁)·log(Δ/ε₀).
    /// </summary>
    /// <remarks>
    /// Обновляется командой <see cref="PredictabilityCommand"/>. Для Лоренца
    /// при ε₀ = 10⁻³ даёт t_h ≈ 12 единиц времени — историческое значение из
    /// работы Лоренца (1965) о пределе предсказуемости атмосферы.
    /// </remarks>
    public string PredictabilityText { get => _predictabilityText; private set => Set(ref _predictabilityText, value); }

    private bool _busy;
    /// <summary>Идёт ли в данный момент анализ.</summary>
    public bool Busy { get => _busy; private set => Set(ref _busy, value); }

    /// <summary>Команда: запустить всё.</summary>
    public RelayCommand RunAllCommand { get; }
    /// <summary>Команда: отменить текущий анализ.</summary>
    public RelayCommand CancelCommand { get; }
    /// <summary>Команда: спектр Ляпунова.</summary>
    public RelayCommand LyapunovCommand { get; }
    /// <summary>Команда: корреляционная размерность.</summary>
    public RelayCommand CorrelationCommand { get; }
    /// <summary>Команда: энтропии.</summary>
    public RelayCommand EntropiesCommand { get; }
    /// <summary>Команда: Hurst.</summary>
    public RelayCommand HurstCommand { get; }
    /// <summary>Команда: DFA.</summary>
    public RelayCommand DfaCommand { get; }
    /// <summary>Команда: 0–1 тест.</summary>
    public RelayCommand ZeroOneCommand { get; }
    /// <summary>Команда: RQA.</summary>
    public RelayCommand RqaCommand { get; }
    /// <summary>Команда: сечение Пуанкаре.</summary>
    public RelayCommand PoincareCommand { get; }
    /// <summary>Команда: бассейны притяжения.</summary>
    public RelayCommand BasinsCommand { get; }
    /// <summary>Команда: символьно-динамический анализ.</summary>
    public RelayCommand SymbolicCommand { get; }
    /// <summary>Команда: резонансный анализ.</summary>
    public RelayCommand ResonanceCommand { get; }
    /// <summary>Команда: multiscale entropy.</summary>
    public RelayCommand MultiscaleCommand { get; }
    /// <summary>
    /// Команда запуска Approximate Entropy.
    /// </summary>
    /// <remarks>
    /// Собирает <see cref="EntropyMeasures.CollectSeriesAsync"/> из 4000 точек,
    /// вычисляет <see cref="ApproximateEntropy.Compute"/> с m = 2, r = 0.2σ.
    /// Результат — в <see cref="ApEnText"/>.
    /// </remarks>
    public RelayCommand ApEnCommand { get; }

    /// <summary>
    /// Команда запуска Lempel–Ziv complexity.
    /// </summary>
    /// <remarks>
    /// Бинаризует ряд по медиане и вычисляет <see cref="LempelZivComplexity.Compute"/>.
    /// Результат — в <see cref="LzText"/>.
    /// </remarks>
    public RelayCommand LzComplexityCommand { get; }

    /// <summary>
    /// Команда запуска Higuchi fractal dimension.
    /// </summary>
    /// <remarks>
    /// Работает по временному ряду из 5000 точек, k_max = 20.
    /// Результат — в <see cref="HiguchiText"/>.
    /// </remarks>
    public RelayCommand HiguchiFdCommand { get; }

    /// <summary>
    /// Команда запуска энтропии Колмогорова–Синая.
    /// </summary>
    /// <remarks>
    /// Использует полный спектр Ляпунова (100k шагов для потоков, 200k для
    /// отображений), затем суммирует положительные показатели.
    /// Результат — в <see cref="KsEntropyText"/>.
    /// </remarks>
    public RelayCommand KsEntropyCommand { get; }

    /// <summary>
    /// Команда запуска Basin entropy.
    /// </summary>
    /// <remarks>
    /// Строит карту бассейнов 150×150 в окне [−10, 10]² и вычисляет
    /// Shannon-энтропию распределения меток аттракторов.
    /// Результат — в <see cref="BasinEntropyText"/>. Только для потоков ≥ 2D.
    /// </remarks>
    public RelayCommand BasinEntropyCommand { get; }

    /// <summary>
    /// Команда запуска оценки горизонта предсказуемости.
    /// </summary>
    /// <remarks>
    /// Вычисляет спектр Ляпунова, автоматически оценивает размер аттрактора
    /// через <see cref="AnalysisViewModel.EstimateAttractorSizeNumeric"/>, затем
    /// применяет <see cref="PredictabilityHorizon.Estimate"/>.
    /// Результат — в <see cref="PredictabilityText"/>.
    /// </remarks>
    public RelayCommand PredictabilityCommand { get; }

    private void Cancel()
    {
        _cts?.Cancel();
        ProgressText = "Отменено.";
    }

    private ChaoticSystem? Cs => _system.Chaotic;
    private double[] Params() => _system.CurrentParamValues();
    private double DtFor(ChaoticSystem cs) => cs.IsDiscrete ? 1.0 : _render.Dt;

    private int _runAllStep;
    private int _runAllTotal;
    private const int RunAllTotalSteps = 16;

    private void NewCts()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
    }

    private async Task RunLyapunovAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "Ляпунов…";
        try
        {
            double dt = DtFor(cs);
            int steps = cs.IsDiscrete ? 200_000 : 100_000;

            var spec = await LyapunovSpectrum.ComputeAsync(
                cs, cs.DefaultInitialState, Params(), dt, steps, 5000, _cts!.Token);

            //var sb = new System.Text.StringBuilder();
            //for (int i = 0; i < spec.Length && i < 8; i++)
            //{
            //    sb.Append($"λ[{i}] = {spec[i],10:F4}");
            //    if (i < spec.Length - 1 && i < 7) sb.Append('\n');
            //}
            //LyapunovSpectrumText = sb.ToString();

            //int positive = spec.Count(v => v > 0.001);

            const int MaxLines = 8;
            const int ValueWidth = 10;
            const double PositiveThreshold = 0.001;

            int lineCount = Math.Min(spec.Length, MaxLines);
            var sb = new StringBuilder(capacity: lineCount * 22);

            for (int i = 0; i < lineCount; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append($"λ[{i}] = {spec[i],ValueWidth:F4}");
            }
            LyapunovSpectrumText = sb.ToString();

            int positive = 0;
            foreach (var v in spec)
                if (v > PositiveThreshold) positive++;

            LyapunovStatus = positive > 0
                ? $"✓ Хаос: {positive} λ > 0 из {spec.Length}"
                : "✗ Все λ ≤ 0 — регулярная динамика";

            KaplanYorkeText = $"D_KY = {KaplanYorke.Compute(spec):F4}";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { LyapunovStatus = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunCorrelationAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "D₂…";
        try
        {
            double dt = DtFor(cs);
            int embed = Math.Max(2, Math.Min(6, cs.Dimension + 1));
            var res = await CorrelationDimension.ComputeAsync(
                cs, cs.DefaultInitialState, Params(), dt,
                sampleCount: 3000, embeddingDim: embed, delay: 10,
                transient: 3000, ct: _cts!.Token);

            CorrelationText = double.IsNaN(res.D2)
                ? "D₂ = —"
                : $"D₂ = {res.D2:F4}  (R² = {res.RSquared:F4}, m = {embed})";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { CorrelationText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunEntropiesAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "Энтропии…";
        try
        {
            double dt = DtFor(cs);
            var series = await EntropyMeasures.CollectSeriesAsync(
                cs, cs.DefaultInitialState, Params(), dt, count: 8192,
                transient: 2000, ct: _cts!.Token);

            double sampEn = EntropyMeasures.SampleEntropy(series, m: 2, rFactor: 0.2);
            double permEn = EntropyMeasures.PermutationEntropy(series, order: 3, delay: 1);
            SampleEntropyText = $"SampEn(2, 0.2σ) = {sampEn:F4}";
            PermutationEntropyText = $"PermEn(m=3)    = {permEn:F4}";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { SampleEntropyText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunHurstAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "Hurst…";
        try
        {
            double dt = DtFor(cs);
            double h = await HurstExponent.ComputeAsync(
                cs, cs.DefaultInitialState, Params(), dt,
                sampleCount: 16384, transient: 2000, ct: _cts!.Token);
            HurstText = double.IsNaN(h) ? "H = —"
                : $"H = {h:F4}  ({(h > 0.55 ? "персист." : h < 0.45 ? "антиперсист." : "~0.5")})";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { HurstText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunDfaAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "DFA…";
        try
        {
            double dt = DtFor(cs);
            double alpha = await DetrendedFluctuation.ComputeAsync(
                cs, cs.DefaultInitialState, Params(), dt,
                sampleCount: 16384, transient: 2000, ct: _cts!.Token);
            DfaText = double.IsNaN(alpha) ? "α = —" : $"α_DFA = {alpha:F4}";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { DfaText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunZeroOneAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "0–1 тест…";
        try
        {
            //double dt = DtFor(cs);
            // Для 0-1 теста нужен грубый шаг интегрирования:
            // 20–30 семплов на характеристическое время 1/λ₁.
            double dt;
            int decimation;
            if (cs.IsDiscrete)
            {
                dt = 1.0;
                decimation = 1;
            }
            else
            {
                // RK4 с точным шагом + прореживание до ~0.2
                dt = 0.01;
                decimation = 20;
            }
            double k = await ZeroOneTest.ComputeAsync(
                cs, cs.DefaultInitialState, Params(), dt,
                sampleCount: 4000, transient: 2000, nC: 50, decimation: decimation, ct: _cts!.Token);
            ZeroOneText = double.IsNaN(k) ? "K = —"
                : $"K = {k:F4}  ({(k > 0.7 ? "хаос" : k < 0.3 ? "регулярность" : "переход")})";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ZeroOneText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunRqaAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "RQA…";
        try
        {
            double dt = DtFor(cs);
            int embed = Math.Max(2, Math.Min(4, cs.Dimension + 1));
            var res = await RecurrenceAnalysis.ComputeAsync(
                cs, cs.DefaultInitialState, Params(), dt,
                N: 600, epsFactor: 0.1, embeddingDim: embed, delay: 8,
                transient: 2000, ct: _cts!.Token);

            RqaText =
                $"RR   = {res.RecurrenceRate:P2}\n" +
                $"DET  = {res.Determinism:P2}\n" +
                $"LAM  = {res.Laminarity:P2}\n" +
                $"L    = {res.MeanDiagonal:F2}\n" +
                $"Lmax = {res.MaxDiagonal}\n" +
                $"ENTR = {res.ShannonEntropyDiag:F3}";

            // Отображение — на полном текущем размере холста
            _render.Renderer.DrawRqaMatrix(res.Matrix, 600, _render.Palette);
            _render.DisplayPixels(_render.Renderer.Pixels);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { RqaText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunPoincareAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        if (cs.IsDiscrete || cs.Dimension < 3) { PoincareText = "Только для потоков 3D+"; return; }
        NewCts(); Busy = true; ProgressText = "Пуанкаре…";
        try
        {
            double dt = DtFor(cs);
            var state = (double[])cs.DefaultInitialState.Clone();
            var rk = new Rk4Scratch(cs.Dimension);
            var pars = Params();
            for (int i = 0; i < 5000; i++) Integrator.Rk4Step(cs, state, pars, dt, rk);
            double zSum = 0; const int samples = 2000;
            for (int i = 0; i < samples; i++)
            { Integrator.Rk4Step(cs, state, pars, dt, rk); zSum += state[2]; }
            double zPlane = zSum / samples;

            var res = await PoincareSection.ComputeAsync(
                cs, cs.DefaultInitialState, Params(), dt,
                zPlane, +1, 4000, 5000, _cts!.Token);

            // Автоподгон масштаба под диапазон точек
            double xmin = double.MaxValue, xmax = double.MinValue;
            double ymin = double.MaxValue, ymax = double.MinValue;
            for (int i = 0; i < res.Count; i++)
            {
                if (res.X[i] < xmin) xmin = res.X[i];
                if (res.X[i] > xmax) xmax = res.X[i];
                if (res.Y[i] < ymin) ymin = res.Y[i];
                if (res.Y[i] > ymax) ymax = res.Y[i];
            }
            double cx = (xmin + xmax) * 0.5, cy = (ymin + ymax) * 0.5;
            double dx = Math.Max(xmax - xmin, 1e-9), dy = Math.Max(ymax - ymin, 1e-9);
            double span = Math.Max(dx, dy);
            double zoom = 0.85 * Math.Min(_render.BmpW, _render.BmpH) / span;

            var cam = new Camera3D
            {
                Yaw = 0,
                Pitch = 0,
                Zoom = zoom,
                CenterX = cx,
                CenterY = cy,
                CenterZ = 0,
                ScreenW = _render.BmpW,
                ScreenH = _render.BmpH
            };

            _render.Renderer.DrawPoincare(res, cam, _render.Palette);
            _render.DisplayPixels(_render.Renderer.Pixels);

            PoincareText = $"Точек: {res.Count}  (z₀ = {zPlane:F3})";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { PoincareText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunBasinsAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        if (cs.IsDiscrete || cs.Dimension < 2) { BasinsText = "Только для потоков 2D+"; return; }
        NewCts(); Busy = true; ProgressText = "Бассейны…";
        try
        {
            double dt = DtFor(cs);
            var res = await BasinOfAttraction.ComputeAsync(
                cs, Params(),
                -10, 10, -10, 10,
                width: 200, height: 200, dt: dt,
                transient: 2000, minSamples: 100, ct: _cts!.Token);

            _render.Renderer.DrawBasins(res, _render.Palette);
            _render.DisplayPixels(_render.Renderer.Pixels);

            BasinsText = $"Аттракторов: {res.AttractorCount}";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { BasinsText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunSymbolicAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "Symbolic…";
        try
        {
            double dt = DtFor(cs);
            var res = await SymbolicDynamics.ComputeAsync(
                cs, cs.DefaultInitialStateCached, Params(), dt,
                numSymbols: 4, sampleCount: 20000, transient: 2000,
                ct: _cts!.Token);

            if (res == null) { SymbolicText = "Недостаточно данных."; return; }

            SymbolicText =
                $"Symbols:    {res.NumSymbols}\n" +
                $"Samples:    {res.SampleCount:N0}\n" +
                $"Shannon H:  {res.ShannonEntropy:F4} bits\n" +
                $"Topol. h:   {res.TopologicalEntropy:F4} nats";
        }
        catch (OperationCanceledException) { SymbolicText = "Отменено."; }
        catch (Exception ex) { SymbolicText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunResonanceAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "Resonance…";
        try
        {
            double dt = DtFor(cs);
            var res = await ResonanceAnalysis.ComputeAsync(
                cs, cs.DefaultInitialStateCached, Params(), dt,
                nSamples: 8192, transient: 2000, topPeaks: 6,
                ct: _cts!.Token);

            if (res.PeakFrequencies.Length == 0)
            {
                ResonanceText = "Резонансы не обнаружены.";
                return;
            }

            var sb = new StringBuilder();
            sb.Append($"f₀ = {res.FundamentalFrequency:F1} (bin)\n");
            sb.Append("Peaks: ");
            for (int i = 0; i < Math.Min(3, res.PeakFrequencies.Length); i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"f{i + 1}={res.PeakFrequencies[i]:F0}");
            }

            if (res.ResonanceRelations.Length > 0)
            {
                sb.Append('\n');
                foreach (var r in res.ResonanceRelations) sb.Append(r).Append('\n');
            }
            else
            {
                sb.Append("\nРезонансы не обнаружены\n(широкополосный спектр — хаос)");
            }

            ResonanceText = sb.ToString();
        }
        catch (OperationCanceledException) { ResonanceText = "Отменено."; }
        catch (Exception ex) { ResonanceText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunMultiscaleAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "Multiscale…";
        try
        {
            double dt = DtFor(cs);
            var res = await MultiscaleEntropy.ComputeAsync(
                cs, cs.DefaultInitialStateCached, Params(), dt,
                maxScale: 12, sampleCount: 3000, transient: 2000,
                ct: _cts!.Token);

            if (res.Cancelled || res.Scales.Length == 0)
            {
                MultiscaleText = "Отменено.";
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < res.Scales.Length; i++)
            {
                double se = res.SampleEntropies[i];
                if (double.IsNaN(se)) continue;
                sb.Append($"τ={res.Scales[i],2}: SampEn={se:F4}");
                if (i % 2 == 1) sb.Append('\n'); else sb.Append("   ");
            }

            double s1 = res.SampleEntropies[0];
            double sMid = res.SampleEntropies[Math.Min(6, res.SampleEntropies.Length - 1)];
            double sLast = res.SampleEntropies[res.SampleEntropies.Length - 1];
            string shape = (sMid > s1 && sMid > sLast)
                ? "Пик → хаос/сложность"
                : (Math.Abs(sLast - s1) < 0.15 * s1
                    ? "Плоская → 1/f шум"
                    : "Убывает → белый шум / периодика");

            sb.Append("\nФорма: ").Append(shape);
            MultiscaleText = sb.ToString();
        }
        catch (OperationCanceledException) { MultiscaleText = "Отменено."; }
        catch (Exception ex) { MultiscaleText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunApEnAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "ApEn…";
        try
        {
            double dt = DtFor(cs);
            var series = await EntropyMeasures.CollectSeriesAsync(
                cs, cs.DefaultInitialStateCached, Params(), dt, count: 4000,
                transient: 2000, ct: _cts!.Token);

            if (series.Length == 0) { ApEnText = "Отменено."; return; }

            double apEn = ApproximateEntropy.Compute(series, m: 2, rFactor: 0.2, _cts!.Token);
            ApEnText = $"ApEn(2, 0.2σ) = {apEn:F4}";
        }
        catch (OperationCanceledException) { ApEnText = "Отменено."; }
        catch (Exception ex) { ApEnText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunLzAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "LZ complexity…";
        try
        {
            double dt = DtFor(cs);
            var series = await EntropyMeasures.CollectSeriesAsync(
                cs, cs.DefaultInitialStateCached, Params(), dt, count: 3000,
                transient: 2000, ct: _cts!.Token);

            if (series.Length == 0) { LzText = "Отменено."; return; }

            double c = LempelZivComplexity.Compute(series, _cts!.Token);
            LzText = double.IsNaN(c) ? "c(n) = —" : $"c(n) = {c:F4}";
        }
        catch (OperationCanceledException) { LzText = "Отменено."; }
        catch (Exception ex) { LzText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunHiguchiAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "Higuchi FD…";
        try
        {
            double dt = DtFor(cs);
            var series = await EntropyMeasures.CollectSeriesAsync(
                cs, cs.DefaultInitialStateCached, Params(), dt, count: 5000,
                transient: 2000, ct: _cts!.Token);

            if (series.Length == 0) { HiguchiText = "Отменено."; return; }

            double d = HiguchiFractalDimension.Compute(series, kMax: 20, _cts!.Token);
            HiguchiText = double.IsNaN(d) ? "D_H = —" : $"D_H = {d:F4}";
        }
        catch (OperationCanceledException) { HiguchiText = "Отменено."; }
        catch (Exception ex) { HiguchiText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunKsEntropyAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "KS entropy…";
        try
        {
            double dt = DtFor(cs);
            int steps = cs.IsDiscrete ? 200_000 : 100_000;

            var spec = await LyapunovSpectrum.ComputeAsync(
                cs, cs.DefaultInitialStateCached, Params(), dt, steps, 5000, _cts!.Token);

            double hKs = KolmogorovSinaiEntropy.Compute(spec);
            int positive = KolmogorovSinaiEntropy.PositiveExponentCount(spec);

            KsEntropyText =
                $"h_KS = {hKs:F4}\n" +
                $"Positive λ: {positive} из {spec.Length}\n" +
                (positive > 1 ? "⚠ Гиперхаос" : positive == 1 ? "✓ Хаос" : "— Регулярность");
        }
        catch (OperationCanceledException) { KsEntropyText = "Отменено."; }
        catch (Exception ex) { KsEntropyText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunBasinEntropyAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        if (cs.IsDiscrete || cs.Dimension < 2) { BasinEntropyText = "Только для потоков 2D+"; return; }
        NewCts(); Busy = true; ProgressText = "Basin entropy…";
        try
        {
            var basins = await BasinOfAttraction.ComputeAsync(
                cs, Params(), -10, 10, -10, 10,
                width: 150, height: 150, dt: DtFor(cs),
                transient: 2000, minSamples: 100, ct: _cts!.Token);

            double h = BasinEntropy.ShannonEntropy(basins);
            double hNorm = BasinEntropy.NormalizedEntropy(basins);

            BasinEntropyText =
                $"Аттракторов: {basins.AttractorCount}\n" +
                $"H = {h:F4} bits\n" +
                $"H_norm = {hNorm:F4}";
        }
        catch (OperationCanceledException) { BasinEntropyText = "Отменено."; }
        catch (Exception ex) { BasinEntropyText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    private async Task RunPredictabilityAsync()
    {
        if (Cs is not ChaoticSystem cs) return;
        NewCts(); Busy = true; ProgressText = "Predictability…";
        try
        {
            double dt = DtFor(cs);
            var spec = await LyapunovSpectrum.ComputeAsync(
                cs, cs.DefaultInitialStateCached, Params(),
                dt, steps: 50_000, transient: 5000, ct: _cts!.Token);

            double lambda1 = spec.Length > 0 ? spec[0] : 0;

            // Оценка размера аттрактора: по нескольким пробным запускам
            double size = EstimateAttractorSizeNumeric(cs, Params(), dt);

            var res = PredictabilityHorizon.Estimate(lambda1, size, epsilon0: 1e-3);

            PredictabilityText =
                lambda1 <= 0
                    ? "Регулярная динамика — горизонт бесконечен."
                    : $"λ₁ = {lambda1:F4}\n" +
                      $"Δ  = {size:F2}\n" +
                      $"t_h ≈ {res.Horizon:F2} t.e.";
        }
        catch (OperationCanceledException) { PredictabilityText = "Отменено."; }
        catch (Exception ex) { PredictabilityText = "Ошибка: " + ex.Message; }
        finally { Busy = false; ProgressText = ""; }
    }

    /// <summary>
    /// Оценивает характерный размер аттрактора Δ по размаху координат вдоль
    /// траектории.
    /// </summary>
    /// <remarks>
    /// <para>Метод продвигает систему на 1000 шагов прогрева, затем
    /// отслеживает min/max по всем трём первым координатам ещё 5000 шагов.
    /// Размер = максимум из размахов (Δx, Δy, Δz).</para>
    /// <para>Используется как Δ в формуле горизонта предсказуемости
    /// <c>t_h = (1/λ₁)·log(Δ/ε₀)</c>. Для систем высокой размерности
    /// рассматриваются только первые три координаты — этого достаточно для
    /// практической оценки.</para>
    /// </remarks>
    /// <param name="cs">Система.</param>
    /// <param name="p">Значения параметров.</param>
    /// <param name="dt">Шаг интегрирования (для потоков).</param>
    /// <returns>
    /// Характерный размер аттрактора в единицах фазового пространства.
    /// Возвращает 0, если траектория вырождена в точку.
    /// </returns>
    private double EstimateAttractorSizeNumeric(ChaoticSystem cs, double[] p, double dt)
    {
        // Быстрая оценка размера аттрактора: гоняем траекторию и ищем
        // min/max по первым 3 координатам.
        var state = (double[]) cs.DefaultInitialStateCached.Clone();
        var scratch = new double[cs.Dimension];
        var rk = cs.IsDiscrete ? null : new Rk4Scratch(cs.Dimension);

        for (int i = 0; i < 1000; i++)
            Integrator.StepOnce(cs, state, scratch, p, dt, rk);

        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        double minZ = double.MaxValue, maxZ = double.MinValue;

        for (int i = 0; i < 5000; i++)
        {
            Integrator.StepOnce(cs, state, scratch, p, dt, rk);
            if (state[0] < minX) minX = state[0];
            if (state[0] > maxX) maxX = state[0];
            if (state.Length > 1)
            {
                if (state[1] < minY) minY = state[1];
                if (state[1] > maxY) maxY = state[1];
            }
            if (state.Length > 2)
            {
                if (state[2] < minZ) minZ = state[2];
                if (state[2] > maxZ) maxZ = state[2];
            }
        }

        double dx = maxX - minX;
        double dy = state.Length > 1 ? maxY - minY : 0;
        double dz = state.Length > 2 ? maxZ - minZ : 0;
        return Math.Max(dx, Math.Max(dy, dz));
    }

    /// <summary>
    /// Сбрасывает все текстовые результаты анализа. Вызывается при смене
    /// системы или изменении её параметров, чтобы «застрявшие» значения
    /// от предыдущей конфигурации не вводили в заблуждение.
    /// </summary>
    private void ResetAllResults()
    {
        // Отменяем текущие задачи
        _cts?.Cancel();

        LyapunovSpectrumText = "—";
        LyapunovStatus = "Нажмите «λ Спектр»";
        KaplanYorkeText = "—";
        CorrelationText = "—";
        BoxCountingText = "—";
        SampleEntropyText = "—";
        PermutationEntropyText = "—";
        HurstText = "—";
        DfaText = "—";
        ZeroOneText = "—";
        RqaText = "—";
        PoincareText = "—";
        BasinsText = "—";
        ApEnText = "—";
        LzText = "—";
        HiguchiText = "—";
        KsEntropyText = "—";
        BasinEntropyText = "—";
        PredictabilityText = "—";
        MultiscaleText = "-";
        SymbolicText = "-";
        ResonanceText = "-";
        ProgressText = "";
        Busy = false;
    }

    /// <summary>
    /// Запускает все методы анализа последовательно. Тяжёлые методы
    /// (Пуанкаре, Бассейны) выполняются в конце, потому что они самые
    /// медленные. Если система не поддерживает метод (например, дискретная
    /// для Пуанкаре), он молча пропускается.
    /// </summary>
    private async Task RunAllAsync()
    {
        if (Cs is not ChaoticSystem cs) return;

        _runAllStep = 0;
        _runAllTotal = RunAllTotalSteps;

        async Task Step(string label, Func<Task> action)
        {
            _runAllStep++;
            ProgressText = $"[{_runAllStep}/{_runAllTotal}] {label}…";
            await action();
            if (_cts?.IsCancellationRequested == true) return;
        }

        await Step("Lyapunov", RunLyapunovAsync);
        await Step("KS entropy", RunKsEntropyAsync);
        await Step("Correlation D₂", RunCorrelationAsync);
        await Step("Entropies", RunEntropiesAsync);
        await Step("ApEn", RunApEnAsync);
        await Step("Hurst", RunHurstAsync);
        await Step("DFA", RunDfaAsync);
        await Step("0–1 test", RunZeroOneAsync);
        await Step("LZ", RunLzAsync);
        await Step("Higuchi FD", RunHiguchiAsync);
        // Символика и резонансы (для систем с достаточной длиной)
        if (cs.Dimension >= 1)
            await Step("Symbolic", RunSymbolicAsync);
        await Step("Resonance", RunResonanceAsync);
        await Step("Multiscale", RunMultiscaleAsync);
        await Step("Predictability", RunPredictabilityAsync);

        // Box-counting по уже отрисованному холсту
        if (_render.Density is ushort[] dens)
        {
            double d = BoxCounting.Compute(dens, _render.BmpW, _render.BmpH);
            BoxCountingText = double.IsNaN(d) ? "D_box = —" : $"D_box = {d:F4}";
        }

        // Геометрия — самая медленная, в конце
        if (!cs.IsDiscrete && cs.Dimension >= 3)
            await Step("Poincaré", RunPoincareAsync);

        if (!cs.IsDiscrete && cs.Dimension >= 2)
            await Step("Basin entropy", RunBasinEntropyAsync);

        ProgressText = "Готово.";
    }

    //private async Task RunAllAsync()
    //{
    //    await RunLyapunovAsync();
    //    await RunCorrelationAsync();
    //    await RunEntropiesAsync();
    //    await RunHurstAsync();
    //    await RunDfaAsync();
    //    await RunZeroOneAsync();

    //    if (_render.Density is ushort[] dens)
    //    {
    //        double d = BoxCounting.Compute(dens, _render.BmpW, _render.BmpH);
    //        BoxCountingText = double.IsNaN(d) ? "D_box = —" : $"D_box = {d:F4}";
    //    }
    //}
}

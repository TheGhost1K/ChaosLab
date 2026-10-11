using System.Diagnostics;
using System.Drawing;
using ChaosLab.Analysis;
using ChaosLab.Core;
using ChaosLab.Rendering;

namespace ChaosLab.ViewModels;

public sealed partial class RenderViewModel
{
    // ================================================================
    // ГЛАВНЫЙ МЕТОД: ДИСПЕТЧЕР ПО РЕЖИМАМ
    // ================================================================

    /// <summary>
    /// Вычисляет один кадр в зависимости от текущего режима отображения.
    /// Вызывается планировщиком в фоновом потоке.
    /// </summary>
    /// <param name="ct">Токен отмены, проверяется внутри тяжёлых циклов.</param>
    /// <returns>Массив Bgr32-пикселей и число отрисованных точек.</returns>
    private (int[], long) ComputeFrame(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        int w = _bmpW, h = _bmpH;

        try
        {
            // Gray-Scott — отдельная ветка (не ChaoticSystem)
            if (_system.Field is Systems.GrayScottField field)
            {
                var result = ComputeGrayScottFrame(field, w, h, ct);
                sw.Stop();
                _lastFrameStopwatchMs = sw.ElapsedMilliseconds;
                return result;
            }

            var cs = _system.Chaotic!;

            // Trail — самый простой случай, не требует общих буферов
            if (Mode == RenderMode.Trail)
            {
                var result = ComputeTrailFrame(cs, w, h, ct);
                sw.Stop();
                _lastFrameStopwatchMs = sw.ElapsedMilliseconds;
                return result;
            }

            // Специализированные режимы
            (int[], long)? specialized = Mode switch
            {
                RenderMode.Bifurcation => ComputeBifurcationFrame(cs, w, h, ct),
                RenderMode.LyapunovMap => ComputeLyapunovMapFrame(cs, w, h, ct),
                RenderMode.Cobweb => ComputeCobwebFrame(cs, w, h),
                RenderMode.PowerSpectrum
                    or RenderMode.ReturnMap
                    or RenderMode.FnnMi
                    or RenderMode.Multifractal
                    or RenderMode.MultiscaleEntropyCurve
                    => ComputePlotFrame(cs, w, h, ct),
                _ => null,
            };

            if (specialized is { } spec)
            {
                sw.Stop();
                _lastFrameStopwatchMs = sw.ElapsedMilliseconds;
                return spec;
            }

            // Стандартный аттрактор (с выбором интегратора)
            var attractor = ComputeAttractorFrame(cs, w, h, ct);
            sw.Stop();
            _lastFrameStopwatchMs = sw.ElapsedMilliseconds;
            return attractor;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _lastFrameStopwatchMs = sw.ElapsedMilliseconds;
            return (_renderer.Pixels, 0);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            sw.Stop();
            _lastFrameStopwatchMs = sw.ElapsedMilliseconds;
            return (_renderer.Pixels, 0);
        }
    }

    // ================================================================
    // GRAY-SCOTT
    // ================================================================

    /// <summary>
    /// Рендерит поле Gray-Scott: 800 шагов эволюции и отрисовка V-поля.
    /// </summary>
    private (int[], long) ComputeGrayScottFrame(
        Systems.GrayScottField field, int w, int h, CancellationToken ct)
    {
        var p = _system.CurrentParamValues();
        field.Step(800, p, ct);
        field.RenderTo(_renderer.Pixels, w, h, Palette);
        return (_renderer.Pixels, (long) field.GridW * field.GridH);
    }

    // ================================================================
    // БИФУРКАЦИИ
    // ================================================================

    /// <summary>
    /// Строит бифуркационную диаграмму. Для отображений — классическое
    /// дерево Фейгенбаума, для потоков — метод локальных максимумов.
    /// </summary>
    private (int[], long) ComputeBifurcationFrame(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        _renderer.Clear();

        if (cs.Parameters.Length == 0)
            return (_renderer.Pixels, 0);

        int pi = Math.Clamp(BifParamIndex, 0, cs.Parameters.Length - 1);
        var pd = cs.Parameters[pi];

        if (cs.IsDiscrete)
        {
            var (yLo, yHi) = cs.BifurcationRange;
            BifurcationRenderer.Render(
                _renderer.Pixels, w, h, cs, pi, pd.Min, pd.Max,
                _system.CurrentParamValues(), cs.DefaultInitialStateCached,
                transient: 500, iterations: 400, coordIndex: 0,
                yLo, yHi, Palette, ct);
        }
        else
        {
            var (yLo, yHi) = DetectFlowBifurcationRange(cs, pi, pd, ct);
            BifurcationRenderer.RenderFlow(
                _renderer.Pixels, w, h, cs, pi, pd.Min, pd.Max,
                _system.CurrentParamValues(), cs.DefaultInitialStateCached,
                transientSteps: 1500, captureSteps: 4000,
                coordIndex: 0, dt: Dt,
                yLo, yHi, Palette, ct);
        }

        return (_renderer.Pixels, (long) w * 400);
    }

    /// <summary>
    /// Быстрая оценка диапазона оси Y для бифуркационной диаграммы потока.
    /// Прогоняет систему на 5 значениях параметра и берёт min/max.
    /// </summary>
    private (double lo, double hi) DetectFlowBifurcationRange(
        ChaoticSystem cs, int paramIndex, ParameterDef pd, CancellationToken ct)
    {
        var p = _system.CurrentParamValues();
        var rk = new Rk4Scratch(cs.Dimension);
        var state = new double[cs.Dimension];

        double vmin = double.MaxValue, vmax = double.MinValue;
        const int samples = 5;

        for (int s = 0; s < samples; s++)
        {
            if (ct.IsCancellationRequested) break;

            double pv = pd.Min + (pd.Max - pd.Min) * s / (samples - 1);
            p[paramIndex] = pv;
            Array.Copy(cs.DefaultInitialStateCached, state, cs.Dimension);

            for (int i = 0; i < 2000; i++) Integrator.Rk4Step(cs, state, p, Dt, rk);
            for (int i = 0; i < 3000; i++)
            {
                Integrator.Rk4Step(cs, state, p, Dt, rk);
                double v = state[0];
                if (v < vmin) vmin = v;
                if (v > vmax) vmax = v;
            }
        }

        if (vmin >= vmax) return (-25, 25);

        double margin = (vmax - vmin) * 0.1;
        return (vmin - margin, vmax + margin);
    }

    // ================================================================
    // КАРТА ЛЯПУНОВА
    // ================================================================

    /// <summary>
    /// Строит карту старшего показателя Ляпунова по двум параметрам.
    /// Прогрессивный рендер: сначала превью 256×256, затем полное.
    /// </summary>
    private (int[], long) ComputeLyapunovMapFrame(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        if (cs.Parameters.Length < 2)
        {
            _renderer.Clear();
            return (_renderer.Pixels, 0);
        }

        int xi = Math.Clamp(LyapXIndex, 0, cs.Parameters.Length - 1);
        int yi = Math.Clamp(LyapYIndex, 0, cs.Parameters.Length - 1);
        if (xi == yi) yi = (xi + 1) % cs.Parameters.Length;

        var pdX = cs.Parameters[xi];
        var pdY = cs.Parameters[yi];
        int iters = cs.IsDiscrete ? 400 : 800;
        int trans = cs.IsDiscrete ? 150 : 300;

        var progress = new Progress<(int[] px, int pw, int ph)>(p =>
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;
            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (p.pw == w && p.ph == h)
                        Array.Copy(p.px, _renderer.Pixels, p.px.Length);
                    else
                        UpscaleNearest(p.px, p.pw, p.ph, _renderer.Pixels, w, h);

                    _bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, w, h),
                        _renderer.Pixels, w * 4, 0);
                    Bitmap = _bitmap;
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Background);
        });

        var final = LyapunovMap.ComputeProgressiveAsync(
            cs, xi, pdX.Min, pdX.Max,
                yi, pdY.Min, pdY.Max,
            w, h, Dt, iters, trans, Palette, progress,
            finalOutputBuffer: _renderer.Pixels,
            ct: ct).GetAwaiter().GetResult();

        if (!ReferenceEquals(final, _renderer.Pixels))
            Array.Copy(final, _renderer.Pixels,
                Math.Min(final.Length, _renderer.Pixels.Length));

        return (_renderer.Pixels, (long) w * h);
    }

    // ================================================================
    // COBWEB
    // ================================================================

    /// <summary>
    /// Рисует cobweb-диаграмму для 1D-отображений.
    /// </summary>
    private (int[], long) ComputeCobwebFrame(ChaoticSystem cs, int w, int h)
    {
        if (!cs.IsDiscrete || cs.Dimension != 1)
        {
            _renderer.Clear();
            return (_renderer.Pixels, 0);
        }

        var (xLo, xHi) = cs.BifurcationRange;
        CobwebRenderer.Render(
            _renderer.Pixels, w, h, cs, _system.CurrentParamValues(),
            xLo, xHi, cs.DefaultInitialStateCached[0], 60, Palette);

        return (_renderer.Pixels, 60);
    }

    // ================================================================
    // ГРАФИКИ (Power Spectrum / Return Map / FNN+MI / Multifractal / MSE)
    // ================================================================

    /// <summary>
    /// Универсальный обработчик режимов, отображающих 1D-графики.
    /// </summary>
    private (int[], long) ComputePlotFrame(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        switch (Mode)
        {
            case RenderMode.PowerSpectrum:
                return RenderPowerSpectrum(cs, w, h, ct);
            case RenderMode.ReturnMap:
                return RenderReturnMap(cs, w, h, ct);
            case RenderMode.FnnMi:
                return RenderFnnMi(cs, w, h, ct);
            case RenderMode.Multifractal:
                return RenderMultifractal(cs, w, h, ct);
            case RenderMode.MultiscaleEntropyCurve:
                return RenderMultiscaleEntropy(cs, w, h, ct);
            default:
                _renderer.Clear();
                return (_renderer.Pixels, 0);
        }
    }

    /// <summary>Лог-график спектра мощности.</summary>
    private (int[], long) RenderPowerSpectrum(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        var ps = PowerSpectrum.ComputeAsync(
            cs, cs.DefaultInitialStateCached, _system.CurrentParamValues(),
            Dt, 8192, 2000, ct).GetAwaiter().GetResult();

        if (ps.Length == 0)
        {
            _renderer.Clear();
            return (_renderer.Pixels, 0);
        }

        var xvals = new double[ps.Length];
        var logPs = new double[ps.Length];
        for (int i = 0; i < ps.Length; i++)
        {
            xvals[i] = i;
            logPs[i] = Math.Log10(ps[i] + 1e-12);
        }

        PlotRenderer.Render(_renderer.Pixels, w, h, xvals, logPs,
            0x080B10, 0x57A9FF, 0x1A2530, 1);

        return (_renderer.Pixels, ps.Length);
    }

    /// <summary>Возвратное отображение xₙ → xₙ₊₁.</summary>
    private (int[], long) RenderReturnMap(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        var rm = ReturnMap.ComputeAsync(
            cs, cs.DefaultInitialStateCached, _system.CurrentParamValues(),
            Dt, 0, 2000, 5000, ct).GetAwaiter().GetResult();

        Array.Clear(_renderer.Pixels);

        if (rm.Count == 0)
            return (_renderer.Pixels, 0);

        double span = Math.Max(rm.MaxX - rm.MinX, rm.MaxY - rm.MinY);
        if (span < 1e-9) span = 1;

        var cam = new Camera3D
        {
            Yaw = 0,
            Pitch = 0,
            Zoom = 0.9 * Math.Min(w, h) / span,
            CenterX = (rm.MinX + rm.MaxX) * 0.5,
            CenterY = (rm.MinY + rm.MaxY) * 0.5,
            CenterZ = 0,
            ScreenW = w,
            ScreenH = h,
        };

        for (int i = 0; i < rm.Count; i++)
        {
            var (px, py) = cam.Project(rm.X[i], rm.Y[i], 0);
            if ((uint) px < (uint) w && (uint) py < (uint) h)
                _renderer.Pixels[py * w + px] = 0x57A9FF;
        }

        return (_renderer.Pixels, rm.Count);
    }

    /// <summary>Mutual Information — FNN диагностика вложения.</summary>
    private (int[], long) RenderFnnMi(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        var series = EntropyMeasures.CollectSeriesAsync(
            cs, cs.DefaultInitialStateCached, _system.CurrentParamValues(),
            Dt, 4000, 2000, decimation: 1, ct).GetAwaiter().GetResult();

        if (series.Length == 0)
        {
            _renderer.Clear();
            return (_renderer.Pixels, 0);
        }

        var (tau, mi) = EmbeddingDiagnostics.MutualInformation(series, 40, 32);
        PlotRenderer.Render(_renderer.Pixels, w, h, tau, mi,
            0x080B10, 0x57A9FF, 0x1A2530, 1);

        return (_renderer.Pixels, tau.Length);
    }

    /// <summary>График D_q для мультифрактального анализа.</summary>
    private (int[], long) RenderMultifractal(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        var mf = Multifractal.ComputeAsync(
            cs, cs.DefaultInitialStateCached, _system.CurrentParamValues(),
            Dt, 1000, 3, 10, 2000, ct).GetAwaiter().GetResult();

        if (mf.Q.Length == 0)
        {
            _renderer.Clear();
            return (_renderer.Pixels, 0);
        }

        PlotRenderer.Render(_renderer.Pixels, w, h, mf.Q, mf.Dq,
            0x080B10, 0x57A9FF, 0x1A2530, 2, drawMarkers: true);

        return (_renderer.Pixels, mf.Q.Length);
    }

    /// <summary>Кривая SampEn(τ) для multiscale entropy.</summary>
    private (int[], long) RenderMultiscaleEntropy(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        var mse = MultiscaleEntropy.ComputeAsync(
            cs, cs.DefaultInitialStateCached, _system.CurrentParamValues(),
            Dt, maxScale: 12, sampleCount: 3000, transient: 2000, ct)
            .GetAwaiter().GetResult();

        if (mse.Cancelled || mse.Scales.Length == 0)
        {
            _renderer.Clear();
            return (_renderer.Pixels, 0);
        }

        var xs = new double[mse.Scales.Length];
        var ys = new double[mse.Scales.Length];
        for (int i = 0; i < mse.Scales.Length; i++)
        {
            xs[i] = mse.Scales[i];
            ys[i] = mse.SampleEntropies[i];
        }

        PlotRenderer.Render(_renderer.Pixels, w, h, xs, ys,
            0x080B10, 0x57A9FF, 0x1A2530, 2, drawMarkers: true);

        return (_renderer.Pixels, mse.Scales.Length);
    }

    // ================================================================
    // ОБЫЧНЫЙ АТТРАКТОР (с выбором интегратора)
    // ================================================================

    /// <summary>
    /// Строит фазовый портрет аттрактора. Поддерживает три интегратора:
    /// Verlet (симплектический), RK45 (адаптивный) и RK4 (по умолчанию).
    /// </summary>
    private (int[], long) ComputeAttractorFrame(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        _renderer.Clear();
        var cam = _camera.Snapshot(w, h);
        var pars = _system.CurrentParamValues();
        int steps = (int) Math.Min(Steps, 50_000_000);
        var state = (double[]) cs.DefaultInitialStateCached.Clone();

        if (UseVerlet && cs is ISymplecticSystem sym)
        {
            var vs = new VerletScratch(sym.HalfDimension);
            for (int i = 0; i < 500; i++)
                Integrator.VerletStep(sym, state, pars, Dt, vs);

            for (int i = 0; i < steps; i++)
            {
                if ((i & 0x3FFF) == 0 && ct.IsCancellationRequested) break;
                Integrator.VerletStep(sym, state, pars, Dt, vs);
                cs.Project3D(state, _xyzBuffer);
                _renderer.PlotPoint(_xyzBuffer[0], _xyzBuffer[1], _xyzBuffer[2], cam);
            }
        }
        else if (UseAdaptiveRk45 && !cs.IsDiscrete)
        {
            var rks = new Rk45Scratch(cs.Dimension);
            double rkDt = Dt;
            for (int i = 0; i < 500; i++)
            {
                while (!Integrator.Rk45Step(cs, state, pars, ref rkDt, Rk45Tolerance, rks))
                    if (rkDt < 1e-12) break;
            }

            for (int i = 0; i < steps; i++)
            {
                if ((i & 0x3FFF) == 0 && ct.IsCancellationRequested) break;

                int guard = 0;
                while (!Integrator.Rk45Step(cs, state, pars, ref rkDt, Rk45Tolerance, rks))
                {
                    if (rkDt < 1e-12 || ++guard > 100) break;
                }

                cs.Project3D(state, _xyzBuffer);
                _renderer.PlotPoint(_xyzBuffer[0], _xyzBuffer[1], _xyzBuffer[2], cam);
            }
        }
        else
        {
            _renderer.Accumulate(cs, state, pars, Dt, steps, cam, warmup: 500, ct);
        }

        _renderer.Colorize(Palette, Gamma);

        // Vector Field overlay (только для 2D отображений)
        if (ShowVectorField && cs is { IsDiscrete: true, Dimension: 2 })
        {
            RenderVectorFieldOverlay(cs, pars, cam, w, h);
        }

        return (_renderer.Pixels, steps);
    }

    /// <summary>
    /// Рисует слой стрелок векторного поля поверх аттрактора.
    /// Область рисования автоматически подстраивается под bounding box
    /// занятых пикселей.
    /// </summary>
    private void RenderVectorFieldOverlay(
        ChaoticSystem cs, double[] pars, Camera3D cam, int w, int h)
    {
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (_renderer.Density[y * w + x] > 0)
                {
                    double vx = (x - cam.ScreenW * 0.5) / cam.Zoom + cam.CenterX;
                    double vy = cam.CenterY - (y - cam.ScreenH * 0.5) / cam.Zoom;
                    if (vx < xMin) xMin = vx;
                    if (vx > xMax) xMax = vx;
                    if (vy < yMin) yMin = vy;
                    if (vy > yMax) yMax = vy;
                }

        if (xMin < xMax && yMin < yMax)
        {
            VectorFieldRenderer.RenderOver(
                _renderer.Pixels, w, h, cs, pars, cam,
                xMin, xMax, yMin, yMax,
                arrowsX: 24, arrowsY: 24,
                arrowLen: 0.4, color: 0x7DDBFF);
        }
    }

    // ================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ================================================================

    /// <summary>
    /// Апскейл массива пикселей методом nearest-neighbor.
    /// Используется для растяжения превью карты Ляпунова на весь холст.
    /// </summary>
    private static void UpscaleNearest(
        int[] src, int sw, int sh, int[] dst, int dw, int dh)
    {
        for (int y = 0; y < dh; y++)
        {
            int sy = y * sh / dh;
            int rowIn = sy * sw;
            int rowOut = y * dw;
            for (int x = 0; x < dw; x++)
            {
                int sx = x * sw / dw;
                dst[rowOut + x] = src[rowIn + sx];
            }
        }
    }
}

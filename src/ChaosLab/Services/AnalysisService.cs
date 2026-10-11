namespace ChaosLab.Services;

/// <summary>
/// Обёртка для запуска фоновых задач анализа с автоматической отменой
/// предыдущей при старте новой.
/// </summary>
/// <remarks>
/// Простой паттерн: хранит один <see cref="CancellationTokenSource"/>.
/// При <see cref="StartNew"/> предыдущий отменяется и заменяется новым.
/// </remarks>
public sealed class AnalysisService : IDisposable
{
    private CancellationTokenSource? _cts;

    /// <summary>Отменяет предыдущий токен и возвращает новый.</summary>
    /// <returns>Свежий токен для новой задачи.</returns>
    public CancellationToken StartNew()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }

    /// <summary>Отменяет текущую задачу (если есть).</summary>
    public void Cancel() => _cts?.Cancel();

    /// <summary>Запускает работу в фоне с токеном отмены.</summary>
    public Task<T> RunAsync<T>(Func<CancellationToken, T> work)
    {
        var token = StartNew();
        return Task.Run(() => work(token), token);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace ChaosLab.ViewModels;

/// <summary>
/// Базовый класс для всех ViewModel-ей. Реализует <see cref="INotifyPropertyChanged"/>
/// с вспомогательными методами <see cref="OnPropertyChanged"/> и <see cref="Set{T}"/>.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private DispatcherTimer? _labelTimer;
    private readonly HashSet<string> _pendingLabels = new(StringComparer.Ordinal);

    /// <summary>Оповещает UI об изменении свойства.</summary>
    /// <param name="name">Имя свойства (автоматически определяется caller-ом).</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Устанавливает поле и вызывает <see cref="OnPropertyChanged"/> только
    /// если значение изменилось.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="field">Ссылка на поле.</param>
    /// <param name="value">Новое значение.</param>
    /// <param name="name">Имя свойства.</param>
    /// <returns><c>true</c>, если значение было изменено.</returns>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>
    /// Оповещает UI об изменении свойства с задержкой (для часто меняющихся
    /// меток, чтобы не спамить WPF-биндинги). Задержка коалесцирует
    /// множественные изменения в одно обновление.
    /// </summary>
    /// <param name="name">Имя свойства.</param>
    /// <param name="ms">Задержка в миллисекундах (по умолчанию 100).</param>
    protected void OnPropertyChangedThrottled(string name, int ms = 100)
    {
        lock (_pendingLabels) _pendingLabels.Add(name);

        if (_labelTimer == null)
        {
            _labelTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
            _labelTimer.Tick += (_, _) =>
            {
                _labelTimer!.Stop();
                string[] toNotify;
                lock (_pendingLabels)
                {
                    toNotify = new string[_pendingLabels.Count];
                    _pendingLabels.CopyTo(toNotify);
                    _pendingLabels.Clear();
                }
                foreach (var n in toNotify) OnPropertyChanged(n);
            };
        }

        _labelTimer.Stop();
        _labelTimer.Start();
    }
}

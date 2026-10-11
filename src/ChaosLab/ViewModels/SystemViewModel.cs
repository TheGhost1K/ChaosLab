using ChaosLab.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ChaosLab.ViewModels;

/// <summary>
/// ViewModel выбора системы и управления её параметрами.
/// </summary>
/// <remarks>
/// Содержит <see cref="ObservableCollection{T}"/> параметров, каждая с
/// двусторонней привязкой к слайдеру. Изменения параметра триггерят
/// <see cref="ParametersChanged"/>.
/// </remarks>
public sealed class SystemViewModel : BaseViewModel
{
    private ISimulation _current = null!;

    /// <summary>Полный список доступных систем.</summary>
    public ISimulation[] AllSystems => SystemCatalog.All;

    /// <summary>Текущая выбранная система.</summary>
    public ISimulation Current
    {
        get => _current;
        set
        {
            if (value == null || ReferenceEquals(value, _current)) return;
            _current = value;
            OnPropertyChanged();
            RebuildParameters();
            OnPropertyChanged(nameof(IsChaotic));
            OnPropertyChanged(nameof(IsField));
            OnPropertyChanged(nameof(IsBifurcatable));
            SystemChanged?.Invoke();
        }
    }

    /// <summary>Коллекция параметров выбранной системы.</summary>
    public ObservableCollection<ParameterViewModel> Params { get; } = new();

    /// <summary>Является ли текущая система точечной (не полем).</summary>
    public bool IsChaotic => _current is ChaoticSystem;

    /// <summary>Является ли текущая система 2D-полем.</summary>
    public bool IsField => _current is not ChaoticSystem;

    /// <summary>Можно ли строить бифуркационную диаграмму.</summary>
    public bool IsBifurcatable => _current is ChaoticSystem cs && cs.Category == "Map";

    /// <summary>Событие смены системы.</summary>
    public event Action? SystemChanged;

    /// <summary>Событие изменения любого параметра.</summary>
    public event Action? ParametersChanged;

    /// <summary>
    /// Создаёт ViewModel системы, загружая первую систему из каталога
    /// по умолчанию и строя для неё список параметров.
    /// </summary>
    public SystemViewModel()
    {
        _current = SystemCatalog.All[0];
        RebuildParameters();
    }

    /// <summary>Текущая система как <see cref="ChaoticSystem"/> (или null).</summary>
    public ChaoticSystem? Chaotic => _current as ChaoticSystem;

    /// <summary>Текущая система как <see cref="Systems.GrayScottField"/> (или null).</summary>
    public Systems.GrayScottField? Field => _current as Systems.GrayScottField;

    /// <summary>Возвращает массив текущих значений параметров.</summary>
    public double[] CurrentParamValues()
    {
        var arr = new double[Params.Count];
        for (int i = 0; i < Params.Count; i++) arr[i] = Params[i].Value;
        return arr;
    }

    private void RebuildParameters()
    {
        foreach (var p in Params) p.PropertyChanged -= OnParamChanged;
        Params.Clear();
        foreach (var def in _current.Parameters)
        {
            var vm = new ParameterViewModel(def);
            vm.PropertyChanged += OnParamChanged;
            Params.Add(vm);
        }
    }

    private void OnParamChanged(object? sender, PropertyChangedEventArgs e)
    {
        ParametersChanged?.Invoke();
    }
}

/// <summary>
/// ViewModel одного параметра системы.
/// </summary>
public sealed class ParameterViewModel : BaseViewModel
{
    private double _value;
    private bool _isSwept;

    /// <summary>Определение параметра (имя, диапазон, дефолт).</summary>
    public ParameterDef Def { get; }

    /// <summary>Текущее значение (двусторонняя привязка к слайдеру).</summary>
    public double Value
    {
        get => _value;
        set => Set(ref _value, value);
    }

    /// <summary>
    /// Разворачивается ли этот параметр по оси в текущем режиме.
    /// Если <c>true</c> — слайдер визуально помечается.
    /// </summary>
    public bool IsSwept
    {
        get => _isSwept;
        set => Set(ref _isSwept, value);
    }

    /// <summary>
    /// Создаёт ViewModel параметра с указанным определением.
    /// </summary>
    /// <param name="def">Определение параметра (имя, диапазон, значение по умолчанию).</param>
    public ParameterViewModel(ParameterDef def)
    {
        Def = def;
        _value = def.Default;
    }
}

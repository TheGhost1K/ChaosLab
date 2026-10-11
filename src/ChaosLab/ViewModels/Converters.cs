using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChaosLab.ViewModels;

/// <summary>
/// Логарифмический конвертер для слайдеров: значение слайдера = log10(value).
/// </summary>
/// <remarks>Используется для параметров, где важна относительная точность
/// (шаг интегрирования, число итераций).</remarks>
public sealed class LogConverter : IValueConverter
{
    /// <summary>Единственный экземпляр для XAML.</summary>
    public static readonly LogConverter Instance = new();

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double v && v > 0 ? Math.Log10(v) : -3.0;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Math.Pow(10, System.Convert.ToDouble(value));
}

/// <summary>Конвертер между <see cref="RenderMode"/> и int для ComboBox.</summary>
public sealed class ModeConverter : IValueConverter
{
    /// <summary>Единственный экземпляр.</summary>
    public static readonly ModeConverter Instance = new();

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is RenderMode m ? (int)m : 0;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int i ? (RenderMode)i : RenderMode.Attractor;
}

/// <summary>Конвертер bool → <see cref="Visibility"/>.</summary>
public sealed class BoolToVisibility : IValueConverter
{
    /// <summary>Единственный экземпляр.</summary>
    public static readonly BoolToVisibility Instance = new();

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Инвертирует bool (для IsEnabled).</summary>
public sealed class InvertBool : IValueConverter
{
    /// <summary>Единственный экземпляр.</summary>
    public static readonly InvertBool Instance = new();

    /// <inheritdoc/>
    public object Convert(object v, Type t, object p, CultureInfo c) => !(v is bool b && b);

    /// <inheritdoc/>
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => !(v is bool b && b);
}

/// <summary>Инвертирует bool в Opacity (для «затемнения» недоступных слайдеров).</summary>
public sealed class InvertBoolToOpacity : IValueConverter
{
    /// <summary>Единственный экземпляр.</summary>
    public static readonly InvertBoolToOpacity Instance = new();

    /// <inheritdoc/>
    public object Convert(object v, Type t, object p, CultureInfo c) => v is bool b && b ? 0.4 : 1.0;

    /// <inheritdoc/>
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

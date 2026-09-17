using System;
using System.Globalization;
using System.Windows.Data;

namespace AnhaltewegRechner.Converters;

/// <summary>Invertiert einen bool-Wert. Wird für den Trocken/Nass-Umschalter benötigt,
/// da beide Zustände aus derselben IsWetRoad-Eigenschaft abgeleitet werden.</summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

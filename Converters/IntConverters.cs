using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace IStock.Converters;

public static class IntConverters
{
    public static readonly IValueConverter IsGreaterThanZero =
        new FuncValueConverter<int, bool>(v => v > 0);

    public static readonly IValueConverter IsZero =
        new FuncValueConverter<int, bool>(v => v == 0);
}
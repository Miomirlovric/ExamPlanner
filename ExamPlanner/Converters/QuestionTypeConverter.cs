using System;
using System.Globalization;
using Domain.Values;
using Microsoft.Maui.Controls;

namespace ExamPlanner.Converters;

public class QuestionTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is QuestionTypeEnum type)
        {
            return type.ToNormalizedString();
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

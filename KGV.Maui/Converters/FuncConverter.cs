using System;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Converters
{
    public class FuncConverter<TIn, TOut> : IValueConverter
    {
        private readonly Func<TIn, TOut> _func;

        public FuncConverter(Func<TIn, TOut> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TIn t) return _func(t);
            try
            {
                // try cast
                return _func((TIn)value!);
            }
            catch
            {
                return default(TOut);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
}

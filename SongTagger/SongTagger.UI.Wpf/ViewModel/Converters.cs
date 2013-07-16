using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SongTagger.UI.Wpf
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Drawing.Color col = (System.Drawing.Color)value;
            Color c = Color.FromArgb(col.A, col.R, col.G, col.B);
            return new System.Windows.Media.SolidColorBrush(c);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush c = (SolidColorBrush)value;
            System.Drawing.Color col = System.Drawing.Color.FromArgb(c.Color.A, c.Color.R, c.Color.G, c.Color.B);
            return col;
        }
    }   

    public class BooleanInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringPaddingConverter : IValueConverter
    {
        public int MaxTextLengthIncPaddingString { get; set; }

        private string paddingString;
        public string PaddingString
        {
            get { return paddingString ?? (paddingString = "..."); }
            set { paddingString = value; }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value as String;
            if (String.IsNullOrWhiteSpace(text))
                return text;

            int length = text.Length;
            int paddingLength = PaddingString.Length;

            if (length <= MaxTextLengthIncPaddingString)
                return text;

            string substring = text.Substring(0, MaxTextLengthIncPaddingString - paddingLength);
            return substring + PaddingString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }

    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan span = value is TimeSpan ? (TimeSpan) value : new TimeSpan();
            return string.Format("{0:D2}:{1:D2}", span.Minutes, span.Seconds);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

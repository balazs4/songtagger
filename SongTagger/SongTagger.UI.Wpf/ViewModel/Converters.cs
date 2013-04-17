﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace SongTagger.UI.Wpf.ViewModel
{
    [ValueConversion(typeof(ObservableCollection<string>), typeof(string))]
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<string> list = value as ObservableCollection<string>;

            if (list == null)
                return String.Empty;

            if (list.Count == 0)
                return String.Empty;

            return list.LastOrDefault();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

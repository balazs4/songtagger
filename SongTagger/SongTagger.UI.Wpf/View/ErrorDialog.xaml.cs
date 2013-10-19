using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SongTagger.UI.Wpf.View
{
    /// <summary>
    /// Interaction logic for ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : Window
    {
        public ErrorDialog(string text)
        {
            InitializeComponent();
            DataContext = text;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfControlLibrary1.Windows.ViewModels;

namespace WpfControlLibrary1.Windows
{
    /// <summary>
    /// Interaction logic for DiggingHole.xaml
    /// </summary>
    public partial class DiggingHole : Window
    {
        public DiggingHole(DiggingHoleViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

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
    /// Interaction logic for AutoJoinDialog.xaml
    /// </summary>
    public partial class AutoJoinDialog : Window
    {
        public AutoJoinDialog(AutoJoinDialogViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}

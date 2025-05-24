// MainWindow.xaml.cs
using System.Windows;

namespace ApiManagerApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ApiManagerApp.ViewModels.MainViewModel();
        }
    }
}
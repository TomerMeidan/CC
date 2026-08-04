using EncoderHub.App.ViewModels;
using System.Windows;

namespace EncoderHub.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

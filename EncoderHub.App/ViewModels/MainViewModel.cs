namespace EncoderHub.App.ViewModels
{
    /// <summary>
    /// Root ViewModel injected into <see cref="MainWindow"/>.
    /// Exposes child ViewModels so XAML bindings can navigate down the hierarchy.
    /// </summary>
    public class MainViewModel
    {
        public DriverControlViewModel DriverControlVM { get; }

        public MainViewModel(DriverControlViewModel driverControlVM)
        {
            DriverControlVM = driverControlVM;
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EncoderHub.App.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels. Provides <see cref="INotifyPropertyChanged"/>
    /// and a helper that fires the event with the correct property name automatically.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

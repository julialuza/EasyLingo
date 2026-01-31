using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyLingo.Infrastructure
{
    public class BaseNotify : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

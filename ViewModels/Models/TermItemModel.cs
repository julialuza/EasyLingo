using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyLingo.ViewModels.Models
{
    public class TermItemModel : INotifyPropertyChanged
    {
        private int termId;
        private string termName;
        private string definition;

        public int TermId
        {
            get => termId;
            set { termId = value; OnPropertyChanged(); }
        }

        public string TermName
        {
            get => termName;
            set { termName = value; OnPropertyChanged(); }
        }

        public string Definition
        {
            get => definition;
            set { definition = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyLingo.ViewModels.Models
{
    public class SetCardModel : INotifyPropertyChanged
    {
        private string name;
        private int progressPercent;
        private int setId;
        public string? CategoryName { get; set; }

        public int SetId
        {
            get => setId;
            set { setId = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public int ProgressPercent
        {
            get => progressPercent;
            set
            {
                progressPercent = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

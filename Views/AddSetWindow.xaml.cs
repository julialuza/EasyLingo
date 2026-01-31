using System.Windows;
using EasyLingo.ViewModels;

namespace EasyLingo.Views
{
    public partial class AddSetWindow : Window
    {
        public AddSetWindow()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                if (DataContext is AddSetViewModel vm)
                {
                    vm.CloseRequested += () => Close();
                }
            };
        }
    }
}

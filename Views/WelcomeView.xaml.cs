using System.Windows.Controls;

namespace EasyLingo.Views
{
    public partial class WelcomeView : UserControl
    {
        public WelcomeView()
        {
            InitializeComponent();
        }

        // Eventy do kliknięcia przycisków
        public event System.Action? LoginClicked;
        public event System.Action? RegisterClicked;

        private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoginClicked?.Invoke();
        }

        private void SignInButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RegisterClicked?.Invoke();
        }
    }
}

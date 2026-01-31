using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EasyLingo.Views
{
    public partial class SidebarView : UserControl
    {
        private MainWindow _mainWindow;
        private Button _activeButton;

        private Brush DefaultColor =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF132735"));

        private Brush ActiveColor =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00697D"));

        public SidebarView()
        {
            InitializeComponent();
            SetActiveButton(DashboardButton);
        }

        public void SetMainWindow(MainWindow window)
        {
            _mainWindow = window;
        }

        private void SetActiveButton(Button button)
        {
            if (_activeButton != null)
                _activeButton.Background = DefaultColor;

            _activeButton = button;
            _activeButton.Background = ActiveColor;
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(DashboardButton);
            _mainWindow?.ShowDashboard();
        }

        private void SetsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SetsButton);
            _mainWindow?.ShowSets();
        }

        private void AchievementsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(AchievementsButton);
            _mainWindow?.ShowAchievements();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Czy na pewno chcesz się wylogować?",
                "Wylogowanie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _mainWindow?.Logout();
            }
        }
    }
}

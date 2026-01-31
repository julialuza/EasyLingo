using System.Windows;
using System.Windows.Controls;

namespace EasyLingo.Views
{
    public partial class TopBarView : UserControl
    {
        public TopBarView()
        {
            InitializeComponent();
        }

        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(
                nameof(UserName),
                typeof(string),
                typeof(TopBarView),
                new PropertyMetadata(""));

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (Window.GetWindow(this) is not EasyLingo.MainWindow mw) return;

            int langId = LanguageComboBox.SelectedIndex == 1 ? 2 : 1;

            mw.ChangeLanguage(langId);
        }

    }
}

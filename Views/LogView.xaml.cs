using EasyLingo.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace EasyLingo.Views
{
    public partial class LogView : UserControl
    {
        public event Action<int>? LoginSucceeded;
        public event Action? RegisterRequested;

        private bool _syncing;
        private string _passwordCache = "";

        public LogView()
        {
            InitializeComponent();

            var vm = new LogViewModel();
            vm.LoginSucceeded += id => LoginSucceeded?.Invoke(id);
            DataContext = vm;

            UpdatePasswordPlaceholder();
        }

        private void SetVmPassword(string value)
        {
            if (DataContext is LogViewModel vm)
            {
                vm.Password = value;
            }
        }

        private void UpdatePasswordPlaceholder()
        {
            bool hasText = !string.IsNullOrEmpty(_passwordCache);
            bool focused = PasswordBox.IsFocused || PasswordVisibleBox.IsFocused;

            PasswordPlaceholder.Visibility = (hasText || focused)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncing) return;

            _syncing = true;

            _passwordCache = PasswordBox.Password;
            SetVmPassword(_passwordCache);

            if (ShowPasswordToggle.IsChecked == true)
                PasswordVisibleBox.Text = _passwordCache;

            _syncing = false;

            UpdatePasswordPlaceholder();
        }

        private void PasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncing) return;

            _syncing = true;

            _passwordCache = PasswordVisibleBox.Text ?? "";
            SetVmPassword(_passwordCache);

            if (ShowPasswordToggle.IsChecked == true)
                PasswordBox.Password = _passwordCache;

            _syncing = false;

            UpdatePasswordPlaceholder();
        }

        private void ShowPasswordToggle_Checked(object sender, RoutedEventArgs e)
        {
            PasswordVisibleBox.Text = _passwordCache;

            PasswordVisibleBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;

            PasswordVisibleBox.Focus();
            PasswordVisibleBox.CaretIndex = PasswordVisibleBox.Text.Length;

            UpdatePasswordPlaceholder();
        }

        private void ShowPasswordToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Password = _passwordCache;

            PasswordVisibleBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;

            PasswordBox.Focus();

            UpdatePasswordPlaceholder();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogViewModel vm && vm.LoginCommand.CanExecute(null))
                vm.LoginCommand.Execute(null);
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterRequested?.Invoke();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdatePasswordPlaceholder();
        }

    }
}

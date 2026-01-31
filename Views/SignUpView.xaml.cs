using EasyLingo.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace EasyLingo.Views
{
    public partial class SignUpView : UserControl
    {
        public event Action? SignInSucceeded;

        private bool _syncing;

        private string _passwordCache = "";
        private string _confirmPasswordCache = "";

        public SignUpView()
        {
            InitializeComponent();

            var vm = new SignUpViewModel();
            vm.RegistrationSucceeded += () => SignInSucceeded?.Invoke();
            DataContext = vm;

            UpdatePlaceholders();
        }

        private void SetVmPasswords()
        {
            if (DataContext is SignUpViewModel vm)
            {
                vm.Password = _passwordCache;
                vm.ConfirmPassword = _confirmPasswordCache;
            }
        }

        private void UpdatePlaceholders()
        {
            // PASSWORD
            bool passHasText = !string.IsNullOrEmpty(_passwordCache);
            bool passFocused = PasswordBox.IsFocused || PasswordVisibleBox.IsFocused;

            PasswordPlaceholder.Visibility = (passHasText || passFocused)
                ? Visibility.Collapsed
                : Visibility.Visible;

            // CONFIRM
            bool confHasText = !string.IsNullOrEmpty(_confirmPasswordCache);
            bool confFocused = ConfirmPasswordBox.IsFocused || ConfirmPasswordVisibleBox.IsFocused;

            ConfirmPasswordPlaceholder.Visibility = (confHasText || confFocused)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;

            _passwordCache = PasswordBox.Password ?? "";
            SetVmPasswords();

            if (ShowPasswordToggle.IsChecked == true)
                PasswordVisibleBox.Text = _passwordCache;

            _syncing = false;
            UpdatePlaceholders();
        }

        private void PasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;

            _passwordCache = PasswordVisibleBox.Text ?? "";
            SetVmPasswords();

            if (ShowPasswordToggle.IsChecked == true)
                PasswordBox.Password = _passwordCache;

            _syncing = false;
            UpdatePlaceholders();
        }

        private void ShowPasswordToggle_Checked(object sender, RoutedEventArgs e)
        {
            PasswordVisibleBox.Text = _passwordCache;

            PasswordVisibleBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;

            PasswordVisibleBox.Focus();
            PasswordVisibleBox.CaretIndex = PasswordVisibleBox.Text.Length;

            UpdatePlaceholders();
        }

        private void ShowPasswordToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Password = _passwordCache;

            PasswordVisibleBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;

            PasswordBox.Focus();

            UpdatePlaceholders();
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;

            _confirmPasswordCache = ConfirmPasswordBox.Password ?? "";
            SetVmPasswords();

            if (ShowConfirmPasswordToggle.IsChecked == true)
                ConfirmPasswordVisibleBox.Text = _confirmPasswordCache;

            _syncing = false;
            UpdatePlaceholders();
        }

        private void ConfirmPasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;

            _confirmPasswordCache = ConfirmPasswordVisibleBox.Text ?? "";
            SetVmPasswords();

            if (ShowConfirmPasswordToggle.IsChecked == true)
                ConfirmPasswordBox.Password = _confirmPasswordCache;

            _syncing = false;
            UpdatePlaceholders();
        }

        private void ShowConfirmPasswordToggle_Checked(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordVisibleBox.Text = _confirmPasswordCache;

            ConfirmPasswordVisibleBox.Visibility = Visibility.Visible;
            ConfirmPasswordBox.Visibility = Visibility.Collapsed;

            ConfirmPasswordVisibleBox.Focus();
            ConfirmPasswordVisibleBox.CaretIndex = ConfirmPasswordVisibleBox.Text.Length;

            UpdatePlaceholders();
        }

        private void ShowConfirmPasswordToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordBox.Password = _confirmPasswordCache;

            ConfirmPasswordVisibleBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = Visibility.Visible;

            ConfirmPasswordBox.Focus();

            UpdatePlaceholders();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SignUpViewModel vm && vm.RegisterCommand.CanExecute(null))
                vm.RegisterCommand.Execute(null);
        }

        private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            SignInSucceeded?.Invoke();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdatePlaceholders();
        }
    }
}

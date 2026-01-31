using EasyLingo.Infrastructure.Commands;
using EasyLingo.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EasyLingo.Services;

namespace EasyLingo.ViewModels
{
    public class SignUpViewModel : INotifyPropertyChanged
    {
        private readonly DataService _dataService = new();

        private string _username = "";
        private string _password = "";
        private string _confirmPassword = "";

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            set => _password = value;
        }

        public string ConfirmPassword
        {
            set => _confirmPassword = value;
        }

        public ICommand RegisterCommand { get; }

        public event Action? RegistrationSucceeded;
        public event PropertyChangedEventHandler? PropertyChanged;

        public SignUpViewModel()
        {
            RegisterCommand = new RelayCommand(async _ => await RegisterAsync());
        }

        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(_password) ||
                string.IsNullOrWhiteSpace(_confirmPassword))
            {
                MessageBox.Show("Wypełnij wszystkie pola");
                return;
            }

            if (_password.Length < 6)
            {
                MessageBox.Show("Hasło musi mieć co najmniej 6 znaków");
                return;
            }

            if (_password != _confirmPassword)
            {
                MessageBox.Show("Hasła nie są takie same");
                return;
            }

            var existingUser = await _dataService.GetUserByUsernameAsync(Username);
            if (existingUser != null)
            {
                MessageBox.Show("Użytkownik o takiej nazwie już istnieje");
                return;
            }

            var hash = PasswordHasher.HashPassword(_password);
            await _dataService.AddUserAsync(Username, hash);

            MessageBox.Show("Rejestracja zakończona pomyślnie 🎉");
            RegistrationSucceeded?.Invoke();
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using EasyLingo.Infrastructure.Commands;
using EasyLingo.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EasyLingo.ViewModels
{
    public class LogViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService = new();

        private string _username = "";
        private string _password = "";

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            private get => _password;
            set { _password = value; }
        }

        public ICommand LoginCommand { get; }

        public event Action<int>? LoginSucceeded;
        public event PropertyChangedEventHandler? PropertyChanged;

        public LogViewModel()
        {
            LoginCommand = new RelayCommand(async _ => await LoginAsync());
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Podaj nazwę użytkownika i hasło");
                return;
            }

            var user = await _authService.LoginAsync(Username, Password);
            if (user == null)
            {
                MessageBox.Show("Nieprawidłowa nazwa użytkownika lub hasło");
                return;
            }

            LoginSucceeded?.Invoke(user.UserId);
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

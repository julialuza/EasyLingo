using System;
using System.Windows.Input;
using EasyLingo.Infrastructure.Commands;

namespace EasyLingo.ViewModels
{
    public class WelcomeViewModel
    {
        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public WelcomeViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
            RegisterCommand = new RelayCommand(ExecuteRegister);
        }

        private void ExecuteLogin(object? parameter)
        {
            Console.WriteLine("Przejście do ekranu logowania");
        }

        private void ExecuteRegister(object? parameter)
        {
            Console.WriteLine("Przejście do ekranu rejestracji");
        }
    }
}

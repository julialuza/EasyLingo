using EasyLingo.Infrastructure;
using EasyLingo.Infrastructure.Commands;
using EasyLingo.Services.Interfaces;
using System;

namespace EasyLingo.ViewModels.StudyModes
{
    public abstract class StudyModeViewModelBase : BaseNotify
    {
        protected readonly IDataService Data;

        public event Action? BackRequested;

        public int UserId { get; }
        public int SetId { get; }

        public RelayCommand BackCommand { get; }

        protected StudyModeViewModelBase(IDataService data, int userId, int setId)
        {
            Data = data;
            UserId = userId;
            SetId = setId;

            BackCommand = new RelayCommand(_ => BackRequested?.Invoke());
        }
        protected void RequestBack()
        => BackRequested?.Invoke();

        public abstract string Title { get; }
        public abstract void Restart();
    }
}

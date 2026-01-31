using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using EasyLingo.Data.Entities;
using EasyLingo.Infrastructure.Commands;
using EasyLingo.Services;
using EasyLingo.ViewModels.Models;
using EasyLingo.Views;

namespace EasyLingo.ViewModels
{
    public class SetDetailsViewModel : INotifyPropertyChanged
    {
        private readonly DataService _dataService = new DataService();

        public int UserId { get; }
        public int SetId { get; }

        public event Action BackRequested;
        public event Action OpenFlashcardsRequested;
        public event Action OpenMultipleChoiceRequested;
        public event Action OpenMatchRequested;
        public event Action OpenTypingRequested;

        private string setName;
        public string SetName { get => setName; set { setName = value; OnPropertyChanged(); } }

        private string setDescription;
        public string SetDescription { get => setDescription; set { setDescription = value; OnPropertyChanged(); } }

        private int progressPercent;
        public int ProgressPercent { get => progressPercent; set { progressPercent = value; OnPropertyChanged(); } }

        public ObservableCollection<TermItemModel> Terms { get; } = new();

        private string newTermName;
        public string NewTermName { get => newTermName; set { newTermName = value; OnPropertyChanged(); } }

        private string newDefinition;
        public string NewDefinition { get => newDefinition; set { newDefinition = value; OnPropertyChanged(); } }

        private TermItemModel selectedTerm;
        public TermItemModel SelectedTerm
        {
            get => selectedTerm;
            set
            {
                selectedTerm = value;
                OnPropertyChanged();

                if (selectedTerm != null)
                {
                    EditTermName = selectedTerm.TermName;
                    EditDefinition = selectedTerm.Definition;
                }
            }
        }

        private string editTermName;
        public string EditTermName { get => editTermName; set { editTermName = value; OnPropertyChanged(); } }

        private string editDefinition;
        public string EditDefinition { get => editDefinition; set { editDefinition = value; OnPropertyChanged(); } }

        public RelayCommand BackCommand { get; }

        // ✅ ODŚWIEŻ = reset postępów
        public RelayCommand ReloadCommand { get; }

        public RelayCommand AddTermCommand { get; }
        public RelayCommand DeleteTermCommand { get; }
        public RelayCommand SaveEditCommand { get; }
        public RelayCommand ClearEditCommand { get; }

        public RelayCommand OpenFlashcardsCommand { get; }
        public RelayCommand OpenMultipleChoiceCommand { get; }
        public RelayCommand OpenMatchCommand { get; }
        public RelayCommand OpenTypingCommand { get; }

        // ✅ nowe
        public RelayCommand EditSetCommand { get; }
        public RelayCommand DeleteSetCommand { get; }

        public SetDetailsViewModel(int userId, int setId)
        {
            UserId = userId;
            SetId = setId;

            BackCommand = new RelayCommand(_ => BackRequested?.Invoke());

            ReloadCommand = new RelayCommand(async _ => await ResetProgressAsync());

            AddTermCommand = new RelayCommand(async _ => await AddTermAsync(), _ => CanAddTerm());
            DeleteTermCommand = new RelayCommand(async p => await DeleteTermAsync(p as TermItemModel), p => p is TermItemModel);
            SaveEditCommand = new RelayCommand(async _ => await SaveEditAsync(), _ => CanSaveEdit());
            ClearEditCommand = new RelayCommand(_ => ClearEdit());

            OpenFlashcardsCommand = new RelayCommand(_ => OpenFlashcardsRequested?.Invoke());
            OpenMultipleChoiceCommand = new RelayCommand(_ => OpenMultipleChoiceRequested?.Invoke());
            OpenMatchCommand = new RelayCommand(_ => OpenMatchRequested?.Invoke());
            OpenTypingCommand = new RelayCommand(_ => OpenTypingRequested?.Invoke());

            EditSetCommand = new RelayCommand(async _ => await EditSetAsync());
            DeleteSetCommand = new RelayCommand(async _ => await DeleteSetAsync());

            _ = LoadAsync();
        }

        private bool CanAddTerm()
            => !string.IsNullOrWhiteSpace(NewTermName) && !string.IsNullOrWhiteSpace(NewDefinition);

        private bool CanSaveEdit()
            => SelectedTerm != null
               && !string.IsNullOrWhiteSpace(EditTermName)
               && !string.IsNullOrWhiteSpace(EditDefinition);

        private async Task LoadAsync()
        {
            var set = await _dataService.GetSetByIdAsync(SetId);
            SetName = set?.Name ?? "Zestaw";
            SetDescription = set?.Description ?? "";

            var terms = await _dataService.GetTermsBySetAsync(SetId);
            Terms.Clear();
            foreach (var t in terms)
            {
                Terms.Add(new TermItemModel
                {
                    TermId = t.TermId,
                    TermName = t.TermName,
                    Definition = t.Definition
                });
            }

            var prog = await _dataService.GetProgressAsync(UserId, SetId);
            ProgressPercent = prog?.ProgressPercent ?? 0;
        }

        // ✅ RESET POSTĘPÓW
        private async Task ResetProgressAsync()
        {
            // potwierdzenie
            var res = MessageBox.Show(
                "Na pewno zresetować postęp tego zestawu? (0% i wszystkie słówka jako nieznane)",
                "Reset postępów",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res != MessageBoxResult.Yes) return;

            await _dataService.ResetSetProgressAsync(UserId, SetId);
            await LoadAsync(); // odśwież pasek + listę
        }

        // ✅ EDYCJA SETU (otwiera AddSetWindow z wypełnionymi polami)
        private async Task EditSetAsync()
        {
            var set = await _dataService.GetSetByIdAsync(SetId);
            if (set == null)
            {
                MessageBox.Show("Nie znaleziono zestawu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (set.UserId != UserId)
            {
                MessageBox.Show("Brak uprawnień do edycji tego zestawu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string? catName = set.SetCategory?.Name;

            var vm = new AddSetViewModel(UserId, set.LangId, set.SetId, set.Name, set.Description ?? "", catName);

            var win = new AddSetWindow
            {
                Owner = Application.Current?.MainWindow,
                DataContext = vm
            };

            vm.CloseRequested += () => win.Close();
            vm.Saved += async () =>
            {
                await LoadAsync();
            };

            win.ShowDialog();
        }

        // ✅ USUŃ SET
        private async Task DeleteSetAsync()
        {
            var res = MessageBox.Show(
                "Na pewno usunąć ten zestaw? Usunie też słówka i postęp.",
                "Usuń zestaw",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res != MessageBoxResult.Yes) return;

            var set = await _dataService.GetSetByIdAsync(SetId);
            if (set == null)
            {
                MessageBox.Show("Nie znaleziono zestawu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (set.UserId != UserId)
            {
                MessageBox.Show("Brak uprawnień do usunięcia tego zestawu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _dataService.DeleteSetAsync(set);
            BackRequested?.Invoke(); // wróć do listy
        }

        private async Task AddTermAsync()
        {
            var term = await _dataService.AddTermAsync(NewTermName.Trim(), NewDefinition.Trim(), SetId);

            Terms.Add(new TermItemModel
            {
                TermId = term.TermId,
                TermName = term.TermName,
                Definition = term.Definition
            });

            NewTermName = "";
            NewDefinition = "";

            await _dataService.RecalculateProgressAsync(UserId, SetId);
            await RefreshProgressOnlyAsync();
        }

        private async Task DeleteTermAsync(TermItemModel item)
        {
            if (item == null) return;

            var term = await _dataService.GetTermByIdAsync(item.TermId);
            if (term == null) return;

            await _dataService.DeleteTermAsync(term);

            Terms.Remove(item);

            if (SelectedTerm?.TermId == item.TermId)
                ClearEdit();

            await _dataService.RecalculateProgressAsync(UserId, SetId);
            await RefreshProgressOnlyAsync();
        }

        private async Task SaveEditAsync()
        {
            if (SelectedTerm == null) return;

            var term = await _dataService.GetTermByIdAsync(SelectedTerm.TermId);
            if (term == null) return;

            term.TermName = EditTermName.Trim();
            term.Definition = EditDefinition.Trim();

            await _dataService.UpdateTermAsync(term);

            SelectedTerm.TermName = term.TermName;
            SelectedTerm.Definition = term.Definition;

            await _dataService.RecalculateProgressAsync(UserId, SetId);
            await RefreshProgressOnlyAsync();
        }

        private async Task RefreshProgressOnlyAsync()
        {
            var prog = await _dataService.GetProgressAsync(UserId, SetId);
            ProgressPercent = prog?.ProgressPercent ?? 0;
        }

        private void ClearEdit()
        {
            SelectedTerm = null;
            EditTermName = "";
            EditDefinition = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

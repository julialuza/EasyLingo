using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EasyLingo.Infrastructure.Commands;
using EasyLingo.Services;
using EasyLingo.ViewModels.Models;
using EasyLingo.Views;
using Microsoft.Win32;

namespace EasyLingo.ViewModels
{
    public class CategoryItemModel
    {
        public int? CategoryId { get; set; }
        public string Name { get; set; } = "";
    }

    public class SetsViewModel : INotifyPropertyChanged
    {
        private readonly DataService _dataService;
        private readonly SetJsonPortService _jsonPort;

        public int UserId { get; }

        public RelayCommand ImportJsonCommand { get; }
        public RelayCommand ExportJsonCommand { get; }

        public RelayCommand SelectSetCommand { get; }

        private int _langId = 1;
        public int LangId
        {
            get => _langId;
            private set { _langId = value; OnPropertyChanged(); }
        }

        private int? _selectedSetId;
        public int? SelectedSetId
        {
            get => _selectedSetId;
            private set
            {
                _selectedSetId = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<CategoryItemModel> Categories { get; } = new();

        private CategoryItemModel? _selectedCategory;
        public CategoryItemModel? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value) return;
                _selectedCategory = value;
                OnPropertyChanged();
                _ = LoadSetsAsync();
            }
        }

        public ObservableCollection<SetCardModel> Sets { get; } = new();

        public event Action<int>? OpenSetRequested;

        public RelayCommand OpenSetCommand { get; }
        public RelayCommand AddSetCommand { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _errorText = "";
        public string ErrorText
        {
            get => _errorText;
            private set { _errorText = value; OnPropertyChanged(); }
        }

        public SetsViewModel(int currentUserId)
        {
            UserId = currentUserId;

            _dataService = new DataService();
            _jsonPort = new SetJsonPortService();

            OpenSetCommand = new RelayCommand(p =>
            {
                if (p is int setId)
                    OpenSetRequested?.Invoke(setId);
            });

            SelectSetCommand = new RelayCommand(p =>
            {
                if (p is int setId)
                    SelectedSetId = setId;
            });

            AddSetCommand = new RelayCommand(_ => OpenAddSetDialog());

            ExportJsonCommand = new RelayCommand(async _ => await ExportAsync(),
                _ => !IsBusy && SelectedSetId != null);

            ImportJsonCommand = new RelayCommand(async _ => await ImportAsync(),
                _ => !IsBusy);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
            await LoadSetsAsync();
        }

        public async Task ReloadAsync()
        {
            await LoadCategoriesAsync(keepSelection: true);
            await LoadSetsAsync();
        }

        public async Task SetLanguageAsync(int langId)
        {
            LangId = langId;
            SelectedSetId = null;
            await LoadCategoriesAsync(keepSelection: false);
            await LoadSetsAsync();
        }

        private async Task LoadCategoriesAsync(bool keepSelection = true)
        {
            try
            {
                ErrorText = "";
                IsBusy = true;

                int? previouslySelectedId = keepSelection ? SelectedCategory?.CategoryId : null;

                Categories.Clear();
                Categories.Add(new CategoryItemModel { CategoryId = null, Name = "Wszystkie" });

                var userSets = await _dataService.GetUserSetProgressCardsByLangAsync(UserId, LangId, categoryId: null);

                var usedCategoryNames = userSets
                    .Select(s => s.CategoryName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)
                    .ToList();

                var cats = await _dataService.GetSetCategoriesByUserAsync(UserId);

                foreach (var c in cats
                    .Where(c => usedCategoryNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                    .OrderBy(c => c.Name))
                {
                    Categories.Add(new CategoryItemModel
                    {
                        CategoryId = c.SetCategoryId,
                        Name = c.Name
                    });
                }

                if (previouslySelectedId != null)
                {
                    SelectedCategory = Categories.FirstOrDefault(x => x.CategoryId == previouslySelectedId)
                                      ?? Categories.First();
                }
                else
                {
                    SelectedCategory = Categories.First();
                }
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
                Categories.Clear();
                Categories.Add(new CategoryItemModel { CategoryId = null, Name = "Wszystkie" });
                SelectedCategory = Categories.First();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSetsAsync()
        {
            try
            {
                ErrorText = "";
                IsBusy = true;

                int? categoryId = SelectedCategory?.CategoryId;

                var userSets = await _dataService.GetUserSetProgressCardsByLangAsync(UserId, LangId, categoryId);

                Sets.Clear();
                foreach (var s in userSets)
                {
                    Sets.Add(new SetCardModel
                    {
                        SetId = s.SetId,
                        Name = s.Name,
                        ProgressPercent = s.ProgressPercent,
                        CategoryName = s.CategoryName
                    });
                }

                if (SelectedSetId != null && !Sets.Any(x => x.SetId == SelectedSetId.Value))
                    SelectedSetId = null;
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OpenAddSetDialog()
        {
            var owner = Application.Current?.MainWindow;

            var vm = new AddSetViewModel(UserId, LangId);

            var win = new AddSetWindow
            {
                Owner = owner,
                DataContext = vm
            };

            vm.CloseRequested += () => win.Close();

            vm.Saved += async () =>
            {
                await ReloadAsync();
            };

            win.ShowDialog();
        }

        private async Task ExportAsync()
        {
            ErrorText = "";

            if (SelectedSetId == null)
            {
                ErrorText = "Kliknij PPM na zestaw, aby go zaznaczyć do eksportu.";
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                DefaultExt = ".json",
                FileName = $"set_{SelectedSetId.Value}.json"
            };

            if (dlg.ShowDialog() != true) return;

            IsBusy = true;
            try
            {
                await _jsonPort.ExportSetToJsonFileAsync(UserId, SelectedSetId.Value, dlg.FileName);
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ImportAsync()
        {
            ErrorText = "";

            var dlg = new OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dlg.ShowDialog() != true) return;

            IsBusy = true;
            try
            {
                await _jsonPort.ImportSetFromJsonFileAsync(UserId, LangId, dlg.FileName);

                await ReloadAsync();
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

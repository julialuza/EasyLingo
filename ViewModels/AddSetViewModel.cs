using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EasyLingo.Infrastructure;
using EasyLingo.Infrastructure.Commands;
using EasyLingo.Services;

namespace EasyLingo.ViewModels
{
    public class AddSetViewModel : BaseNotify
    {
        private readonly DataService _data = new DataService();

        public int UserId { get; }
        public int LangId { get; }

        public int? EditingSetId { get; }
        public bool IsEditMode => EditingSetId != null;

        public event Action? CloseRequested;

        public event Action? Saved;

        // ====== Pola formularza ======

        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private string _categoryName = "";
        public string CategoryName
        {
            get => _categoryName;
            set { _categoryName = value; OnPropertyChanged(); }
        }

        private string _errorText = "";
        public string ErrorText
        {
            get => _errorText;
            set { _errorText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> CategorySuggestions { get; } = new();

        // ====== Komendy ======
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            private set { _isSaving = value; OnPropertyChanged(); }
        }

        public AddSetViewModel(int userId, int langId)
        {
            UserId = userId;
            LangId = langId;

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke());

            _ = LoadCategorySuggestionsAsync();
        }

        public AddSetViewModel(int userId, int langId, int editingSetId, string name, string description, string? categoryName)
            : this(userId, langId)
        {
            EditingSetId = editingSetId;
            Name = name ?? "";
            Description = description ?? "";
            CategoryName = categoryName ?? "";
        }

        private async Task LoadCategorySuggestionsAsync()
        {
            try
            {
                CategorySuggestions.Clear();

                var cats = await _data.GetSetCategoriesByUserAsync(UserId);
                foreach (var c in cats.Select(x => x.Name).Distinct().OrderBy(x => x))
                    CategorySuggestions.Add(c);
            }
            catch
            {
            }
        }

        private async Task SaveAsync()
        {
            ErrorText = "";

            var n = (Name ?? "").Trim();
            var d = (Description ?? "").Trim();
            var cat = (CategoryName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(n))
            {
                ErrorText = "Podaj nazwę zestawu.";
                return;
            }

            IsSaving = true;
            try
            {
                if (!IsEditMode)
                {
                    await _data.AddSetAsync(UserId, n, d, LangId, string.IsNullOrWhiteSpace(cat) ? null : cat);
                }
                else
                {
                    var set = await _data.GetSetByIdAsync(EditingSetId!.Value);
                    if (set == null)
                        throw new Exception("Nie znaleziono zestawu do edycji.");

                    if (set.UserId != UserId)
                        throw new Exception("Brak uprawnień do edycji tego zestawu.");

                    set.Name = n;
                    set.Description = d;

                    set.SetCategoryId = await _data.GetOrCreateSetCategoryIdAsync(UserId,
                        string.IsNullOrWhiteSpace(cat) ? null : cat);

                    await _data.UpdateSetAsync(set);
                }

                Saved?.Invoke();
                CloseRequested?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }

    }
}

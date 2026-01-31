using EasyLingo.Services;
using EasyLingo.ViewModels.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EasyLingo.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DataService _dataService;
        private readonly int _userId;

        private int _langId = 1;

        public ObservableCollection<SetCardModel> ContinueSets { get; } = new();

        private string _welcomeText = "Witaj!";
        public string WelcomeText { get => _welcomeText; private set { _welcomeText = value; OnPropertyChanged(); } }

        private string _subtitleText = "Kontynuuj naukę i śledź swoje postępy.";
        public string SubtitleText { get => _subtitleText; private set { _subtitleText = value; OnPropertyChanged(); } }

        private string _yourProgressTitle = "Twoje postępy";
        public string YourProgressTitle { get => _yourProgressTitle; private set { _yourProgressTitle = value; OnPropertyChanged(); } }

        private string _completedSetsTitle = "Ukończone zestawy";
        public string CompletedSetsTitle { get => _completedSetsTitle; private set { _completedSetsTitle = value; OnPropertyChanged(); } }

        private string _continueTitle = "Kontynuuj naukę zestawów";
        public string ContinueTitle { get => _continueTitle; private set { _continueTitle = value; OnPropertyChanged(); } }

        private int _overallProgressPercent;
        public int OverallProgressPercent { get => _overallProgressPercent; private set { _overallProgressPercent = value; OnPropertyChanged(); } }

        private string _completedSetsText = "0/0";
        public string CompletedSetsText { get => _completedSetsText; private set { _completedSetsText = value; OnPropertyChanged(); } }

        public DashboardViewModel(int userId)
        {
            _userId = userId;
            _dataService = new DataService();

            _ = ReloadAsync();
        }

        public async Task SetLanguageAsync(int langId)
        {
            _langId = langId;
            await ReloadAsync();
        }

        public async Task ReloadAsync()
        {
            var user = await _dataService.GetUserByIdAsync(_userId);
            var username = user?.Username ?? "user";

            ApplyUiLanguage(username);

            var stats = await _dataService.GetDashboardStatsAsync(_userId, _langId);
            OverallProgressPercent = stats.OverallProgressPercent;
            CompletedSetsText = $"{stats.CompletedSets}/{stats.TotalSets}";

            var continueSets = await _dataService.GetContinueSetsForDashboardAsync(_userId, _langId, 3);

            ContinueSets.Clear();
            foreach (var s in continueSets)
            {
                ContinueSets.Add(new SetCardModel
                {
                    SetId = s.SetId,
                    Name = s.Name,
                    ProgressPercent = s.ProgressPercent
                });
            }
        }

        private void ApplyUiLanguage(string username)
        {
            if (_langId == 2)
            {
                WelcomeText = $"Hallo, {username}!";
                SubtitleText = "Lerne weiter und verfolge deine Fortschritte.";
                YourProgressTitle = "Dein Fortschritt";
                CompletedSetsTitle = "Abgeschlossene Sets";
                ContinueTitle = "Lerne weiter – Sets";
            }
            else // domyślnie EN
            {
                WelcomeText = $"Hello, {username}!";
                SubtitleText = "Keep learning and track your progress.";
                YourProgressTitle = "Your progress";
                CompletedSetsTitle = "Completed sets";
                ContinueTitle = "Continue learning sets";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EasyLingo.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyLingo.ViewModels
{
    public class AchievementsViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private readonly int _userId;
        private int _langId;

        public ObservableCollection<AchievementItem> Achievements { get; } = new();

        private string _completedSetsText = "0/0";
        public string CompletedSetsText
        {
            get => _completedSetsText;
            private set { _completedSetsText = value; OnPropertyChanged(); }
        }

        private int _overallProgressPercent;
        public int OverallProgressPercent
        {
            get => _overallProgressPercent;
            private set { _overallProgressPercent = value; OnPropertyChanged(); }
        }

        private int _completedSets;
        public int CompletedSets
        {
            get => _completedSets;
            private set { _completedSets = value; OnPropertyChanged(); }
        }

        private int _totalSets;
        public int TotalSets
        {
            get => _totalSets;
            private set { _totalSets = value; OnPropertyChanged(); }
        }

        public AchievementsViewModel(AppDbContext db, int userId, int langId)
        {
            _db = db;
            _userId = userId;
            _langId = langId;

            _ = LoadAsync();
        }

        public async Task SetLanguageAsync(int langId)
        {
            _langId = langId;
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                Achievements.Clear();

                var sets = await _db.Sets
                    .AsNoTracking()
                    .Where(s => s.LangId == _langId && s.UserId == _userId)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var setIds = sets.Select(s => s.SetId).ToList();

                TotalSets = sets.Count;

                var progresses = await _db.UserSetProgresses
                    .AsNoTracking()
                    .Where(p => p.UserId == _userId && setIds.Contains(p.SetId))
                    .ToListAsync();

                CompletedSets = progresses.Count(p => p.ProgressPercent >= 100);

                CompletedSetsText = $"{CompletedSets}/{TotalSets}";

                OverallProgressPercent = TotalSets == 0
                    ? 0
                    : (int)Math.Round(100.0 * CompletedSets / TotalSets);

                foreach (var set in sets)
                {
                    int percent = progresses.FirstOrDefault(p => p.SetId == set.SetId)?.ProgressPercent ?? 0;

                    Achievements.Add(new AchievementItem
                    {
                        Icon = percent >= 100 ? "🏆" : "📘",
                        Title = $"Zestaw: {set.Name}",
                        Description = percent >= 100
                            ? "Zestaw ukończony!"
                            : $"Postęp: {percent}%",
                        ProgressPercent = Math.Clamp(percent, 0, 100),
                        StatusText = percent >= 100 ? "UNLOCKED" : "LOCKED"
                    });
                }

                AddCompletedSetsMilestone("Pierwszy ukończony zestaw", "Ukończ 1 zestaw", 1);
                AddCompletedSetsMilestone("2 ukończone zestawy", "Ukończ 2 zestawy", 2);
                AddCompletedSetsMilestone("Wszystkie zestawy", "Ukończ wszystkie zestawy", TotalSets);
            }
            catch (Exception ex)
            {
                Achievements.Clear();
                Achievements.Add(new AchievementItem
                {
                    Icon = "⚠️",
                    Title = "Błąd ładowania osiągnięć",
                    Description = ex.Message,
                    ProgressPercent = 0,
                    StatusText = "ERROR"
                });
            }
        }

        private void AddCompletedSetsMilestone(string title, string description, int requiredSets)
        {
            if (requiredSets <= 0) requiredSets = 1;

            int capped = Math.Min(CompletedSets, requiredSets);
            int progress = (int)Math.Round(100.0 * capped / requiredSets);
            bool unlocked = CompletedSets >= requiredSets;

            Achievements.Add(new AchievementItem
            {
                Icon = unlocked ? "🏁" : "🔒",
                Title = title,
                Description = $"{description} ({CompletedSets}/{requiredSets})",
                ProgressPercent = Math.Clamp(progress, 0, 100),
                StatusText = unlocked ? "UNLOCKED" : "LOCKED"
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class AchievementItem
    {
        public string Icon { get; set; } = "🏅";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int ProgressPercent { get; set; } = 0;
        public string StatusText { get; set; } = "LOCKED";
    }
}

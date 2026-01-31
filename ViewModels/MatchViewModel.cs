using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EasyLingo.Infrastructure;
using EasyLingo.Infrastructure.Commands;
using EasyLingo.Infrastructure.StudyModes;
using EasyLingo.Services.Interfaces;
using EasyLingo.ViewModels.StudyModes;

namespace EasyLingo.ViewModels
{
    public class MatchItem : BaseNotify
    {
        private bool _isMatched;

        public int TermId { get; set; }
        public string Text { get; set; } = "";
        public string PairKey { get; set; } = "";

        public bool IsMatched
        {
            get => _isMatched;
            set { _isMatched = value; OnPropertyChanged(); }
        }
    }

    [StudyMode("match", "Dopasuj pary")]
    public class MatchViewModel : StudyModeViewModelBase
    {
        private readonly Random _rng = new Random();

        public override string Title => "Dopasuj pary";

        public ObservableCollection<MatchItem> LeftItems { get; } = new();
        public ObservableCollection<MatchItem> RightItems { get; } = new();

        private MatchItem? _selectedLeft;
        public MatchItem? SelectedLeft
        {
            get => _selectedLeft;
            set { _selectedLeft = value; OnPropertyChanged(); _ = TryResolveAsync(); }
        }

        private MatchItem? _selectedRight;
        public MatchItem? SelectedRight
        {
            get => _selectedRight;
            set { _selectedRight = value; OnPropertyChanged(); _ = TryResolveAsync(); }
        }

        private string _feedback = "";
        public string Feedback
        {
            get => _feedback;
            private set { _feedback = value; OnPropertyChanged(); }
        }

        private int _matchedCount;
        public int MatchedCount
        {
            get => _matchedCount;
            private set { _matchedCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(MatchedText)); }
        }

        private int _roundSize;
        public int RoundSize
        {
            get => _roundSize;
            private set { _roundSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(MatchedText)); }
        }

        public string MatchedText => RoundSize == 0 ? "" : $"{MatchedCount}/{RoundSize} dopasowane";

        public RelayCommand NewRoundCommand { get; }

        private List<(int TermId, string TermName, string Definition)> _terms = new();

        private bool _isResolving;
        private bool _ignoreSelectionChange;

        public MatchViewModel(IDataService data, int userId, int setId)
            : base(data, userId, setId)
        {
            NewRoundCommand = new RelayCommand(_ => BuildRound(), _ => _terms.Count >= 2);
            _ = LoadAsync();
        }

        public override void Restart()
        {
            if (_terms.Count >= 2)
                BuildRound();
        }

        private async Task LoadAsync()
        {
            var terms = await Data.GetTermsBySetAsync(SetId);
            _terms = terms
                .Select(t => (t.TermId, t.TermName, t.Definition))
                .Where(x => !string.IsNullOrWhiteSpace(x.TermName) && !string.IsNullOrWhiteSpace(x.Definition))
                .ToList();

            if (_terms.Count < 2)
            {
                Feedback = "Dodaj więcej słówek (min. 2), żeby gra w pary miała sens";
                RoundSize = 0;
                MatchedCount = 0;
                LeftItems.Clear();
                RightItems.Clear();
                return;
            }

            BuildRound();
        }

        private void BuildRound()
        {
            Feedback = "";
            MatchedCount = 0;

            _ignoreSelectionChange = true;
            SelectedLeft = null;
            SelectedRight = null;
            _ignoreSelectionChange = false;

            LeftItems.Clear();
            RightItems.Clear();

            var round = _terms
                .OrderBy(_ => _rng.Next())
                .Take(Math.Min(6, _terms.Count))
                .ToList();

            RoundSize = round.Count;

            foreach (var t in round)
            {
                var key = $"T{t.TermId}";

                LeftItems.Add(new MatchItem { TermId = t.TermId, Text = t.TermName, PairKey = key });
                RightItems.Add(new MatchItem { TermId = t.TermId, Text = t.Definition, PairKey = key });
            }

            var shuffledRight = RightItems.OrderBy(_ => _rng.Next()).ToList();
            RightItems.Clear();
            foreach (var it in shuffledRight) RightItems.Add(it);
        }

        private async Task TryResolveAsync()
        {
            if (_ignoreSelectionChange) return;
            if (_isResolving) return;
            if (SelectedLeft == null || SelectedRight == null) return;

            if (SelectedLeft.IsMatched || SelectedRight.IsMatched)
            {
                Feedback = "Ta para jest już dopasowana";
                ClearSelections();
                return;
            }

            _isResolving = true;

            try
            {
                if (SelectedLeft.PairKey == SelectedRight.PairKey)
                {
                    SelectedLeft.IsMatched = true;
                    SelectedRight.IsMatched = true;

                    MatchedCount++;
                    Feedback = "✅ Dobrze!";

                    await Data.UpdateStatusAsync(UserId, SelectedLeft.TermId, 1);
                    await Data.RecalculateProgressAsync(UserId, SetId);

                    ClearSelections();

                    if (MatchedCount >= RoundSize)
                        Feedback = "Wszystkie pary dopasowane! Kliknij „Nowa runda”.";
                }
                else
                {
                    Feedback = "❌ Nie pasuje. Spróbuj jeszcze raz.";
                    ClearSelections();
                }
            }
            finally
            {
                _isResolving = false;
            }
        }

        private void ClearSelections()
        {
            _ignoreSelectionChange = true;
            SelectedLeft = null;
            SelectedRight = null;
            _ignoreSelectionChange = false;
        }
    }
}

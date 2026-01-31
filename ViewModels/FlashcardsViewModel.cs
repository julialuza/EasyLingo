using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyLingo.Infrastructure.Commands;
using EasyLingo.Infrastructure.StudyModes;
using EasyLingo.Services.Interfaces;
using EasyLingo.ViewModels.StudyModes;

namespace EasyLingo.ViewModels
{
    public class FlashcardItem
    {
        public int TermId { get; set; }
        public string Front { get; set; } = "";
        public string Back { get; set; } = "";
    }

    [StudyMode("flashcards", "Fiszki")]
    public class FlashcardsViewModel : StudyModeViewModelBase
    {
        private readonly Random _rng = new Random();

        public override string Title => "Fiszki";

        // --- UI state ---
        private bool _isFlipped;
        public bool IsFlipped
        {
            get => _isFlipped;
            set { _isFlipped = value; OnPropertyChanged(); OnPropertyChanged(nameof(FlipButtonText)); }
        }

        private bool _isRoundFinished;
        public bool IsRoundFinished
        {
            get => _isRoundFinished;
            private set { _isRoundFinished = value; OnPropertyChanged(); }
        }

        private string _frontText = "";
        public string FrontText { get => _frontText; private set { _frontText = value; OnPropertyChanged(); } }

        private string _backText = "";
        public string BackText { get => _backText; private set { _backText = value; OnPropertyChanged(); } }

        private string _statusText = "";
        public string StatusText { get => _statusText; private set { _statusText = value; OnPropertyChanged(); } }

        public string FlipButtonText => IsFlipped ? "UKRYJ" : "POKAŻ ODPOWIEDŹ";

        // --- counters ---
        private int _knownCount;
        public int KnownCount { get => _knownCount; private set { _knownCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(CountersText)); } }

        private int _unknownCount;
        public int UnknownCount { get => _unknownCount; private set { _unknownCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(CountersText)); } }

        private int _remainingCount;
        public int RemainingCount { get => _remainingCount; private set { _remainingCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(CountersText)); } }

        public string CountersText => $"Znam: {KnownCount}   |   Nie znam: {UnknownCount}   |   Zostało: {RemainingCount}";

        // --- commands ---
        public RelayCommand FlipCommand { get; }
        public RelayCommand KnowCommand { get; }
        public RelayCommand DontKnowCommand { get; }

        public RelayCommand RepeatUnknownCommand { get; }
        public RelayCommand RepeatAllCommand { get; }

        // --- data ---
        private List<FlashcardItem> _all = new();
        private Queue<FlashcardItem> _queue = new();

        private HashSet<int> _knownThisRound = new();
        private HashSet<int> _unknownThisRound = new();

        private FlashcardItem? _current;

        public FlashcardsViewModel(IDataService data, int userId, int setId)
            : base(data, userId, setId)
        {
            FlipCommand = new RelayCommand(_ =>
            {
                if (_current == null) return;
                IsFlipped = !IsFlipped;
            });

            KnowCommand = new RelayCommand(async _ => await MarkAsync(known: true), _ => CanAnswer());
            DontKnowCommand = new RelayCommand(async _ => await MarkAsync(known: false), _ => CanAnswer());

            RepeatUnknownCommand = new RelayCommand(_ => StartRepeatUnknown(), _ => IsRoundFinished && _unknownThisRound.Count > 0);
            RepeatAllCommand = new RelayCommand(async _ => await StartRepeatAllResetAsync(), _ => IsRoundFinished && _all.Count > 0);

            _ = LoadAsync();
        }

        public override void Restart()
        {
            if (_all.Count > 0)
                StartNewRound(_all);
        }

        private bool CanAnswer()
            => _current != null && IsFlipped == true && IsRoundFinished == false;

        private async Task LoadAsync()
        {
            var terms = await Data.GetTermsBySetAsync(SetId);
            _all = terms
                .Where(t => !string.IsNullOrWhiteSpace(t.TermName) && !string.IsNullOrWhiteSpace(t.Definition))
                .Select(t => new FlashcardItem
                {
                    TermId = t.TermId,
                    Front = t.TermName,
                    Back = t.Definition
                })
                .ToList();

            if (_all.Count == 0)
            {
                StatusText = "Dodaj słówka do zestawu, żeby korzystać z fiszek.";
                FrontText = "";
                BackText = "";
                IsRoundFinished = true;
                KnownCount = 0;
                UnknownCount = 0;
                RemainingCount = 0;
                return;
            }

            StartNewRound(_all);
        }

        private void StartNewRound(IEnumerable<FlashcardItem> items)
        {
            _knownThisRound = new HashSet<int>();
            _unknownThisRound = new HashSet<int>();

            var shuffled = items.OrderBy(_ => _rng.Next()).ToList();
            _queue = new Queue<FlashcardItem>(shuffled);

            IsRoundFinished = false;
            KnownCount = 0;
            UnknownCount = 0;
            RemainingCount = _queue.Count;

            StatusText = "Kliknij „POKAŻ ODPOWIEDŹ”, a potem wybierz ZNAM / NIE ZNAM.";
            NextCard();
        }

        private void NextCard()
        {
            IsFlipped = false;

            if (_queue.Count == 0)
            {
                FinishRound();
                return;
            }

            _current = _queue.Dequeue();
            FrontText = _current.Front;
            BackText = _current.Back;

            RemainingCount = _queue.Count + 1;
        }

        private void FinishRound()
        {
            _current = null;
            FrontText = "Koniec rundy";
            BackText = "";
            IsFlipped = false;

            RemainingCount = 0;
            IsRoundFinished = true;

            StatusText = _unknownThisRound.Count > 0
                ? "Możesz powtórzyć nieznane albo zresetować i powtórzyć wszystko od nowa."
                : "Super! Wszystko oznaczone jako „ZNAM”. Możesz zresetować i powtórzyć całość.";
        }

        private async Task MarkAsync(bool known)
        {
            if (_current == null) return;

            var termId = _current.TermId;

            if (known)
            {
                _knownThisRound.Add(termId);
                _unknownThisRound.Remove(termId);

                KnownCount = _knownThisRound.Count;
                UnknownCount = _unknownThisRound.Count;

                StatusText = "Zapisano jako: ZNAM";
                await Data.UpdateStatusAsync(UserId, termId, 1);
            }
            else
            {
                _unknownThisRound.Add(termId);
                _knownThisRound.Remove(termId);

                KnownCount = _knownThisRound.Count;
                UnknownCount = _unknownThisRound.Count;

                StatusText = "Zapisano jako: NIE ZNAM";
                await Data.UpdateStatusAsync(UserId, termId, 0);
            }

            await Data.RecalculateProgressAsync(UserId, SetId);
            NextCard();
        }

        private void StartRepeatUnknown()
        {
            var list = _all.Where(c => _unknownThisRound.Contains(c.TermId)).ToList();
            if (list.Count == 0)
            {
                StatusText = "Nie masz nieznanych słówek do powtórki.";
                return;
            }
            StartNewRound(list);
        }

        private async Task StartRepeatAllResetAsync()
        {
            StatusText = "Resetuję postępy w tym zestawie…";

            foreach (var c in _all)
                await Data.UpdateStatusAsync(UserId, c.TermId, 0);

            await Data.RecalculateProgressAsync(UserId, SetId);

            StartNewRound(_all);
        }
    }
}

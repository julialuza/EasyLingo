using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EasyLingo.Infrastructure;
using EasyLingo.Infrastructure.Commands;
using EasyLingo.Services;

namespace EasyLingo.ViewModels
{
    public enum OptionState
    {
        Normal,
        Correct,
        Wrong
    }

    public class OptionItem : BaseNotify
    {
        private OptionState _state = OptionState.Normal;

        public string Text { get; set; } = "";
        public bool IsCorrect { get; set; }

        public OptionState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(); }
        }
    }

    public class MultipleChoiceViewModel : BaseNotify
    {
        private readonly DataService _data = new DataService();
        private readonly Random _rng = new Random();

        public event Action? BackRequested;

        public int UserId { get; }
        public int SetId { get; }

        private List<(int TermId, string TermName, string Definition)> _terms = new();
        private Queue<(int TermId, string TermName, string Definition)> _queue = new();

        private (int TermId, string TermName, string Definition)? _current;

        private string _question = "";
        public string Question { get => _question; private set { _question = value; OnPropertyChanged(); } }

        private string _feedback = "";
        public string Feedback { get => _feedback; private set { _feedback = value; OnPropertyChanged(); } }

        public ObservableCollection<OptionItem> Options { get; } = new();

        private bool _canAnswer = true;
        public bool CanAnswer { get => _canAnswer; private set { _canAnswer = value; OnPropertyChanged(); } }

        private int _index = 0;
        private int _total = 0;

        private int _score = 0;
        private int _answered = 0;

        private bool _isFinished = false;

        public string ProgressText => _total == 0 ? "" : $"Pytanie {_index}/{_total}";
        public string ScoreText => _total == 0 ? "" : $"Wynik: {_score}/{_answered}";

        private string _nextButtonText = "NASTĘPNE";
        public string NextButtonText { get => _nextButtonText; private set { _nextButtonText = value; OnPropertyChanged(); } }

        private bool _askTermToDefinition = true; // true: term -> definicja, false: definicja -> term
        public bool AskTermToDefinition
        {
            get => _askTermToDefinition;
            set
            {
                if (_askTermToDefinition == value) return;
                _askTermToDefinition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModeLabel));

                if (_terms.Count >= 2)
                {
                    StartNewSession();
                    NextQuestion();
                }
            }
        }

        public string ModeLabel =>
        _uiLangId == 2
        ? (AskTermToDefinition ? "Modus: EN → PL" : "Modus: PL → EN")
        : (AskTermToDefinition ? "Mode: EN → PL" : "Mode: PL → EN");


        public RelayCommand BackCommand { get; }
        public RelayCommand AnswerCommand { get; }
        public RelayCommand NextCommand { get; }

        private readonly int _uiLangId;
        public MultipleChoiceViewModel(int userId, int setId, int uiLangId)
        {
            UserId = userId;
            SetId = setId;
            _uiLangId = uiLangId;

            BackCommand = new RelayCommand(_ => BackRequested?.Invoke());
            AnswerCommand = new RelayCommand(async p => await AnswerAsync(p as OptionItem), _ => CanAnswer);
            NextCommand = new RelayCommand(_ => NextOrFinish(), _ => _total > 0);

            _ = LoadAsync();

        }

        private async Task LoadAsync()
        {
            var terms = await _data.GetTermsBySetAsync(SetId);
            _terms = terms.Select(t => (t.TermId, t.TermName, t.Definition)).ToList();

            if (_terms.Count < 2)
            {
                Question = "Dodaj więcej słówek (min. 2).";
                Options.Clear();
                Feedback = "";
                NextButtonText = "ZAKOŃCZ";
                CanAnswer = false;
                _isFinished = true;

                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ScoreText));
                return;
            }

            StartNewSession();
            NextQuestion();
        }

        private void StartNewSession()
        {
            _isFinished = false;

            var shuffled = _terms.OrderBy(_ => _rng.Next()).ToList();
            _queue = new Queue<(int TermId, string TermName, string Definition)>(shuffled);

            _total = shuffled.Count;
            _index = 0;

            _score = 0;
            _answered = 0;

            Feedback = "";
            NextButtonText = "NASTĘPNE";
            CanAnswer = true;

            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ScoreText));
        }

        private void NextOrFinish()
        {
            if (_isFinished)
            {
                BackRequested?.Invoke();
                return;
            }

            if (CanAnswer && _current != null)
            {
                Feedback = "Najpierw wybierz odpowiedź.";
                return;
            }

            if (_queue.Count == 0)
            {
                FinishNow();
                return;
            }

            NextQuestion();
        }

        private void NextQuestion()
        {
            Feedback = "";
            CanAnswer = true;

            Options.Clear();

            _current = _queue.Dequeue();
            _index++;

            if (AskTermToDefinition)
                Question = _uiLangId == 2
                    ? $"Was bedeutet: {_current.Value.TermName}?"
                    : $"Co znaczy: {_current.Value.TermName}?";
            else
                Question = _uiLangId == 2
                    ? $"Wie heißt auf Englisch: {_current.Value.Definition}?"
                    : $"Jak brzmi po angielsku: {_current.Value.Definition}?";


            foreach (var opt in BuildOptions(_current.Value))
                Options.Add(opt);

            NextButtonText = (_queue.Count == 0) ? "ZAKOŃCZ" : "NASTĘPNE";

            OnPropertyChanged(nameof(ProgressText));
        }

        private List<OptionItem> BuildOptions((int TermId, string TermName, string Definition) current)
        {
            // tryby definicja - tłumaczenie / tłumaczenie - definicja
            List<string> uniquePool;

            if (AskTermToDefinition)
            {
                uniquePool = _terms
                    .Select(t => t.Definition)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                uniquePool = _terms
                    .Select(t => t.TermName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            string correct = AskTermToDefinition ? current.Definition : current.TermName;

            int desired = Math.Min(4, uniquePool.Count);
            desired = Math.Max(desired, 2);

            var wrongPool = uniquePool
                .Where(x => !string.Equals(x, correct, StringComparison.OrdinalIgnoreCase))
                .OrderBy(_ => _rng.Next())
                .ToList();

            var opts = new List<OptionItem>
            {
                new OptionItem { Text = correct, IsCorrect = true, State = OptionState.Normal }
            };

            foreach (var w in wrongPool.Take(desired - 1))
                opts.Add(new OptionItem { Text = w, IsCorrect = false, State = OptionState.Normal });

            return opts.OrderBy(_ => _rng.Next()).ToList();
        }

        private async Task AnswerAsync(OptionItem? chosen)
        {
            if (_current == null || chosen == null || !CanAnswer)
                return;

            CanAnswer = false;

            bool ok = chosen.IsCorrect;

            // poświetlenie kafelków - czerwony/zielony
            foreach (var o in Options)
            {
                if (o.IsCorrect)
                    o.State = OptionState.Correct;
                else if (ReferenceEquals(o, chosen))
                    o.State = OptionState.Wrong;
                else
                    o.State = OptionState.Normal;
            }

            _answered++;
            if (ok) _score++;

            if (ok)
            {
                await _data.UpdateStatusAsync(UserId, _current.Value.TermId, 1);
                await _data.RecalculateProgressAsync(UserId, SetId);
            }

            Feedback = ok ? "✅ Dobrze!" : "❌ Źle.";

            OnPropertyChanged(nameof(ScoreText));

            // jeśli ostatnie pytanie, koniec quizu
            if (_queue.Count == 0)
            {
                Feedback = $"{(ok ? "Dobrze!" : "Źle.")}\n" +
                           $"Koniec quizu. Wynik: {_score}/{_answered}\n" +
                           "Kliknij „ZAKOŃCZ”, aby wrócić.";

                FinishNow(setButtonTextOnly: true);
            }
        }

        private void FinishNow(bool setButtonTextOnly = false)
        {
            _isFinished = true;
            CanAnswer = false;

            if (!setButtonTextOnly)
            {
                Question = "Koniec quizu";
                Options.Clear();
                Feedback = $"Wynik końcowy: {_score}/{_answered}\nKliknij „ZAKOŃCZ”, aby wrócić.";
            }

            NextButtonText = "ZAKOŃCZ";
            OnPropertyChanged(nameof(ProgressText));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EasyLingo.Infrastructure.Commands;
using EasyLingo.Infrastructure.StudyModes;
using EasyLingo.Services.Interfaces;
using EasyLingo.ViewModels.StudyModes;

namespace EasyLingo.ViewModels
{
    [StudyMode("typing", "Wpisywanie")]
    public class TypingViewModel : StudyModeViewModelBase
    {
        private readonly Random _rng = new Random();

        public override string Title => "Wpisywanie";

        private List<(int TermId, string TermName, string Definition)> _all = new();
        private Queue<(int TermId, string TermName, string Definition)> _queue = new();

        private (int TermId, string TermName, string Definition)? _current;

        private int _index = 0;
        private int _total = 0;
        private int _correct = 0;
        private int _answered = 0;

        private readonly List<(int TermId, string TermName, string Definition)> _wrongCurrentSession = new();
        private List<(int TermId, string TermName, string Definition)> _lastWrongSnapshot = new();

        private bool _awaitingNext = false;

        private string _prompt = "";
        public string Prompt { get => _prompt; private set { _prompt = value; OnPropertyChanged(); } }

        private string _answer = "";
        public string Answer { get => _answer; set { _answer = value; OnPropertyChanged(); } }

        private string _feedback = "";
        public string Feedback { get => _feedback; private set { _feedback = value; OnPropertyChanged(); } }

        public string ProgressText => _total == 0 ? "" : $"Słówko {_index}/{_total}";
        public string ScoreText => _total == 0 ? "" : $"Poprawne: {_correct}/{_answered}";

        private string _nextButtonText = "NASTĘPNE";
        public string NextButtonText { get => _nextButtonText; private set { _nextButtonText = value; OnPropertyChanged(); } }

        private bool _canCheck = true;
        public bool CanCheck { get => _canCheck; private set { _canCheck = value; OnPropertyChanged(); } }

        private Visibility _endActionsVisible = Visibility.Collapsed;
        public Visibility EndActionsVisible
        {
            get => _endActionsVisible;
            private set { _endActionsVisible = value; OnPropertyChanged(); }
        }

        public RelayCommand CheckCommand { get; }
        public RelayCommand NextCommand { get; }
        public RelayCommand PrimaryEnterCommand { get; }

        public RelayCommand RepeatWrongCommand { get; }
        public RelayCommand RepeatAllCommand { get; }

        public TypingViewModel(IDataService data, int userId, int setId)
            : base(data, userId, setId)
        {
            CheckCommand = new RelayCommand(async _ => await CheckAsync(), _ => CanCheck && _current != null);
            NextCommand = new RelayCommand(_ => NextOrFinish(), _ => _total > 0);

            PrimaryEnterCommand = new RelayCommand(async _ =>
            {
                if (_current == null)
                {
                    RequestBack();
                    return;
                }

                if (!_awaitingNext) await CheckAsync();
                else NextOrFinish();
            });

            RepeatWrongCommand = new RelayCommand(
                _ =>
                {
                    StartSessionFromWrongSnapshot();
                    if (_queue.Count > 0) NextQuestion();
                },
                _ => _lastWrongSnapshot.Count > 0
            );

            RepeatAllCommand = new RelayCommand(
                _ =>
                {
                    StartSessionFromAll();
                    if (_queue.Count > 0) NextQuestion();
                },
                _ => _all.Count > 0
            );

            _ = LoadAsync();
        }

        public override void Restart()
        {
            if (_all.Count > 0)
            {
                StartSessionFromAll();
                if (_queue.Count > 0) NextQuestion();
            }
        }

        private async Task LoadAsync()
        {
            var terms = await Data.GetTermsBySetAsync(SetId);
            _all = terms.Select(t => (t.TermId, t.TermName, t.Definition)).ToList();

            if (_all.Count == 0)
            {
                Prompt = "Brak słówek w zestawie.";
                Feedback = "Dodaj słówka w szczegółach zestawu 🙂";
                CanCheck = false;
                NextButtonText = "ZAKOŃCZ";
                _total = 0;
                EndActionsVisible = Visibility.Collapsed;
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ScoreText));
                return;
            }

            StartSessionFromAll();
            NextQuestion();
        }

        private void StartSessionCore(List<(int TermId, string TermName, string Definition)> baseList)
        {
            _wrongCurrentSession.Clear();

            var shuffled = baseList.OrderBy(_ => _rng.Next()).ToList();
            _queue = new Queue<(int TermId, string TermName, string Definition)>(shuffled);

            _total = shuffled.Count;
            _index = 0;
            _correct = 0;
            _answered = 0;

            _current = null;
            EndActionsVisible = Visibility.Collapsed;

            Feedback = "";
            Answer = "";
            CanCheck = true;
            _awaitingNext = false;
            NextButtonText = "NASTĘPNE";

            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ScoreText));
        }

        private void StartSessionFromAll()
        {
            StartSessionCore(_all.ToList());
            CommandManager.InvalidateRequerySuggested();
        }

        private void StartSessionFromWrongSnapshot()
        {
            if (_lastWrongSnapshot.Count == 0) return;
            StartSessionCore(_lastWrongSnapshot.ToList());
            CommandManager.InvalidateRequerySuggested();
        }

        private void NextOrFinish()
        {
            if (_current == null)
            {
                RequestBack();
                return;
            }

            if (!_awaitingNext)
            {
                Feedback = "Najpierw kliknij „SPRAWDŹ” (lub Enter) 🙂";
                return;
            }

            if (_queue.Count == 0)
            {
                ShowFinished();
                return;
            }

            NextQuestion();
        }

        private void NextQuestion()
        {
            Feedback = "";
            Answer = "";

            _awaitingNext = false;
            CanCheck = true;

            if (_queue.Count == 0)
            {
                ShowFinished();
                return;
            }

            _current = _queue.Dequeue();
            _index++;

            Prompt = $"Wpisz po angielsku: {_current.Value.Definition}";
            NextButtonText = (_queue.Count == 0) ? "ZAKOŃCZ" : "NASTĘPNE";
            EndActionsVisible = Visibility.Collapsed;

            OnPropertyChanged(nameof(ProgressText));
        }

        private async Task CheckAsync()
        {
            if (_current == null) return;

            var expected = (_current.Value.TermName ?? "").Trim();
            var got = (Answer ?? "").Trim();

            if (string.IsNullOrWhiteSpace(got))
            {
                Feedback = "Wpisz odpowiedź 🙂";
                return;
            }

            bool ok = string.Equals(got, expected, StringComparison.OrdinalIgnoreCase);

            _answered++;
            if (ok) _correct++;
            else _wrongCurrentSession.Add(_current.Value);

            if (ok)
            {
                await Data.UpdateStatusAsync(UserId, _current.Value.TermId, 1);
                await Data.RecalculateProgressAsync(UserId, SetId);
            }

            Feedback = ok ? "✅ Dobrze!" : $"❌ Źle. Poprawna odpowiedź: {expected}";

            _awaitingNext = true;
            CanCheck = false;

            OnPropertyChanged(nameof(ScoreText));

            if (_queue.Count == 0)
                ShowFinished();
        }

        private void ShowFinished()
        {
            _lastWrongSnapshot = _wrongCurrentSession.ToList();

            _current = null;

            CanCheck = false;
            _awaitingNext = true;

            Prompt = "Koniec ✅";
            Answer = "";

            Feedback =
                $"Wynik: {_correct}/{_answered}\n" +
                (_lastWrongSnapshot.Count > 0
                    ? $"Błędne: {_lastWrongSnapshot.Count}\nWybierz co powtórzyć poniżej."
                    : "Świetnie! Nie masz błędnych odpowiedzi.");

            NextButtonText = "ZAKOŃCZ";
            EndActionsVisible = Visibility.Visible;

            CommandManager.InvalidateRequerySuggested();

            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ScoreText));
        }
    }
}

using EasyLingo.Views;
using System.Threading.Tasks;
using System.Windows;
using EasyLingo.ViewModels;
using EasyLingo.Data;
using EasyLingo.Services;
using EasyLingo.Services.Interfaces;

namespace EasyLingo
{
    public partial class MainWindow : Window
    {
        public int LoggedUserId { get; private set; } = -1;

        public int SelectedLangId { get; set; } = 1;

        private readonly IDataService _dataService = new DataService();

        private DashboardView? _dashboardView;
        private SetsView? _setsView;
        private AchievementsView? _achievementsView;

        private DashboardViewModel? _dashboardVM;
        private SetsViewModel? _setsVM;
        private AchievementsViewModel? _achievementsVM;

        public MainWindow()
        {
            InitializeComponent();
            ShowWelcome();
        }

        private void ShowWelcome()
        {
            var welcome = new WelcomeView();
            welcome.LoginClicked += ShowLoginView;
            welcome.RegisterClicked += ShowSignUpView;

            StartContentControl.Content = welcome;
            StartContentControl.Visibility = Visibility.Visible;
            MainAppGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowLoginView()
        {
            var logView = new LogView();
            logView.LoginSucceeded += OnLoginSuccess;

            logView.RegisterRequested += ShowSignUpView;

            StartContentControl.Content = logView;
        }

        private void ShowSignUpView()
        {
            var signInView = new SignUpView();
            signInView.SignInSucceeded += ShowLoginView;
            StartContentControl.Content = signInView;
        }

        private async void OnLoginSuccess(int userId)
        {
            LoggedUserId = userId;
            var user = await _dataService.GetUserByIdAsync(userId);
            TopBar.UserName = user?.Username ?? "";

            StartContentControl.Visibility = Visibility.Collapsed;
            MainAppGrid.Visibility = Visibility.Visible;

            _dashboardVM = new DashboardViewModel(userId);

            _setsVM = new SetsViewModel(userId);
            _setsVM.OpenSetRequested += ShowSetDetails;

            _achievementsVM = new AchievementsViewModel(new AppDbContext(), userId, SelectedLangId);

            _dashboardView = new DashboardView { DataContext = _dashboardVM };
            _setsView = new SetsView { DataContext = _setsVM };
            _achievementsView = new AchievementsView { DataContext = _achievementsVM };

            Sidebar.SetMainWindow(this);

            _ = _setsVM.SetLanguageAsync(SelectedLangId);

            MainContentControl.Content = _dashboardView;
        }

        public void ShowDashboard()
        {
            if (_dashboardView != null)
                MainContentControl.Content = _dashboardView;
        }

        public async void ShowSets()
        {
            if (_setsVM != null)
                await _setsVM.ReloadAsync();

            if (_setsView != null)
                MainContentControl.Content = _setsView;
        }

        public async void ShowAchievements()
        {
            if (_achievementsVM != null)
                await _achievementsVM.LoadAsync();

            if (_achievementsView != null)
                MainContentControl.Content = _achievementsView;
        }

        public async void ChangeLanguage(int langId)
        {
            SelectedLangId = langId;

            if (_setsVM != null)
                await _setsVM.SetLanguageAsync(langId);

            if (_achievementsVM != null)
                await _achievementsVM.SetLanguageAsync(langId);

            if (_dashboardVM != null)
                await _dashboardVM.SetLanguageAsync(langId);
        }

        public void ShowSetDetails(int setId)
        {
            var vm = new SetDetailsViewModel(LoggedUserId, setId);

            vm.BackRequested += () => ShowSets();

            vm.OpenFlashcardsRequested += () => ShowFlashcards(setId);
            vm.OpenMultipleChoiceRequested += () => ShowMultipleChoice(setId);
            vm.OpenMatchRequested += () => ShowMatch(setId);
            vm.OpenTypingRequested += () => ShowTyping(setId);

            MainContentControl.Content = new SetDetailsView { DataContext = vm };
        }

        public void ShowFlashcards(int setId)
        {
            var vm = new FlashcardsViewModel(_dataService, LoggedUserId, setId);

            vm.BackRequested += async () =>
            {
                await RefreshAfterLearningAsync();
                ShowSetDetails(setId);
            };

            MainContentControl.Content = new FlashcardsView { DataContext = vm };
        }

        public void ShowMultipleChoice(int setId)
        {
            var vm = new MultipleChoiceViewModel(LoggedUserId, setId, SelectedLangId);

            vm.BackRequested += async () =>
            {
                await RefreshAfterLearningAsync();
                ShowSetDetails(setId);
            };

            MainContentControl.Content = new MultipleChoiceView { DataContext = vm };
        }


        public void ShowMatch(int setId)
        {
            var vm = new MatchViewModel(_dataService, LoggedUserId, setId);

            vm.BackRequested += async () =>
            {
                await RefreshAfterLearningAsync();
                ShowSetDetails(setId);
            };

            MainContentControl.Content = new MatchView { DataContext = vm };
        }

        public void ShowTyping(int setId)
        {
            var vm = new TypingViewModel(_dataService, LoggedUserId, setId);

            vm.BackRequested += async () =>
            {
                await RefreshAfterLearningAsync();
                ShowSetDetails(setId);
            };

            MainContentControl.Content = new TypingView { DataContext = vm };
        }

        private async Task RefreshAfterLearningAsync()
        {
            if (_setsVM != null)
                await _setsVM.ReloadAsync();

            if (_achievementsVM != null)
                await _achievementsVM.LoadAsync();

            if (_dashboardVM != null)
                await _dashboardVM.ReloadAsync();
        }

        public void Logout()
        {
            LoggedUserId = -1;

            _dashboardView = null;
            _setsView = null;
            _achievementsView = null;

            _dashboardVM = null;
            _setsVM = null;
            _achievementsVM = null;

            MainContentControl.Content = null;

            ShowWelcome();
        }
    }
}

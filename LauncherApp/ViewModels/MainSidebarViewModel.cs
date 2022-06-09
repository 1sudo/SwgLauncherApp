using System.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;

namespace LauncherApp.ViewModels
{
    internal class MainSidebarViewModel
    {
        public IRelayCommand VoteButton { get; }
        public IRelayCommand DonateButton { get; }
        public IRelayCommand ResourcesButton { get; }
        public IRelayCommand BugReportButton { get; }
        public IRelayCommand SkillPlannerButton { get; }
        public IRelayCommand SettingsButton { get; }
        public IRelayCommand OptionsButton { get; }
        public IRelayCommand DeveloperButton { get; }

        public MainSidebarViewModel()
        {
            VoteButton = new RelayCommand(() => Trace.WriteLine("Vote button pressed"));
            DonateButton = new RelayCommand(() => Trace.WriteLine("Donate button pressed"));
            ResourcesButton = new RelayCommand(() => Trace.WriteLine("Resources button pressed"));
            BugReportButton = new RelayCommand(() => Trace.WriteLine("Bugreport button pressed"));
            SkillPlannerButton = new RelayCommand(() => Trace.WriteLine("Skillplanner button pressed"));
            SettingsButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Settings));
            OptionsButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.OptionsAndMods));
            DeveloperButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Developer));
        }
    }
}

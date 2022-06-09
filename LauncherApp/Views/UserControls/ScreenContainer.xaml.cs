using System.Collections.Generic;
using System.Windows.Controls;
using LauncherApp.ViewModels;

namespace LauncherApp.Views.UserControls
{
    /// <summary>
    /// Interaction logic for MainScreen.xaml
    /// </summary>
    public partial class ScreenContainer
    {
        public ScreenContainer()
        {
            InitializeComponent();

            List<UserControl> userControls = new()
            {
                RulesUserControl,
                InstallDirectorySelectionUserControl,
                BaseGameVerificationUserControl,
                AccountCreationUserControl,
                AccountLoginUserControl,
                UpdatesUserControl,
                SettingsUserControl,
                OptionsAndModsUserControl,
                DeveloperUserControl,
                SetupSidebarUserControl,
                AccountSidebarUserControl,
                MainSidebarUserControl
            };

            DataContext = new ScreenContainerViewModel(userControls);

            SetupScreenViewModel setupScreenViewModel = new();
            SetupSidebarUserControl.DataContext = setupScreenViewModel;
            RulesUserControl.DataContext = setupScreenViewModel;
            InstallDirectorySelectionUserControl.DataContext = setupScreenViewModel;
            BaseGameVerificationUserControl.DataContext = setupScreenViewModel;

            AccountScreenViewModel accountScreenViewModel = new();
            AccountSidebarUserControl.DataContext = accountScreenViewModel;
            AccountCreationUserControl.DataContext = accountScreenViewModel;
            AccountLoginUserControl.DataContext = accountScreenViewModel;

            MainSidebarUserControl.DataContext = new MainSidebarViewModel();
            UpdatesUserControl.DataContext = new UpdatesViewModel();
            SettingsUserControl.DataContext = new SettingsViewModel();
            OptionsAndModsUserControl.DataContext = new OptionsAndModsViewModel();
            DeveloperUserControl.DataContext = new DeveloperViewModel();
        }
    }
}

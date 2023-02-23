using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Input;
using LauncherApp.Models.Properties;
using LauncherApp.Models.Util;

namespace LauncherApp.ViewModels;

internal class SetupScreenViewModel : SetupScreenViewModelProperties
{
    public IAsyncRelayCommand? RulesAndRegulationsNextButton { get; }
    public IRelayCommand? EasySetupButton { get; }
    public IRelayCommand? AdvancedSetupButton { get; }
    public IRelayCommand? InstallDirectoryBackButton { get; }
    public IAsyncRelayCommand? InstallDirectoryNextButton { get; }
    public IRelayCommand? AdvancedSetupBrowseButton { get; }
    public IRelayCommand? BaseGameVerificationBrowseButton { get; }
    public IAsyncRelayCommand? BaseGameVerificationNextButton { get; }
    public IRelayCommand? BaseGameVerificationBackButton { get; }
    public IRelayCommand? RulesCancelButton { get; }
    public IRelayCommand? InstallDirectoryCancelButton { get; }
    public IRelayCommand? BaseGameVerificationCancelButton { get; }

    public SetupScreenViewModel()
    {
        CurrentScreen = 0;
        AdvancedSetupBubble = Visibility.Collapsed;
        AdvancedSetupDetails = Visibility.Collapsed;
        BaseGameVerificationDetails = Visibility.Collapsed;
        SelectedSetupType = (int)SetupType.Easy;

        RulesAndRegulationsNextButton = new AsyncRelayCommand(GoToNextScreen);
        EasySetupButton = new RelayCommand(SetEasySetup);
        AdvancedSetupButton = new RelayCommand(SetAdvancedSetup);
        InstallDirectoryBackButton = new RelayCommand(GoToPreviousScreen);
        InstallDirectoryNextButton = new AsyncRelayCommand(GoToNextScreen);
        AdvancedSetupBrowseButton = new RelayCommand(AdvancedSetupBrowse);
        BaseGameVerificationBrowseButton = new RelayCommand(BaseGameVerificationBrowse);
        BaseGameVerificationNextButton = new AsyncRelayCommand(GoToNextScreen);
        BaseGameVerificationBackButton = new RelayCommand(GoToPreviousScreen);
        RulesCancelButton = new RelayCommand(CloseApplication);
        InstallDirectoryCancelButton = new RelayCommand(CloseApplication);
        BaseGameVerificationCancelButton = new RelayCommand(CloseApplication);
    }

    internal static void ToggleNextButton(SetupScreenViewModelProperties vmp, bool enabled, int nextButton)
    {
        if (nextButton == (int)NextButton.Rules)
        {
            vmp.RulesAndRegulationsNextButtonToggle = enabled;
        }
        else if (nextButton == (int)NextButton.BaseGameVerification)
        {
            vmp.BaseGameVerificationNextButtonToggle = enabled;
        }
    } 

    private async Task GoToNextScreen()
    {
        if (CurrentScreen == (int)Screen.RulesAndRegulations)
        {
            ScreenContainerViewModel.EnableScreen(Screen.InstallDirectorySelection);
            CurrentScreen = (int)Screen.InstallDirectorySelection;
        }
        else if (CurrentScreen == (int)Screen.InstallDirectorySelection)
        {
            ScreenContainerViewModel.EnableScreen(Screen.BaseGameVerification);
            CurrentScreen = (int)Screen.BaseGameVerification;
        }
        else if (CurrentScreen == (int) Screen.BaseGameVerification)
        {
            if (await Models.Handlers.HttpHandler.CheckBaseInstallation(BaseGameVerificationSelectedDirectoryTextBox!))
            {
                ConfigFile? config = ConfigFile.GetConfig()!;

                // Default for Easy setup type is in ConfigFile defaults
                if (SelectedSetupType == (int)SetupType.Advanced)
                {
                    config!.Servers![0].GameLocation = BaseGameVerificationSelectedDirectoryTextBox;
                }
                
                config!.Servers![0].Verified = true;

                ConfigFile.SetConfig(config);

                ScreenContainerViewModel.EnableScreen(Screen.AccountLogin);
            }
            else
            {
                System.Windows.MessageBox.Show("Invalid game directory selected, please try again.", 
                    "Invalid Game Directory", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }

    private void GoToPreviousScreen()
    {
        if (CurrentScreen == (int)Screen.InstallDirectorySelection)
        {
            ScreenContainerViewModel.EnableScreen(Screen.RulesAndRegulations);
            CurrentScreen = (int)Screen.RulesAndRegulations;
        }
        else if (CurrentScreen == (int)Screen.BaseGameVerification)
        {
            ScreenContainerViewModel.EnableScreen(Screen.InstallDirectorySelection);
            CurrentScreen = (int)Screen.InstallDirectorySelection;
        }
    }

    private void SetEasySetup()
    {
        EasySetupBubble = Visibility.Visible;
        AdvancedSetupBubble = Visibility.Collapsed;
        SelectedSetupType = (int)SetupType.Easy;
        AdvancedSetupDetails = Visibility.Collapsed;
    }

    private void SetAdvancedSetup()
    {
        EasySetupBubble = Visibility.Collapsed;
        AdvancedSetupBubble = Visibility.Visible;
        SelectedSetupType = (int)SetupType.Advanced;
        AdvancedSetupDetails = Visibility.Visible;
    }

    private void AdvancedSetupBrowse()
    {
        using var dialog = new FolderBrowserDialog();
        DialogResult result = dialog.ShowDialog();

        if (result.ToString().Trim() == "Cancel")
        {
            AdvancedSetupTextBox = string.Empty;
        }
        else if (result.ToString().Trim() == "OK")
        {
            AdvancedSetupTextBox = dialog.SelectedPath.Replace("\\", "/");
        }
    }

    private void BaseGameVerificationBrowse()
    {
        using var dialog = new FolderBrowserDialog();
        DialogResult result = dialog.ShowDialog();

        if (result.ToString().Trim() == "Cancel")
        {
            BaseGameVerificationSelectedDirectoryTextBox = string.Empty;
        }
        else if (result.ToString().Trim() == "OK")
        {
            BaseGameVerificationSelectedDirectoryTextBox = dialog.SelectedPath.Replace("\\", "/");
        }
    }

    internal static void ToggleGameValidationDetails(SetupScreenViewModelProperties vmp, bool enabled)
    {
        if (enabled)
        {
            vmp.BaseGameVerificationDetails = Visibility.Visible;
        }
        else
        {
            vmp.BaseGameVerificationDetails = Visibility.Collapsed;
        }
    }

    private void CloseApplication()
    {
        TrueExit.Value = true;
        System.Windows.Application.Current.MainWindow!.Close();
    }
}

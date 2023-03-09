using System.Windows;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherApp.Models;
using LibLauncherApp.Properties;

namespace LauncherApp.ViewModels;

public enum SetupType
{
    Easy = 0,
    Advanced = 1
}

internal class SetupScreenViewModel : ObservableObject
{
    public IRelayCommand? RulesAndRegulationsNextButton { get; }
    public IRelayCommand? EasySetupButton { get; }
    public IRelayCommand? AdvancedSetupButton { get; }
    public IRelayCommand? InstallDirectoryBackButton { get; }
    public IRelayCommand? InstallDirectoryNextButton { get; }
    public IRelayCommand? AdvancedSetupBrowseButton { get; }
    public IRelayCommand? BaseGameVerificationBrowseButton { get; }
    public IRelayCommand? BaseGameVerificationNextButton { get; }
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

        RulesAndRegulationsNextButton = new RelayCommand(GoToNextScreen);
        EasySetupButton = new RelayCommand(SetEasySetup);
        AdvancedSetupButton = new RelayCommand(SetAdvancedSetup);
        InstallDirectoryBackButton = new RelayCommand(GoToPreviousScreen);
        InstallDirectoryNextButton = new RelayCommand(GoToNextScreen);
        AdvancedSetupBrowseButton = new RelayCommand(AdvancedSetupBrowse);
        BaseGameVerificationBrowseButton = new RelayCommand(BaseGameVerificationBrowse);
        BaseGameVerificationNextButton = new RelayCommand(GoToNextScreen);
        BaseGameVerificationBackButton = new RelayCommand(GoToPreviousScreen);
        RulesCancelButton = new RelayCommand(CloseApplication);
        InstallDirectoryCancelButton = new RelayCommand(CloseApplication);
        BaseGameVerificationCancelButton = new RelayCommand(CloseApplication);
    }

    internal void ToggleNextButton(bool enabled, int nextButton)
    {
        if (nextButton == (int)NextButton.Rules)
        {
            RulesAndRegulationsNextButtonToggle = enabled;
        }
        else if (nextButton == (int)NextButton.BaseGameVerification)
        {
            BaseGameVerificationNextButtonToggle = enabled;
        }
    } 

    private void GoToNextScreen()
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
            if (FileHandler.CheckBaseInstallation(BaseGameVerificationSelectedDirectoryTextBox!))
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

    internal void ToggleGameValidationDetails(bool enabled)
    {
        if (enabled)
        {
            BaseGameVerificationDetails = Visibility.Visible;
        }
        else
        {
            BaseGameVerificationDetails = Visibility.Collapsed;
        }
    }

    private void CloseApplication()
    {
        TrueExit.Value = true;
        System.Windows.Application.Current.MainWindow!.Close();
    }

    private bool _rulesAndRegulationsCheckbox;
    private bool _rulesAndRegulationsNextButtonToggle;
    private bool _baseGameVerificationNextButtonToggle;
    private bool _gameValidationCheckbox;
    private Visibility _easySetupBubble;
    private Visibility _advancedSetupBubble;
    private Visibility _advancedSetupDetails;
    private Visibility _baseGameVerificationDetails;
    private string? _advancedSetupTextBox;
    private string? _baseGameVerificationSelectedDirectoryTextBox;

    public int SelectedSetupType { get; set; }
    public int CurrentScreen { get; set; }

    public Visibility EasySetupBubble
    {
        get => _easySetupBubble;
        set => SetProperty(ref _easySetupBubble, value);
    }

    public Visibility AdvancedSetupBubble
    {
        get => _advancedSetupBubble;
        set => SetProperty(ref _advancedSetupBubble, value);
    }

    public Visibility AdvancedSetupDetails
    {
        get => _advancedSetupDetails;
        set => SetProperty(ref _advancedSetupDetails, value);
    }

    public Visibility BaseGameVerificationDetails
    {
        get => _baseGameVerificationDetails;
        set => SetProperty(ref _baseGameVerificationDetails, value);
    }

    public bool RulesAndRegulationsCheckbox
    {
        get => _rulesAndRegulationsCheckbox;
        set
        {
            SetProperty(ref _rulesAndRegulationsCheckbox, value);
            ToggleNextButton(value, (int)NextButton.Rules);
        }
    }

    public bool RulesAndRegulationsNextButtonToggle
    {
        get => _rulesAndRegulationsNextButtonToggle;
        set => SetProperty(ref _rulesAndRegulationsNextButtonToggle, value);
    }

    public bool BaseGameVerificationNextButtonToggle
    {
        get => _baseGameVerificationNextButtonToggle;
        set => SetProperty(ref _baseGameVerificationNextButtonToggle, value);
    }

    public bool GameValidationCheckbox
    {
        get => _gameValidationCheckbox;
        set
        {
            SetProperty(ref _gameValidationCheckbox, value);
            ToggleGameValidationDetails(value);
        }
    }

    public string? AdvancedSetupTextBox
    {
        get => _advancedSetupTextBox!;
        set => SetProperty(ref _advancedSetupTextBox, value);
    }

    public string? BaseGameVerificationSelectedDirectoryTextBox
    {
        get => _baseGameVerificationSelectedDirectoryTextBox;
        set
        {
            SetProperty(ref _baseGameVerificationSelectedDirectoryTextBox, value);
            ToggleNextButton((value != string.Empty), (int)NextButton.BaseGameVerification);
        }
    }
}

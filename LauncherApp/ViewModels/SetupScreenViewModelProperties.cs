using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using static LauncherApp.ViewModels.SetupScreenViewModel;

namespace LauncherApp.ViewModels
{
    public enum SetupType
    {
        Easy = 0,
        Advanced = 1
    }

    public class SetupScreenViewModelProperties : ObservableObject
    {
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
                ToggleNextButton(this, value, (int)NextButton.Rules);
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
                ToggleGameValidationDetails(this, value);
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
                ToggleNextButton(this, (value != string.Empty), (int)NextButton.BaseGameVerification);
            }
        }
    }
}
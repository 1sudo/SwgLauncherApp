using System.Security;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

using static LauncherApp.ViewModels.AccountScreenViewModel;

namespace LauncherApp.ViewModels;

public enum WatermarkType
{
    AccountLoginUsername = 0,
    AccountLoginPassword = 1,
    AccountCreationUsername = 3,
    AccountCreationEmail = 4,
    AccountCreationPassword = 5,
    AccountCreationPasswordConfirmation = 6,
    AccountCreationSecurityQuestionAnswer = 7,
    AccountCreationDiscord = 8
}

internal class AccountScreenViewModelProperties : ObservableObject
{
    private string? _accountLoginUsernameTextBox;
    private SecureString? _accountLoginPasswordBox;
    private string? _accountLoginPasswordWatermark;
    private string? _accountLoginUsernameWatermark;
    private string? _accountLoginFailedTextBlock;
    private bool _accountKeepLoggedInCheckbox;
    private string? _accountCreationUsernameTextBox;
    private string? _accountCreationUsernameWatermark;
    private string? _accountCreationEmailAddressTextBox;
    private string? _accountCreationEmailAddressWatermark;
    private SecureString? _accountCreationPasswordBox;
    private string? _accountCreationPasswordWatermark;
    private SecureString? _accountCreationPasswordConfirmationBox;
    private string? _accountCreationPasswordConfirmationWatermark;
    private bool _accountCreationNewsletterSubscriptionCheckbox;
    private string? _accountCreationSecurityQuestionAnswerTextBox;
    private string? _accountCreationSecurityQuestionAnswerWatermark;
    private string? _accountCreationDiscordTextBox;
    private string? _accountCreationDiscordWatermark;
    private string? _accountCreationFailedTextBlock;
    private bool _createAccountButtonToggle;
    private bool _accountLoginButtonToggle;
    private Visibility? _accountLoginFailedTextBlockVisibility;
    private Visibility? _accountCreationFailedTextBlockVisibility;

    public int CurrentScreen { get; set; }

    public string? AccountLoginUsernameTextBox
    {
        get => _accountLoginUsernameTextBox;
        set
        {
            SetProperty(ref _accountLoginUsernameTextBox, value);
            WatermarkIntercept(this, (int)WatermarkType.AccountLoginUsername);
        }
    }

    public SecureString? AccountLoginPasswordBox
    {
        get => _accountLoginPasswordBox;
        set
        {
            SetProperty(ref _accountLoginPasswordBox, value);
            WatermarkIntercept(this, (int)WatermarkType.AccountLoginPassword);
        }
    }

    public string? AccountLoginPasswordWatermark
    {
        get => _accountLoginPasswordWatermark;
        set => SetProperty(ref _accountLoginPasswordWatermark, value);
    }

    public string? AccountLoginUsernameWatermark
    {
        get => _accountLoginUsernameWatermark;
        set => SetProperty(ref _accountLoginUsernameWatermark, value);
    }

    public string? AccountLoginFailedTextBlock
    {
        get => _accountLoginFailedTextBlock;
        set => SetProperty(ref _accountLoginFailedTextBlock, value);
    }

    public bool AccountKeepLoggedInCheckbox
    {
        get => _accountKeepLoggedInCheckbox;
        set => SetProperty(ref _accountKeepLoggedInCheckbox, value);
    }

    public string? AccountCreationUsernameTextBox
    {
        get => _accountCreationUsernameTextBox;
        set
        {
            SetProperty(ref _accountCreationUsernameTextBox, value);
            WatermarkIntercept(this, (int)WatermarkType.AccountCreationUsername);
        } 
    }

    public string? AccountCreationUsernameWatermark
    {
        get => _accountCreationUsernameWatermark;
        set => SetProperty(ref _accountCreationUsernameWatermark, value);
    }

    public string? AccountCreationEmailAddressTextBox
    {
        get => _accountCreationEmailAddressTextBox;
        set
        {
            SetProperty(ref _accountCreationEmailAddressTextBox, value);
            WatermarkIntercept(this, (int)WatermarkType.AccountCreationEmail);
        }
    }

    public string? AccountCreationEmailAddressWatermark
    {
        get => _accountCreationEmailAddressWatermark;
        set => SetProperty(ref _accountCreationEmailAddressWatermark, value);
    }

    public SecureString? AccountCreationPasswordBox
    {
        get => _accountCreationPasswordBox;
        set
        {
            SetProperty(ref _accountCreationPasswordBox, value);
            WatermarkIntercept(this, (int)WatermarkType.AccountCreationPassword);
        }
    }

    public string? AccountCreationPasswordWatermark
    {
        get => _accountCreationPasswordWatermark;
        set => SetProperty(ref _accountCreationPasswordWatermark, value);
    }

    public SecureString? AccountCreationPasswordConfirmationBox
    {
        get => _accountCreationPasswordConfirmationBox;
        set
        {
            SetProperty(ref _accountCreationPasswordConfirmationBox, value);
            WatermarkIntercept(this, (int)WatermarkType.AccountCreationPasswordConfirmation);
        }
    }

    public string? AccountCreationPasswordConfirmationWatermark
    {
        get => _accountCreationPasswordConfirmationWatermark;
        set => SetProperty(ref _accountCreationPasswordConfirmationWatermark, value);
    }

    public bool AccountCreationNewsletterSubscriptionCheckbox
    {
        get => _accountCreationNewsletterSubscriptionCheckbox;
        set => SetProperty(ref _accountCreationNewsletterSubscriptionCheckbox, value);
    }

    public string? AccountCreationSecurityQuestionAnswerTextBox
    {
        get => _accountCreationSecurityQuestionAnswerTextBox;
        set
        {
            SetProperty(ref _accountCreationSecurityQuestionAnswerTextBox, value);
            WatermarkIntercept(this, (int)WatermarkType.AccountCreationSecurityQuestionAnswer);
        }
    }

    public string? AccountCreationSecurityQuestionAnswerWatermark
    {
        get => _accountCreationSecurityQuestionAnswerWatermark;
        set => SetProperty(ref _accountCreationSecurityQuestionAnswerWatermark, value);
    }

    public string? AccountCreationDiscordTextBox
    {
        get => _accountCreationDiscordTextBox;
        set
        {
            SetProperty(ref _accountCreationDiscordTextBox, value);
            WatermarkIntercept(this, (int)WatermarkType.AccountCreationDiscord);
        }
    }

    public string? AccountCreationDiscordWatermark
    {
        get => _accountCreationDiscordWatermark;
        set => SetProperty(ref _accountCreationDiscordWatermark, value);
    }

    public string? AccountCreationFailedTextBlock
    {
        get => _accountCreationFailedTextBlock;
        set => SetProperty(ref _accountCreationFailedTextBlock, value);
    }

    public bool CreateAccountButtonToggle
    {
        get => _createAccountButtonToggle;
        set => SetProperty(ref _createAccountButtonToggle, value);
    }

    public bool AccountLoginButtonToggle
    {
        get => _accountLoginButtonToggle;
        set => SetProperty(ref _accountLoginButtonToggle, value);
    }

    public Visibility? AccountLoginFailedTextBlockVisibility
    {
        get => _accountLoginFailedTextBlockVisibility;
        set => SetProperty(ref _accountLoginFailedTextBlockVisibility, value);
    }

    public Visibility? AccountCreationFailedTextBlockVisibility
    {
        get => _accountCreationFailedTextBlockVisibility;
        set => SetProperty(ref _accountCreationFailedTextBlockVisibility, value);
    }
}

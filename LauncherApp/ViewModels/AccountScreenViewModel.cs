using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using LibLauncherApp.Properties;
using LibLauncherApp.gRPC;
using LibLauncherApp.gRPC.Models;
using LauncherApp.Models;
using System.Security;
using CommunityToolkit.Mvvm.ComponentModel;

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

internal class AccountScreenViewModel : ObservableObject
{
    public IAsyncRelayCommand? AccountLoginButton { get; }
    public IRelayCommand? AccountLoginCreateAccountButton { get; }
    public IRelayCommand AccountSidebarCreateAccountButton { get; }
    public IRelayCommand? AccountCreationCreateAccountButton { get; }
    public IRelayCommand? AccountCreationCancelButton { get; }
    private const int ErrorSleepTime = 5000;
    public static event EventHandler<OnSetUsernameEventArgs>? OnSetUsername;
    private readonly Requests _requests;

    public AccountScreenViewModel()
    {
        _requests = new Requests();
        AccountLoginFailedTextBlockVisibility = Visibility.Collapsed;

        AccountLoginButton = new AsyncRelayCommand(AccountLogin);
        AccountLoginCreateAccountButton = new RelayCommand(GoToAccountCreationScreen);
        AccountSidebarCreateAccountButton = new RelayCommand(GoToAccountCreationScreen);
        AccountCreationCreateAccountButton = new RelayCommand(CreateAccount);
        AccountCreationCancelButton = new RelayCommand(GoToAccountLoginScreen);
        Requests.OnLoggedIn += OnLoggedIn;
        Requests.OnLoginFailed += OnLoginFailed;
        Requests.OnAccountCreated += OnAccountCreated;
        Requests.OnAccountCreateFailed += OnAccountCreationFailed;
        Views.UserControls.AccountScreen.AccountLogin.OnAutoLogin += OnAutoLogin;

        AccountLoginUsernameWatermark = "Username";
        AccountLoginPasswordWatermark = "Password";
        AccountCreationUsernameWatermark = "Username";
        AccountCreationEmailAddressWatermark = "Email";
        AccountCreationPasswordWatermark = "Password";
        AccountCreationPasswordConfirmationWatermark = "Password Confirmation";
        AccountCreationSecurityQuestionAnswerWatermark = "Answer";
        AccountCreationDiscordWatermark = "Discord ID - e.g. User#1234";

        var config = ConfigFile.GetCurrentServer();

        if (config is not null)
        {
            AccountKeepLoggedInCheckbox = config.AutoLogin;
        }
    }

    private void OnAccountCreated(object? sender, OnAccountCreatedEventArgs args)
    {
        ClearAllTextBoxes();
        ScreenContainerViewModel.EnableScreen(Screen.AccountLogin);
    }

    private void OnAccountCreationFailed(object? sender, OnAccountCreateFailedEventArgs args)
    {
        AccountCreationFailedTextBlockVisibility = Visibility.Visible;
        AccountCreationFailedTextBlock = args.Reason;

        Thread t = new(() =>
        {
            Thread.Sleep(ErrorSleepTime);
            AccountCreationFailedTextBlockVisibility = Visibility.Collapsed;
            AccountCreationFailedTextBlock = "";
        });

        t.Start();
    }

    private void OnLoggedIn(object? sender, OnLoggedInEventArgs args)
    {
        var config = ConfigFile.GetConfig();

        var currentServer = config?.Servers?[config.ActiveServer];

        if (currentServer is not null && !args.AutoLogin)
        {
            currentServer.AutoLogin = AccountKeepLoggedInCheckbox;
            currentServer.Username = AccountLoginUsernameTextBox;
            string password = new System.Net.NetworkCredential(string.Empty, AccountLoginPasswordBox).Password;
            currentServer.Password = password;
        }

        if (config is not null && args.Characters is not null)
        {
            ConfigFile.SaveCharacters(args.Characters, config);
            ConfigFile.SetConfig(config);
        }

        ScreenContainerViewModel.EnableScreen(Screen.Updates);
        ClearAllTextBoxes();

        if (args.Username is not null)
        {
            OnSetUsername?.Invoke(this, new OnSetUsernameEventArgs(args.Username));
        }
    }

    private void OnLoginFailed(object? sender, OnLoginFailedEventArgs args)
    {
        AccountLoginFailedTextBlockVisibility = Visibility.Visible;
        AccountLoginFailedTextBlock = args.Reason;

        Thread t = new(() =>
        {
            Thread.Sleep(ErrorSleepTime);
            AccountLoginFailedTextBlockVisibility = Visibility.Collapsed;
            AccountLoginFailedTextBlock = "";
        });

        t.Start();
    }

    private void GoToAccountCreationScreen()
    {
        ScreenContainerViewModel.EnableScreen(Screen.AccountCreation);
        CurrentScreen = (int)Screen.AccountCreation;
    }

    private async void OnAutoLogin(object? sender, EventArgs args)
    {
        var config = ConfigFile.GetCurrentServer();

        if (config is not null)
        {
            if (config.Username is not null && config.Password is not null)
            {
                await _requests.RequestLogin(config.Username,
                    new System.Net.NetworkCredential(string.Empty, config.Password).Password, true);
            }
        }
    }

    private async Task AccountLogin()
    {
        if (AccountLoginUsernameTextBox is null) return;

        await _requests.RequestLogin(AccountLoginUsernameTextBox,
            new System.Net.NetworkCredential(string.Empty, AccountLoginPasswordBox).Password, false);
    }

    private void ClearAllTextBoxes()
    {
        AccountCreationUsernameTextBox = string.Empty;
        AccountCreationEmailAddressTextBox = string.Empty;
        AccountCreationPasswordBox?.Clear();
        AccountCreationPasswordConfirmationBox?.Clear();
        AccountCreationNewsletterSubscriptionCheckbox = false;
        AccountCreationSecurityQuestionAnswerTextBox = string.Empty;
        AccountCreationDiscordTextBox = string.Empty;
        AccountLoginUsernameTextBox = string.Empty;
        AccountLoginPasswordBox?.Clear();
        AccountKeepLoggedInCheckbox = false;
    }

    private async void CreateAccount()
    {
        var password1 = new System.Net.NetworkCredential(string.Empty, AccountCreationPasswordBox).Password;
        var password2 = new System.Net.NetworkCredential(string.Empty, AccountCreationPasswordConfirmationBox).Password;
        var subscribed = (AccountCreationNewsletterSubscriptionCheckbox == true) ? 1 : 0;

        if (password1 == password2)
        {
            await _requests.RequestAccount(new AccountModel
            {
                username = AccountCreationUsernameTextBox,
                password = password1,
                email = AccountCreationEmailAddressTextBox,
                discord = AccountCreationDiscordTextBox ?? "",
                subscribed = subscribed,
            });
        }
    }

    private void GoToAccountLoginScreen()
    {
        ScreenContainerViewModel.EnableScreen(Screen.AccountLogin);
        CurrentScreen = (int)Screen.AccountLogin;
    }

    internal void WatermarkIntercept(int watermarkType)
    {
        if (watermarkType == (int)WatermarkType.AccountLoginUsername)
        {
            AccountLoginUsernameWatermark =
                (AccountLoginUsernameTextBox!.Length < 1) ? "Username" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountLoginPassword)
        {
            AccountLoginPasswordWatermark = 
                (AccountLoginPasswordBox!.Length < 1) ? "Password" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationUsername)
        {
            AccountCreationUsernameWatermark =
                (AccountCreationUsernameTextBox!.Length < 1) ? "Username" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationEmail)
        {
            AccountCreationEmailAddressWatermark =
                (AccountCreationEmailAddressTextBox!.Length < 1) ? "Email" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationPassword)
        {
            AccountCreationPasswordWatermark =
                (AccountCreationPasswordBox!.Length < 1) ? "Password" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationPasswordConfirmation)
        {
            AccountCreationPasswordConfirmationWatermark =
                (AccountCreationPasswordConfirmationBox!.Length < 1) ? "Password Confirmation" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationSecurityQuestionAnswer)
        {
            AccountCreationSecurityQuestionAnswerWatermark =
                (AccountCreationSecurityQuestionAnswerTextBox!.Length < 1) ? "Answer" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationDiscord)
        {
            AccountCreationDiscordWatermark =
                (AccountCreationDiscordTextBox!.Length < 1) ? "Discord ID - e.g. User#1234" : string.Empty;
        }

        ValidateAllFields();
    }

    private void ValidateAllFields()
    {
        if (AccountCreationUsernameTextBox is not null && AccountCreationUsernameTextBox!.Length > 0 &&
            AccountCreationEmailAddressTextBox is not null && AccountCreationEmailAddressTextBox!.Length > 0 &&
            AccountCreationPasswordBox is not null && AccountCreationPasswordBox!.Length > 0 &&
            AccountCreationPasswordConfirmationBox is not null && AccountCreationPasswordConfirmationBox!.Length > 0 &&
            AccountCreationSecurityQuestionAnswerTextBox is not null && AccountCreationSecurityQuestionAnswerTextBox!.Length > 0)
        {
            CreateAccountButtonToggle = true;
        }
        else
        {
            CreateAccountButtonToggle = false;
        }

        if (AccountLoginUsernameTextBox is not null && AccountLoginUsernameTextBox!.Length > 0 &&
            AccountLoginPasswordBox is not null && AccountLoginPasswordBox!.Length > 0)
        {
            AccountLoginButtonToggle = true;
        }
        else
        {
            AccountLoginButtonToggle = false;
        }
    }

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
            WatermarkIntercept((int)WatermarkType.AccountLoginUsername);
        }
    }

    public SecureString? AccountLoginPasswordBox
    {
        get => _accountLoginPasswordBox;
        set
        {
            SetProperty(ref _accountLoginPasswordBox, value);
            WatermarkIntercept((int)WatermarkType.AccountLoginPassword);
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
            WatermarkIntercept((int)WatermarkType.AccountCreationUsername);
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
            WatermarkIntercept((int)WatermarkType.AccountCreationEmail);
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
            WatermarkIntercept((int)WatermarkType.AccountCreationPassword);
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
            WatermarkIntercept((int)WatermarkType.AccountCreationPasswordConfirmation);
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
            WatermarkIntercept((int)WatermarkType.AccountCreationSecurityQuestionAnswer);
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
            WatermarkIntercept((int)WatermarkType.AccountCreationDiscord);
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

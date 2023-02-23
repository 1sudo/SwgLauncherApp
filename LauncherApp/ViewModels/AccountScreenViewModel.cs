using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using LauncherApp.Models.Properties;
using LibgRPC.Models;

namespace LauncherApp.ViewModels;

internal class AccountScreenViewModel : AccountScreenViewModelProperties
{
    public IAsyncRelayCommand? AccountLoginButton { get; }
    public IRelayCommand? AccountLoginCreateAccountButton { get; }
    public IRelayCommand AccountSidebarCreateAccountButton { get; }
    public IRelayCommand? AccountCreationCreateAccountButton { get; }
    public IRelayCommand? AccountCreationCancelButton { get; }
    private const int ErrorSleepTime = 5000;
    public static Action<string>? SetUsername { get; set; }

    public AccountScreenViewModel()
    {
        AccountLoginFailedTextBlockVisibility = Visibility.Collapsed;

        AccountLoginButton = new AsyncRelayCommand(AccountLogin);
        AccountLoginCreateAccountButton = new RelayCommand(GoToAccountCreationScreen);
        AccountSidebarCreateAccountButton = new RelayCommand(GoToAccountCreationScreen);
        AccountCreationCreateAccountButton = new RelayCommand(CreateAccount);
        AccountCreationCancelButton = new RelayCommand(GoToAccountLoginScreen);
        LibgRPC.Requests.LoggedIn += OnLoggedIn;
        LibgRPC.Requests.LoginFailed += OnLoginFailed;
        LibgRPC.Requests.AccountCreated += OnAccountCreated;
        LibgRPC.Requests.AccountCreationFailed += OnAccountCreationFailed;

        AccountLoginUsernameWatermark = "Username";
        AccountLoginPasswordWatermark = "Password";
        AccountCreationUsernameWatermark = "Username";
        AccountCreationEmailAddressWatermark = "Email";
        AccountCreationPasswordWatermark = "Password";
        AccountCreationPasswordConfirmationWatermark = "Password Confirmation";
        AccountCreationSecurityQuestionAnswerWatermark = "Answer";
        AccountCreationDiscordWatermark = "Discord ID - e.g. User#1234";
    }

    private void OnAccountCreated(string status)
    {
        ClearAllTextBoxes();
        ScreenContainerViewModel.EnableScreen(Screen.AccountLogin);
    }

    private void OnAccountCreationFailed(string status)
    {
        AccountCreationFailedTextBlockVisibility = Visibility.Visible;
        AccountCreationFailedTextBlock = status;

        Thread t = new(() =>
        {
            Thread.Sleep(ErrorSleepTime);
            AccountCreationFailedTextBlockVisibility = Visibility.Collapsed;
            AccountCreationFailedTextBlock = "";
        });

        t.Start();
    }

    private void OnLoggedIn(List<string> characters, string username)
    {
        var config = ConfigFile.GetConfig();

        if (config is not null)
        {
            ConfigFile.SaveCharacters(characters, config);
        }

        ScreenContainerViewModel.EnableScreen(Screen.Updates);
        ClearAllTextBoxes();

        SetUsername?.Invoke(username);
    }

    private void OnLoginFailed(string reason)
    {
        AccountLoginFailedTextBlockVisibility = Visibility.Visible;
        AccountLoginFailedTextBlock = reason;

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

    private async Task AccountLogin()
    {
        if (AccountLoginUsernameTextBox is null) return;

        // Factor in active server (config!.Servers![config.ActiveServer].ServiceAddress)
        // Action calls OnLoggedIn() once authenticated
        await LibgRPC.Requests.RequestLogin(AccountLoginUsernameTextBox, 
            new System.Net.NetworkCredential(string.Empty, AccountLoginPasswordBox).Password);
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
            await LibgRPC.Requests.RequestAccount(new AccountModel
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

    internal static void WatermarkIntercept(AccountScreenViewModelProperties vmp, int watermarkType)
    {
        if (watermarkType == (int)WatermarkType.AccountLoginUsername)
        {
            vmp.AccountLoginUsernameWatermark =
                (vmp.AccountLoginUsernameTextBox!.Length < 1) ? "Username" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountLoginPassword)
        {
            vmp.AccountLoginPasswordWatermark = 
                (vmp.AccountLoginPasswordBox!.Length < 1) ? "Password" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationUsername)
        {
            vmp.AccountCreationUsernameWatermark =
                (vmp.AccountCreationUsernameTextBox!.Length < 1) ? "Username" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationEmail)
        {
            vmp.AccountCreationEmailAddressWatermark =
                (vmp.AccountCreationEmailAddressTextBox!.Length < 1) ? "Email" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationPassword)
        {
            vmp.AccountCreationPasswordWatermark =
                (vmp.AccountCreationPasswordBox!.Length < 1) ? "Password" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationPasswordConfirmation)
        {
            vmp.AccountCreationPasswordConfirmationWatermark =
                (vmp.AccountCreationPasswordConfirmationBox!.Length < 1) ? "Password Confirmation" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationSecurityQuestionAnswer)
        {
            vmp.AccountCreationSecurityQuestionAnswerWatermark =
                (vmp.AccountCreationSecurityQuestionAnswerTextBox!.Length < 1) ? "Answer" : string.Empty;
        }

        if (watermarkType == (int)WatermarkType.AccountCreationDiscord)
        {
            vmp.AccountCreationDiscordWatermark =
                (vmp.AccountCreationDiscordTextBox!.Length < 1) ? "Discord ID - e.g. User#1234" : string.Empty;
        }

        ValidateAllFields(vmp);
    }

    private static void ValidateAllFields(AccountScreenViewModelProperties vmp)
    {
        if (vmp.AccountCreationUsernameTextBox is not null && vmp.AccountCreationUsernameTextBox!.Length > 0 &&
            vmp.AccountCreationEmailAddressTextBox is not null && vmp.AccountCreationEmailAddressTextBox!.Length > 0 &&
            vmp.AccountCreationPasswordBox is not null && vmp.AccountCreationPasswordBox!.Length > 0 &&
            vmp.AccountCreationPasswordConfirmationBox is not null && vmp.AccountCreationPasswordConfirmationBox!.Length > 0 &&
            vmp.AccountCreationSecurityQuestionAnswerTextBox is not null && vmp.AccountCreationSecurityQuestionAnswerTextBox!.Length > 0)
        {
            vmp.CreateAccountButtonToggle = true;
        }
        else
        {
            vmp.CreateAccountButtonToggle = false;
        }

        if (vmp.AccountLoginUsernameTextBox is not null && vmp.AccountLoginUsernameTextBox!.Length > 0 &&
            vmp.AccountLoginPasswordBox is not null && vmp.AccountLoginPasswordBox!.Length > 0)
        {
            vmp.AccountLoginButtonToggle = true;
        }
        else
        {
            vmp.AccountLoginButtonToggle = false;
        }
    }
}

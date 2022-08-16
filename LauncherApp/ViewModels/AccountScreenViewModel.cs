using System;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace LauncherApp.ViewModels
{
    internal class AccountScreenViewModel : AccountScreenViewModelProperties
    {
        public IAsyncRelayCommand? AccountLoginButton { get; }
        public IRelayCommand? AccountLoginCreateAccountButton { get; }
        public IRelayCommand AccountSidebarCreateAccountButton { get; }
        public IRelayCommand? AccountCreationCreateAccountButton { get; }
        public IRelayCommand? AccountCreationCancelButton { get; }

        public AccountScreenViewModel()
        {
            AccountLoginButton = new AsyncRelayCommand(AccountLogin);
            AccountLoginCreateAccountButton = new RelayCommand(GoToAccountCreationScreen);
            AccountSidebarCreateAccountButton = new RelayCommand(GoToAccountCreationScreen);
            AccountCreationCreateAccountButton = new RelayCommand(CreateAccount);
            AccountCreationCancelButton = new RelayCommand(GoToAccountLoginScreen);

            AccountLoginUsernameWatermark = "Username";
            AccountLoginPasswordWatermark = "Password";
            AccountCreationUsernameWatermark = "Username";
            AccountCreationEmailAddressWatermark = "Email";
            AccountCreationPasswordWatermark = "Password";
            AccountCreationPasswordConfirmationWatermark = "Password Confirmation";
            AccountCreationSecurityQuestionAnswerWatermark = "Answer";
            AccountCreationDiscordWatermark = "Discord ID - e.g. User#1234";
        }

        private void GoToAccountCreationScreen()
        {
            ScreenContainerViewModel.EnableScreen(Screen.AccountCreation);
            CurrentScreen = (int)Screen.AccountCreation;
        }

        private async Task AccountLogin()
        {
            ScreenContainerViewModel.EnableScreen(Screen.Updates);
            

            if (AccountLoginUsernameTextBox is null) return;

            Trace.WriteLine($"Login account: {AccountLoginUsernameTextBox}");

            await LibgRPC.Requests.RequestLogin(AccountLoginUsernameTextBox, new System.Net.NetworkCredential(string.Empty, AccountLoginPasswordBox).Password);

            ClearAllTextBoxes();

            /*ConfigFile? config = ConfigFile.GetConfig();

            GameLoginResponseProperties? response = await Models.Handlers.ApiHandler.AccountLoginAsync(config!.Servers![config.ActiveServer].ApiUrl!,
                AccountLoginUsernameTextBox!, AccountLoginPasswordBox!.ToString()!);

            Trace.WriteLine(response!.Result);

            if (response!.Result == "Success")
            {
                ConfigFile.SaveCharacters(response.Characters!, config);
                ClearAllTextBoxes();
            }
            else
            {

            }*/
        }

        private void ClearAllTextBoxes()
        {
            AccountCreationUsernameTextBox = string.Empty;
            AccountCreationEmailAddressTextBox = string.Empty;
            AccountCreationPasswordBox = new SecureString();
            AccountCreationPasswordConfirmationBox = new SecureString();
            AccountCreationNewsletterSubscriptionCheckbox = false;
            AccountCreationSecurityQuestionAnswerTextBox = string.Empty;
            AccountCreationDiscordTextBox = string.Empty;
            AccountLoginUsernameTextBox = string.Empty;
            AccountLoginPasswordBox = new SecureString();
            AccountKeepLoggedInCheckbox = false;
        }

        private void CreateAccount()
        {

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
}

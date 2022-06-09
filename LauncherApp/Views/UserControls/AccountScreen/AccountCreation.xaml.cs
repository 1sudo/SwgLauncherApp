using System.Windows.Controls;
using LauncherApp.ViewModels;

namespace LauncherApp.Views.UserControls.AccountScreen
{
    /// <summary>
    /// Interaction logic for AccountCreation.xaml
    /// </summary>
    public partial class AccountCreation
    {
        public AccountCreation()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                ((AccountScreenViewModel)DataContext).AccountCreationPasswordBox = 
                    ((PasswordBox)sender).SecurePassword;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                ((AccountScreenViewModel)DataContext).AccountCreationPasswordConfirmationBox = 
                    ((PasswordBox)sender).SecurePassword;
            }
        }
    }
}

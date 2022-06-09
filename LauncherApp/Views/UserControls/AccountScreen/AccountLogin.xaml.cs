using System.Windows.Controls;
using LauncherApp.ViewModels;

namespace LauncherApp.Views.UserControls.AccountScreen
{
    /// <summary>
    /// Interaction logic for AccountLogin.xaml
    /// </summary>
    public partial class AccountLogin
    {
        public AccountLogin()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                ((AccountScreenViewModel)DataContext).AccountLoginPasswordBox = 
                    ((PasswordBox)sender).SecurePassword;
            }
        }
    }
}

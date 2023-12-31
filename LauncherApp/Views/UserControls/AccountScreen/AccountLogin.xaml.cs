﻿using System;
using System.Windows.Controls;
using LibLauncherApp.Properties;
using LauncherApp.ViewModels;

namespace LauncherApp.Views.UserControls.AccountScreen;

/// <summary>
/// Interaction logic for AccountLogin.xaml
/// </summary>
public partial class AccountLogin
{
    public static event EventHandler? OnAutoLogin;

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

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        AutoLogin();
    }

    private void AutoLogin()
    {
        var config = ConfigFile.GetCurrentServer();

        if (config is not null && config.Verified)
        {
            if (config.AutoLogin)
            {
                OnAutoLogin?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

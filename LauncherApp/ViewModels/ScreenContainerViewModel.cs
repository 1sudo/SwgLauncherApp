using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using LauncherApp.Models.Properties;

namespace LauncherApp.ViewModels
{
    public enum NextButton
    {
        Rules = 0,
        BaseGameVerification = 1,
        AccountLogin = 2,
        AccountCreate = 3
    }

    public enum Screen
    {
        RulesAndRegulations = 0,
        InstallDirectorySelection = 1,
        BaseGameVerification = 2,
        AccountCreation = 3,
        AccountLogin = 4,
        Updates = 5,
        Settings = 6,
        OptionsAndMods = 7,
        Developer = 8
    }

    internal class ScreenContainerViewModel : ObservableObject
    {
        static List<UserControl>? _controls;

        public ScreenContainerViewModel(List<UserControl>? controls)
        {
            _controls = controls;

            ConfigFile? config = ConfigFile.GetConfig();

            if (config is not null && config.Servers![config.ActiveServer].Verified)
            {
                EnableScreen(Screen.AccountLogin);
            }
            else
            {
                EnableScreen(Screen.RulesAndRegulations);
            }
        }

        private static void DisableAllScreens()
        {
            if (_controls is not null)
            {
                _controls.ForEach(x => x.Visibility = Visibility.Collapsed);
            }
        }

        public static void EnableScreen(Screen screen)
        {
            DisableAllScreens();

            if (_controls is not null)
            {
                switch (screen)
                {
                    case Screen.RulesAndRegulations:
                        _controls[(int)Screen.RulesAndRegulations].Visibility = Visibility.Visible;
                        _controls[9].Visibility = Visibility.Visible;
                        break;
                    case Screen.InstallDirectorySelection:
                        _controls[(int)Screen.InstallDirectorySelection].Visibility = Visibility.Visible;
                        _controls[9].Visibility = Visibility.Visible;
                        break;
                    case Screen.BaseGameVerification:
                        _controls[(int)Screen.BaseGameVerification].Visibility = Visibility.Visible;
                        _controls[9].Visibility = Visibility.Visible;
                        break;
                    case Screen.AccountCreation:
                        _controls[(int)Screen.AccountCreation].Visibility = Visibility.Visible;
                        _controls[10].Visibility = Visibility.Visible;
                        break;
                    case Screen.AccountLogin:
                        _controls[(int)Screen.AccountLogin].Visibility = Visibility.Visible;
                        _controls[10].Visibility = Visibility.Visible;
                        break;
                    case Screen.Updates:
                        _controls[(int)Screen.Updates].Visibility = Visibility.Visible;
                        _controls[11].Visibility = Visibility.Visible;
                        break;
                    case Screen.Settings:
                        _controls[(int)Screen.Settings].Visibility = Visibility.Visible;
                        _controls[11].Visibility = Visibility.Visible;
                        break;
                    case Screen.OptionsAndMods:
                        _controls[(int)Screen.OptionsAndMods].Visibility = Visibility.Visible;
                        _controls[11].Visibility = Visibility.Visible;
                        break;
                    case Screen.Developer:
                        _controls[(int)Screen.Developer].Visibility = Visibility.Visible;
                        _controls[11].Visibility = Visibility.Visible;
                        break;
                }
            }
        }
    }
}

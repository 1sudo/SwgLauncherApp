using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using LauncherApp.Models.Properties;
using LauncherApp.Models.Util;
using LauncherApp.ViewModels;

namespace LauncherApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly NotifyIcon _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();

        MainWindowViewModel mainWindowViewModel = new();
        MainWindowTitleBarUserControl.DataContext = mainWindowViewModel;
        MainWindowMainMenuUserControl.DataContext = mainWindowViewModel;

        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = new Icon(@"legacy.ico");
        _notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(NotifyIcon_MouseDoubleClick!);

        ContextMenuStrip menuStrip = new();

        ToolStripButton exitButton = new() { Text = "Exit" };
        exitButton.Click += (s, e) => Environment.Exit(0);

        ToolStripButton aboutButton = new() { Text = "About" };

        ToolStripLabel versionLabel = new() { Text = "v1.0.0.19" };

        menuStrip.Items.Add(versionLabel);
        menuStrip.Items.Add(new ToolStripSeparator());
        menuStrip.Items.Add(aboutButton);
        menuStrip.Items.Add(exitButton);

        _notifyIcon.ContextMenuStrip = menuStrip;
    }

    private void NotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (TrueExit.Value) return;

        e.Cancel = true;
        WindowState = WindowState.Minimized;
        ShowInTaskbar = true;
        Hide();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Minimized:
                ShowInTaskbar = false;
                _notifyIcon.BalloonTipTitle = "Minimize Successful";
                _notifyIcon.BalloonTipText = "Minimized the app ";
                _notifyIcon.ShowBalloonTip(400);
                _notifyIcon.Visible = true;
                break;
            case WindowState.Normal:
                _notifyIcon.Visible = false;
                ShowInTaskbar = true;
                break;
        }
    }

    private void Window_Initialized(object sender, EventArgs e)
    {
        var procName = Process.GetCurrentProcess().ProcessName;
        var processes = Process.GetProcessesByName(procName);

        if (processes.Length > 1)
        {
            System.Windows.MessageBox.Show("Launcher is already running! Check your taskbar for a running instance of the launcher if it's not visible.", "Launcher Already Running", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(0);
        }
    }

    private async void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Generate manifest file
        if (Keyboard.IsKeyDown(Key.LeftCtrl) &&
            Keyboard.IsKeyDown(Key.LeftShift) &&
            Keyboard.IsKeyDown(Key.F12))
        {
            using var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            string generateFromFolder = "";

            if (result.ToString().Trim() == "Cancel")
            {
                Close();
            }
            else if (result.ToString().Trim() == "OK")
            {
                generateFromFolder = dialog.SelectedPath.Replace("\\", "/");
            }

            try
            {
                await ManifestGenerator.GenerateManifestAsync(generateFromFolder);
            }
            catch { }
        }
    }
}

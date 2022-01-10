using Microsoft.VisualBasic.ApplicationServices;

namespace LauncherApp
{
    public class SingleInstanceApplication : System.Windows.Application
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            // Call the OnStartup event on our base class
            base.OnStartup(e);

            // Create our MainWindow and show it
            MainWindow window = new();
            window.Show();
        }

        public void Activate()
        {
            // Reactivate the main window
            MainWindow.Activate();
        }
    }

    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private SingleInstanceApplication _application;
#pragma warning disable IDE0052 // Remove unread private members
        private System.Collections.ObjectModel.ReadOnlyCollection<string> _commandLine;
#pragma warning restore IDE0052 // Remove unread private members

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SingleInstanceManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs)
        {
            // First time _application is launched
            _commandLine = eventArgs.CommandLine;
            _application = new SingleInstanceApplication();
            _application.Run();
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Subsequent launches
            base.OnStartupNextInstance(eventArgs);
            _commandLine = eventArgs.CommandLine;
            _application.Activate();
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            SingleInstanceManager manager = new();
            manager.Run(args);
        }
    }
}

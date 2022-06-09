using LauncherApp.ViewModels;

namespace LauncherApp.Views.UserControls.SetupScreen
{
    /// <summary>
    /// Interaction logic for Rules.xaml
    /// </summary>
    public partial class Rules
    {
        public Rules()
        {
            InitializeComponent();
            DataContext = new SetupScreenViewModel();
        }
    }
}

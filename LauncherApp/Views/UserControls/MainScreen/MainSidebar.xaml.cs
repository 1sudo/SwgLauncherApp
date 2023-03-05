using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LauncherApp.Views.UserControls.MainScreen
{
    /// <summary>
    /// Interaction logic for MainScreenSidebar.xaml
    /// </summary>
    public partial class MainSidebar : UserControl
    {
        public MainSidebar()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            foreach (InputBinding ib in InputBindings)
            {
                window.InputBindings.Add(ib);
            }
        }
    }
}

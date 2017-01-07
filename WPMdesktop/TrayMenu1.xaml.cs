using System.Windows;

namespace WPMdesktop
{
    /// <summary>
    /// Interaction logic for TrayMenu.xaml
    /// </summary>
    public partial class TrayMenu : Window
    {
        public TrayMenu()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}

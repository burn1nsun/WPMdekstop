using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPMdesktop
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public bool idlePause = true;
        public bool trayMenuEnabled = true;
        public double idleTimerInterval;

        public Settings()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void checkBoxIdlePause_Unchecked(object sender, RoutedEventArgs e)
        {
            idlePause = false;
        }

        private void checkBoxIdlePause_Checked(object sender, RoutedEventArgs e)
        {
            idlePause = true;
        }

        private void trayMenuCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            trayMenuEnabled = true;
        }

        private void trayMenuCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            trayMenuEnabled = false;
        }

        private void idleTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            idleTimerInterval = idleTimeSlider.Value;
        }
    }
}

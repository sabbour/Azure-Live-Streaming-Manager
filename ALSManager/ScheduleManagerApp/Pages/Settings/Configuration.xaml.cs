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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScheduleManagerApp.Pages.Settings
{
    /// <summary>
    /// Interaction logic for SettingsWizard.xaml
    /// </summary>
    public partial class Configuration : UserControl
    {
        public Configuration()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Default.Configured = true;
            AppSettings.Default.Save();
            NavigationCommands.BrowseBack.Execute(null, MainWindow.MainFrame);
        }


    }
}

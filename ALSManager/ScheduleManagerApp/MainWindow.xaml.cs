using FirstFloor.ModernUI.Windows.Controls;
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

namespace ScheduleManagerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        private static ModernFrame _mainFrame;

        public static ModernFrame MainFrame
        {
            get {
                if (_mainFrame == null)
                {
                    var f = Application.Current.MainWindow;
                    _mainFrame = GetDescendantFromName(f, "ContentFrame") as ModernFrame;
                }
                return _mainFrame;
            }
        }


        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (AppSettings.Default.Configured == false)
            {
                // If the app hasn't been configured, go to the Configuration Page
                ContentSource = new Uri("Pages/SettingsPage.xaml", UriKind.RelativeOrAbsolute);
            } 
        }

        /// <summary>
        /// Gets the name of the descendant from.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <returns>Gets a descendant FrameworkElement based on its name.A descendant FrameworkElement with the specified name or null if not found.</returns>
        private static FrameworkElement GetDescendantFromName(DependencyObject parent, string name)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            if (count < 1)
                return null;

            FrameworkElement fe;

            for (int i = 0; i < count; i++)
            {
                fe = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;
                if (fe != null)
                {
                    if (fe.Name == name)
                        return fe;

                    fe = GetDescendantFromName(fe, name);
                    if (fe != null)
                        return fe;
                }
            }

            return null;

        }
    }
}

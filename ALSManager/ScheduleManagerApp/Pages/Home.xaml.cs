using ALSManager.Models;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;
using ScheduleManagerApp.Services;
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

namespace ScheduleManagerApp.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl, IContent
    {
        public Home()
        {
            InitializeComponent();
        }

        public void OnFragmentNavigation(FirstFloor.ModernUI.Windows.Navigation.FragmentNavigationEventArgs e)
        {
            //throw new NotImplementedException();
        }

        public void OnNavigatedFrom(FirstFloor.ModernUI.Windows.Navigation.NavigationEventArgs e)
        {
            //throw new NotImplementedException();
        }

        public async void OnNavigatedTo(FirstFloor.ModernUI.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                using (var channelsService = new ChannelsService())
                {
                    ChannelLoadingProgress.IsActive = true;
                    ChannelsListView.ItemsSource = await channelsService.GetChannels();
                    ChannelLoadingProgress.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
            }
        }

        public void OnNavigatingFrom(FirstFloor.ModernUI.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            var channel = ((sender as Button).DataContext as MediaChannel);
            if (channel.IsScheduleable)
            {
                ScheduleProgress.IsActive = true;
                (sender as Control).IsEnabled = false;
                try
                {
                    using (var archivingService = new ArchivesService())
                    {

                        var schedulerParameters = new SchedulerParameters
                        {
                            ChannelId = channel.Id
                        };

                        // Fire and forget
                        archivingService.CreateArchive(schedulerParameters);
                        ModernDialog.ShowMessage("Scheduling requested. Check back in a few minutes.", "Requested", MessageBoxButton.OK);
                    }
                }
                catch (Exception ex)
                {
                    ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
                    (sender as Control).IsEnabled = true;
                    ScheduleProgress.IsActive = false;
                }
            }
            else
            {
                ModernDialog.ShowMessage("This channel has a schedule running already", "Information", MessageBoxButton.OK);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private async void RefreshChannelsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var channelsService = new ChannelsService())
                {
                    ChannelLoadingProgress.IsActive = true;
                    ChannelsListView.ItemsSource = await channelsService.GetChannels();
                    ChannelLoadingProgress.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
            }
        }
    }
}

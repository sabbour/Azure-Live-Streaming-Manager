using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ALSManager.Services.ScheduleManagerServices;
using System.Configuration;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using ALSManager.Models;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        public ServiceConfiguration ServiceConfiguration { get; set; }
        public UnitTest1()
        {
            ServiceConfiguration = GetServiceConfiguration();
        }

        private ServiceConfiguration GetServiceConfiguration()
        {
            ServiceConfiguration serviceConfiguration = new ServiceConfiguration
            {
                AzureSubscriptionId = Tests.TestSettings.Default.AzureSubscriptionId,
                AADAudience = Tests.TestSettings.Default.AADAudience,
                AADTenant = Tests.TestSettings.Default.AADTenant,
                AADClientId = Tests.TestSettings.Default.AADClientId,
                AADSecret = Tests.TestSettings.Default.AADSecret,
                ManagementCertificateThumbprint = Tests.TestSettings.Default.ManagementCertificateThumbprint,
                MediaServiceAccountName = Tests.TestSettings.Default.MediaServiceAccountName,
                MediaServiceAccountKey = Tests.TestSettings.Default.MediaServiceAccountKey,
                SchedulerRegion = Tests.TestSettings.Default.SchedulerRegion,
                DefaultProgramArchivalWindowMinutes = Tests.TestSettings.Default.DefaultProgramArchivalWindowMinutes,
                ArchivalWindowMinutes = Tests.TestSettings.Default.ArchivalWindowMinutes,
                OverlappingArchivalWindowMinutes = Tests.TestSettings.Default.OverlappingArchivalWindowMinutes,
                ScheduleManagerEndpoint = Tests.TestSettings.Default.ScheduleManagerEndpoint
            };
            return serviceConfiguration;
        }

        [TestMethod]
        public async Task TestAcquireToken()
        {
            var authContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", ServiceConfiguration.AADTenant));
            var clientCredential = new ClientCredential(ServiceConfiguration.AADClientId, ServiceConfiguration.AADSecret);
            var authResult = await authContext.AcquireTokenAsync("http://mediaschedulerdaemon/", clientCredential);
        }

        [TestMethod]
        public async Task TestSetupSchedulerService()
        {
            var cloudServiceName = NamingHelpers.GetSchedulerCloudServiceName(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceLabel = NamingHelpers.GetSchedulerCloudServiceLabel(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceDescription = NamingHelpers.GetSchedulerCloudServiceDescription(ServiceConfiguration.MediaServiceAccountName);
            var jobCollectionName = NamingHelpers.GetSchedulerJobCollectionName(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceRegion = ServiceConfiguration.SchedulerRegion;

            // Create Scheduler Service
            var schedulerService = new SchedulerService(ServiceConfiguration);

            // Create Cloud Service for scheduler (if not exists, otheriwse, returns null)
            var cloudService = await schedulerService.CreateSchedulerCloudServiceIfNotExistsAsync(cloudServiceName, cloudServiceLabel, cloudServiceDescription, cloudServiceRegion);

            // Create a Job Collection (if not exists, otherwise, returns null)
            var jobCollection = await schedulerService.CreateJobCollectionIfNotExistsAsync(cloudServiceName, jobCollectionName);
        }

        [TestMethod]
        public async Task TestCreateChannelAndProgram()
        {
            // Initialize the services
            var channelService = new ChannelsService(ServiceConfiguration);
            var schedulerService = new SchedulerService(ServiceConfiguration);

            // Parameters
            var channelName = "Test1";
            var cloudServiceName = NamingHelpers.GetSchedulerCloudServiceName(ServiceConfiguration.MediaServiceAccountName);
            var jobCollectionName = NamingHelpers.GetSchedulerJobCollectionName(ServiceConfiguration.MediaServiceAccountName);
            var streamingProtocol = StreamingProtocol.RTMP;

            // Create the channel
            var mediaChannel = await channelService.CreateChannelIfNotExistsAsync(NamingHelpers.GetChannelName(channelName), NamingHelpers.GetChannelDescription(channelName), streamingProtocol);

            // Create the DefaultProgram (the one that keeps on running without us touching it)
            var defaultProgram = await channelService.CreateDefaultProgramIfNotExistsAsync(mediaChannel, ServiceConfiguration.DefaultProgramArchivalWindowMinutes);

            // Create the first 1-hour Archival Program
            var archivalProgram = await channelService.CreateArchivalProgramAsync(mediaChannel, DateTime.UtcNow, ServiceConfiguration.ArchivalWindowMinutes);
            var archivalJobName = NamingHelpers.GetArchivingJobName(mediaChannel.Name);

            // Create a job to call the API scheduler endpoint after the archival window has elapsed with the current program Id
            var bodyParameters = new SchedulerParameters
            {
                ChannelId = mediaChannel.Id,
                ProgramId = archivalProgram.Id,
                JobName = archivalJobName
            };

            var headers = new Dictionary<string, string>()
            {
                { "Content-Type", "application/json" },
            };

            var nextProgramJob = await schedulerService.CreateOneTimeHttpJobAsync(
                cloudServiceName,
                jobCollectionName,
                archivalJobName,
                DateTime.UtcNow.AddMinutes(ServiceConfiguration.ArchivalWindowMinutes).AddMinutes(-1 * ServiceConfiguration.OverlappingArchivalWindowMinutes),
                bodyParameters,
                headers,
                "POST",
                ServiceConfiguration.ScheduleManagerEndpoint);
        }

        [TestMethod]
        public async Task TestCleanup()
        {
            var channelService = new ChannelsService(ServiceConfiguration);
            await channelService.DeleteAllChannelsAsync(false);

        }
    }
}

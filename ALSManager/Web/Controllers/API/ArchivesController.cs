using ALSManager.Models;
using ALSManager.Services.ScheduleManagerServices;
using ALSManager.Web.Helpers;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace ALSManager.Web.Controllers.API
{
    public class ArchivesController : BaseAPIController
    {
        private ChannelsService ChannelsService { get; set; }
        private SchedulerService SchedulerService { get; set; }

        public ArchivesController()
        {
            ChannelsService = new ChannelsService(ServiceConfiguration);
            SchedulerService = new SchedulerService(ServiceConfiguration);
        }


        // GET api/archives?channelId=nb:cid:UUID:bf111377-83ac-4907-a5ca-5e8052eeaeef&timestamp=1421934256
        public IEnumerable<ArchiveProgram> Get(string channelId, long timestamp)
        {
            // Convert timestamp into DateTime (UTC based)
            DateTime requestedTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            requestedTime = requestedTime.AddSeconds(timestamp);

            // Retrieve Channel
            var channel = ChannelsService.GetChannel(channelId);

            if (channel == null)
                return null;

            // Convert into our own object
            var returnChannel = Projections.ProjectChannel(channel, ServiceConfiguration.ArchivalWindowMinutes, ServiceConfiguration.OverlappingArchivalWindowMinutes);

            return returnChannel.Programs.Where(p => p.Finished >= requestedTime && p.Started <= requestedTime);
        }


        // POST api/archives
        //[Authorize]
        public HttpResponseMessage Post(SchedulerParameters schedulerParameters)
        {
            System.Diagnostics.Trace.TraceInformation("Archiving requested for Channel ID [{0}], Program ID [{1}], Job Name [{2}]", schedulerParameters.ChannelId, schedulerParameters.ProgramId, schedulerParameters.JobName);
           
            // Fire and forget because otherwise, the scheduler will timeout after 30 seconds
            Task.Run(async () =>
            {
                try
                {
                    var cloudServiceName = NamingHelpers.GetSchedulerCloudServiceName(ServiceConfiguration.MediaServiceAccountName);
                    var jobCollectionName = NamingHelpers.GetSchedulerJobCollectionName(ServiceConfiguration.MediaServiceAccountName);

                    // Ensure that the scheduler is prepared
                    await PrepareScheduler();

                    // Get the Channel by Channel Id
                    var currentChannel = ChannelsService.GetChannel(schedulerParameters.ChannelId);
                    System.Diagnostics.Trace.TraceInformation("Retrieved Channel [{0}] is {1}", schedulerParameters.ChannelId, currentChannel);

                    // Update its cross domain access policy if needed
                    ChannelsService.UpdateCrossSiteAccessPoliciesForChannelIfNeeded(currentChannel);

                    // Identify what are we trying to do
                    var runningPrograms = currentChannel.Programs.ToList().Where(p => p.State == Microsoft.WindowsAzure.MediaServices.Client.ProgramState.Running);
                    var countOfRunningPrograms = runningPrograms.Count();
                    System.Diagnostics.Trace.TraceInformation("Channel [{0}] has [{1}] Running programs", schedulerParameters.ChannelId, countOfRunningPrograms);


                    // If we have more than 2 programs (Default and Archival) running
                    if (countOfRunningPrograms > 2)
                    {
                        // We are in an undefined state
                        System.Diagnostics.Trace.TraceError("Channel [{0}] has [{1}] Running programs. This is an undefined state. Aborting.", schedulerParameters.ChannelId, countOfRunningPrograms);
                        return;
                    }

                    // Now we got this out of the way
                    // If we have no running programs
                    if (countOfRunningPrograms == 0)
                    {
                        // Reset the channel
                        try
                        {

                            System.Diagnostics.Trace.TraceError("Resetting Channel ID [{0}]");
                            await currentChannel.ResetAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceError("Couldn't reset Channel ID [{0}]. \nException: {1}", currentChannel.Id, ex);
                        }

                        System.Diagnostics.Trace.TraceInformation("Starting DefaultProgram");

                        // Maybe we forgot to run the DefaultProgram from the portal, or deleted it
                        // Start by creating the DefaultProgram then create the first ArchivalProgram and calling the scheduler
                        var defaultProgram = await ChannelsService.CreateDefaultProgramIfNotExistsAsync(currentChannel, ServiceConfiguration.DefaultProgramArchivalWindowMinutes);
                    }

                    // Create an Archival Program
                    System.Diagnostics.Trace.TraceInformation("Starting ArchivalProgram");
                    var archivalProgram = await ChannelsService.CreateArchivalProgramAsync(currentChannel, DateTime.UtcNow, ServiceConfiguration.ArchivalWindowMinutes, ServiceConfiguration.OverlappingArchivalWindowMinutes);
                    var archivalJobName = NamingHelpers.GetArchivingJobName(currentChannel.Name);

                    // Schedule the next job run
                    System.Diagnostics.Trace.TraceInformation("Scheduling next job run");
                    var nextProgramJob = await CreateScheduledJob(currentChannel, archivalProgram, archivalJobName);

                    // If we had 2 running programs, then we are trying to schedule the follow-up and toggle the archives
                    if (nextProgramJob != null && countOfRunningPrograms == 2)
                    {
                        System.Diagnostics.Trace.TraceInformation("We had 2 running programs, then we are trying to schedule the follow-up and toggle the archives");
                       
                        // Stop the current (old) program
                        // Get the Program by Program Id
                        System.Diagnostics.Trace.TraceInformation("Getting current program [{0}]", schedulerParameters.ProgramId);
                        var currentProgram = ChannelsService.GetProgram(schedulerParameters.ProgramId);

                        // Stop it
                        System.Diagnostics.Trace.TraceInformation("Stopping current program [{0}]", schedulerParameters.ProgramId);
                        await currentProgram.StopAsync();
                        System.Diagnostics.Trace.TraceInformation("Stopped [{0}]", schedulerParameters.ProgramId);

                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Exception: {0}\n", ex);
                }
            });

            return Request.CreateResponse(HttpStatusCode.OK);
        }


        /// <summary>
        /// Prepares the scheduler
        /// </summary>
        /// <returns></returns>
        private async Task PrepareScheduler()
        {
            var cloudServiceName = NamingHelpers.GetSchedulerCloudServiceName(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceLabel = NamingHelpers.GetSchedulerCloudServiceLabel(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceDescription = NamingHelpers.GetSchedulerCloudServiceDescription(ServiceConfiguration.MediaServiceAccountName);
            var jobCollectionName = NamingHelpers.GetSchedulerJobCollectionName(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceRegion = ServiceConfiguration.SchedulerRegion;

            // Create Scheduler Service
            System.Diagnostics.Trace.TraceInformation("Preparing scheduler for Cloud Service [{0}], Job Collection[{1}]", cloudServiceName, jobCollectionName);
            var schedulerService = new SchedulerService(ServiceConfiguration);

            // Create Cloud Service for scheduler (if not exists, otheriwse, returns null)
            System.Diagnostics.Trace.TraceInformation("Creating Cloud Service for scheduler if it doesn't exist");
            var cloudService = await schedulerService.CreateSchedulerCloudServiceIfNotExistsAsync(cloudServiceName, cloudServiceLabel, cloudServiceDescription, cloudServiceRegion);

            // Create a Job Collection (if not exists, otherwise, returns null)
            System.Diagnostics.Trace.TraceInformation("Creating Job Collection for scheduler if it doesn't exist");
            var jobCollection = await schedulerService.CreateJobCollectionIfNotExistsAsync(cloudServiceName, jobCollectionName);
        }

        /// <summary>
        /// Creates a scheduled job to archive
        /// </summary>
        /// <param name="mediaChannel"></param>
        /// <param name="archivalProgram"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        private async Task<Microsoft.WindowsAzure.Scheduler.Models.JobCreateOrUpdateResponse> CreateScheduledJob(IChannel mediaChannel, IProgram archivalProgram, string jobName)
        {
            var cloudServiceName = NamingHelpers.GetSchedulerCloudServiceName(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceLabel = NamingHelpers.GetSchedulerCloudServiceLabel(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceDescription = NamingHelpers.GetSchedulerCloudServiceDescription(ServiceConfiguration.MediaServiceAccountName);
            var jobCollectionName = NamingHelpers.GetSchedulerJobCollectionName(ServiceConfiguration.MediaServiceAccountName);
            var cloudServiceRegion = ServiceConfiguration.SchedulerRegion;

            var bodyParameters = new SchedulerParameters
            {
                ChannelId = mediaChannel.Id,
                ProgramId = archivalProgram.Id,
                JobName = jobName
            };

            var headers = new Dictionary<string, string>()
            {
                { "Content-Type", "application/json" },
            };

            var nextProgramJob = await SchedulerService.CreateOneTimeHttpJobAsync(
                cloudServiceName,
                jobCollectionName,
                jobName,
                DateTime.UtcNow.AddMinutes(ServiceConfiguration.ArchivalWindowMinutes).AddMinutes(-1 * ServiceConfiguration.OverlappingArchivalWindowMinutes),
                bodyParameters,
                headers,
                "POST",
                ServiceConfiguration.ScheduleManagerEndpoint);

            return nextProgramJob;
        }

    }
}

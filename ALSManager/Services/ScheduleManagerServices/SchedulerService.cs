using Hyak.Common;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Management.Scheduler;
using Microsoft.WindowsAzure.Management.Scheduler.Models;
using Microsoft.WindowsAzure.Scheduler;
using Microsoft.WindowsAzure.Scheduler.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALSManager.Services.ScheduleManagerServices
{
    public class SchedulerService : ServiceBase
    {
        private CloudServiceManagementClient _cloudServiceManagementClient;

        public CloudServiceManagementClient CloudServiceManagementClient
        {
            get { return _cloudServiceManagementClient; }
            set { _cloudServiceManagementClient = value; }
        }

        private SchedulerManagementClient _schedulerManagementClient;

        public SchedulerManagementClient SchedulerManagementClient
        {
            get { return _schedulerManagementClient; }
            set { _schedulerManagementClient = value; }
        }

        public SchedulerService(ServiceConfiguration serviceConfiguration)
            : base(serviceConfiguration)
        {
            CloudServiceManagementClient = new CloudServiceManagementClient(CertificateCloudCredentials);
            SchedulerManagementClient = new SchedulerManagementClient(CertificateCloudCredentials);
            try
            {
                System.Diagnostics.Trace.TraceInformation("Registering Schedluer Resource");
                SchedulerManagementClient.RegisterResourceProvider();
            }
            catch (Hyak.Common.CloudException)
            {
                // Probably it is registered before
                // but TODO: do some logging in case it isn't the expected error
                System.Diagnostics.Trace.TraceInformation("Schedluer Resource already registered");
            }
        }

        /// <summary>
        /// Creates a Scheduler Cloud Service (if not exists, otheriwse, returns null)
        /// </summary>
        /// <param name="cloudServiceName"></param>
        /// <param name="label"></param>
        /// <param name="description"></param>
        /// <param name="email"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public async Task<CloudServiceOperationStatusResponse> CreateSchedulerCloudServiceIfNotExistsAsync(string cloudServiceName, string label, string description, string region)
        {
            bool exists = false;
            try
            {
                var availbilityResponse = await CloudServiceManagementClient.CloudServices.GetAsync(cloudServiceName);
                exists = true;
            }
            catch (CloudException)
            {
                exists = false;
            }

            // If we didn't find it, create it
            if (exists == true)
            {
                return null;
            }
            else {
                // The Cloud Service doesn't exist, we can proceed
                var cloudServiceCreateParameters = new CloudServiceCreateParameters()
                {
                    Description = description,
                    GeoRegion = region,
                    Label = label
                };

                return await CloudServiceManagementClient.CloudServices.CreateAsync(cloudServiceName, cloudServiceCreateParameters);
            }
        }

        /// <summary>
        /// Create a Job Collection (if not exists, otheriwse, returns null)
        /// </summary>
        /// <param name="cloudServiceName"></param>
        /// <param name="jobCollectionName"></param>
        public async Task<SchedulerOperationStatusResponse> CreateJobCollectionIfNotExistsAsync(string cloudServiceName, string jobCollectionName)
        {
            // Check name availability
            var availabilityResponse = await SchedulerManagementClient.JobCollections.CheckNameAvailabilityAsync(cloudServiceName, jobCollectionName);
            if (availabilityResponse.IsAvailable)
            {
                var jobCollectionParameters = new JobCollectionCreateParameters
                    {
                        Label = jobCollectionName,
                        IntrinsicSettings = new JobCollectionIntrinsicSettings()
                        {
                            Plan = JobCollectionPlan.Standard,
                            Quota = new JobCollectionQuota()
                            {
                                MaxJobCount = 50,
                                MaxRecurrence = new JobCollectionMaxRecurrence()
                                {
                                    Frequency = JobCollectionRecurrenceFrequency.Minute,
                                    Interval = 1
                                }
                            }
                        }
                    };

                return await SchedulerManagementClient.JobCollections.CreateAsync(cloudServiceName, jobCollectionName, jobCollectionParameters);
            }
            else return null;
        }

        /// <summary>
        /// Create a Job
        /// </summary>
        /// <param name="cloudServiceName"></param>
        /// <param name="jobCollectionName"></param>
        /// <param name="startTime"></param>
        /// <returns></returns>
        public async Task<JobCreateOrUpdateResponse> CreateOneTimeHttpJobAsync(string cloudServiceName, string jobCollectionName, string jobName, DateTime startTime, object bodyParameters, Dictionary<string, string> headers, string method, string scheduleManagerEndpoint)
        {
            var schedulerClient = new SchedulerClient(cloudServiceName, jobCollectionName, CertificateCloudCredentials);


            var jobCreateParameters = new JobCreateOrUpdateParameters()
            {
                Action = new JobAction()
                {
                    Type = JobActionType.Http,
                    Request = new JobHttpRequest()
                    {
                        Body = JsonConvert.SerializeObject(bodyParameters),
                        Headers = headers,
                        Method = method,
                        Uri = new Uri(scheduleManagerEndpoint + "/api/archives")
                        ,
                        Authentication = new AADOAuthAuthentication()
                        {
                            Tenant = ServiceConfiguration.AADTenant,
                            Audience = ServiceConfiguration.AADAudience,
                            ClientId = ServiceConfiguration.AADClientId,
                            Secret = ServiceConfiguration.AADSecret,
                            Type = HttpAuthenticationType.ActiveDirectoryOAuth
                        }
                    },
                    RetryPolicy = new RetryPolicy()
                    {
                        RetryCount = 2,
                        RetryInterval = TimeSpan.FromMinutes(1),
                        RetryType = RetryType.Fixed
                    }
                },
                StartTime = startTime
            };
            return await schedulerClient.Jobs.CreateOrUpdateAsync(jobName, jobCreateParameters);
        }

    }
}

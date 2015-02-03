using ALSManager.Services.ScheduleManagerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ALSManager.Web.Controllers.API
{
    public class BaseAPIController : ApiController
    {
        public ServiceConfiguration ServiceConfiguration { get; set; }
        private ServiceConfiguration GetServiceConfiguration()
        {

            ServiceConfiguration serviceConfiguration = new ServiceConfiguration
            {
                AzureSubscriptionId = System.Configuration.ConfigurationManager.AppSettings["AzureSubscriptionId"],
                AADAudience = System.Configuration.ConfigurationManager.AppSettings["AADAudience"],
                AADTenant = System.Configuration.ConfigurationManager.AppSettings["AADTenant"],
                AADClientId = System.Configuration.ConfigurationManager.AppSettings["AADClientId"],
                AADSecret = System.Configuration.ConfigurationManager.AppSettings["AADSecret"],
                ManagementCertificateThumbprint = System.Configuration.ConfigurationManager.AppSettings["ManagementCertificateThumbprint"],
                MediaServiceAccountName = System.Configuration.ConfigurationManager.AppSettings["MediaServiceAccountName"],
                MediaServiceAccountKey = System.Configuration.ConfigurationManager.AppSettings["MediaServiceAccountKey"],
                SchedulerRegion = System.Configuration.ConfigurationManager.AppSettings["SchedulerRegion"],
                DefaultProgramArchivalWindowMinutes = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["DefaultProgramArchivalWindowMinutes"]),
                ArchivalWindowMinutes = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["ArchivalWindowMinutes"]),
                OverlappingArchivalWindowMinutes = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["OverlappingArchivalWindowMinutes"]),
                ScheduleManagerEndpoint = System.Configuration.ConfigurationManager.AppSettings["ScheduleManagerEndpoint"]
            };
            return serviceConfiguration;
        }

        public BaseAPIController()
        {
            ServiceConfiguration = GetServiceConfiguration();
        }
    }
}

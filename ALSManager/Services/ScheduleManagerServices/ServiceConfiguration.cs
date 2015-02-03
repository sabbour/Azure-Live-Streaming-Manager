using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALSManager.Services.ScheduleManagerServices
{
    public class ServiceConfiguration
    {
        /// <summary>
        /// Azure Subscription Id
        /// </summary>
        public string AzureSubscriptionId { get; set; }
        /// <summary>
        /// Azure Management Certificate Thumbprint
        /// </summary>
        public string ManagementCertificateThumbprint { get; set; }
        /// <summary>
        /// Media Service Account Name
        /// </summary>
        public string MediaServiceAccountName { get; set; }
        /// <summary>
        /// Media Service Account Key
        /// </summary>
        public string MediaServiceAccountKey { get; set; }
        /// <summary>
        /// Region hosting the Scheduler ex: North Europe, West Europe, etc.
        /// </summary>
        public string SchedulerRegion { get; set; }
        /// <summary>
        /// How many minutes each DefaultProgram will be (to allow VOD rewind)
        /// </summary>
        public int DefaultProgramArchivalWindowMinutes { get; set; }

        /// <summary>
        /// How many minutes each Program will be
        /// </summary>
        public int ArchivalWindowMinutes{ get; set; }

        /// <summary>
        /// How many minutes Programs will overlap, to allow time for Azure to start new programs
        /// </summary>
        public int OverlappingArchivalWindowMinutes { get; set; }

        /// <summary>
        /// Azure AD Audience. Used to authenticate the client against the Web Service that manages the schedule
        /// </summary>
        public string AADAudience { get; set; }

        /// <summary>
        /// Azure AD Tenant. Used to authenticate the client against the Web Service that manages the schedule
        /// </summary>
        public string AADTenant { get; set; }
        /// <summary>
        /// Azure AD Client Id for this scheduler. Used to authenticate the client against the Web Service that manages the schedule
        /// </summary>
        public string AADClientId { get; set; }
        /// <summary>
        /// Azure AD Secret for this scheduler. Used to authenticate the client against the Web Service that manages the schedule
        /// </summary>
        public string AADSecret { get; set; }

        /// <summary>
        /// Service API Endpoint where the API is hosted
        /// </summary>
        public string ScheduleManagerEndpoint { get; set; }
    }
}

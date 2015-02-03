using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure;

namespace ALSManager.Services.ScheduleManagerServices
{
    public class ServiceBase : IDisposable
    {
        private CloudMediaContext _cmContext;

        protected CloudMediaContext CloudMediaContext
        {
            get { return _cmContext; }
            set { _cmContext = value; }
        }

        private MediaServicesCredentials _cachedCredentials;

        protected MediaServicesCredentials CachedCredentials
        {
            get { return _cachedCredentials; }
            set { _cachedCredentials = value; }
        }

        private X509Certificate2 _managementCertificate;

        public X509Certificate2 ManagementCertificate
        {
            get { return _managementCertificate; }
            set { _managementCertificate = value; }
        }

        private CertificateCloudCredentials _certificateCloudCredentials;

        public CertificateCloudCredentials CertificateCloudCredentials
        {
            get { return _certificateCloudCredentials; }
            set { _certificateCloudCredentials = value; }
        }

        private ServiceConfiguration _serviceConfiguration;

        public ServiceConfiguration ServiceConfiguration
        {
            get { return _serviceConfiguration; }
            set { _serviceConfiguration = value; }
        }
        
        
        public ServiceBase(ServiceConfiguration configuration)
        {
            ServiceConfiguration = configuration;
            CachedCredentials = new MediaServicesCredentials(configuration.MediaServiceAccountName, configuration.MediaServiceAccountKey);
            CloudMediaContext = new CloudMediaContext(CachedCredentials);

            // Obtain the Azure Management Certificate
            try
            {
                ManagementCertificate = FindManagementCertificate(configuration.ManagementCertificateThumbprint);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Unable to get a valid Management Certificate with this Thumbprint: {0}. Go to https://msdn.microsoft.com/en-US/library/azure/gg551722.aspx to configure.", configuration.ManagementCertificateThumbprint), e);
            }

            // Create Certificate Cloud Credentials used to manage Azure Account
            try
            {
                CertificateCloudCredentials = new CertificateCloudCredentials(configuration.AzureSubscriptionId, ManagementCertificate);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to get a create valid Certificate Cloud Credentials.", e);
            }

            // Refresh the token
            try
            {
                CloudMediaContext.Credentials.RefreshToken(); 
            }
            catch (Exception e)
            {
                throw new Exception("Unable to authenticate with Azure Media Services. Check connectivity and service keys.",e);
            }
        }

        /// <summary>
        /// Finds a management certificate by thumbprint.
        /// The certificate has to be installed in the My store for the Current User.
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        protected X509Certificate2 FindManagementCertificate(string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false)[0];
            store.Close();
            return certificate;
        }

        public void Dispose()
        {
        }
    }
}

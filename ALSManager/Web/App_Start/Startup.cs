using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Owin;
using System.Configuration;
using Microsoft.Owin.Security.ActiveDirectory;

[assembly: OwinStartup(typeof(ALSManager.Web.Startup))]

namespace ALSManager.Web
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters {
                        ValidAudience = ConfigurationManager.AppSettings["AADAudience"]
                    },
                    Tenant = ConfigurationManager.AppSettings["AADTenant"]
                });
        }
    }
}

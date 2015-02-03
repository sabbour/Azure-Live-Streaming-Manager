using SchedulerManagerApp.Helpers.RESTService.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerManagerApp.Helpers.RESTService
{
    interface IRESTService
    {
        Task<string> GetAccessToken();
        Task<T> GetAsync<T>(string endpoint, HttpValueCollection queryString = null, bool authenticatedRequest = false);
        Task<T> PostAsync<T>(string endpoint, HttpValueCollection queryString = null, object content = null, bool authenticatedRequest = false);
        Task<T> PutAsync<T>(string endpoint, HttpValueCollection queryString = null, object content = null, bool authenticatedRequest = false);
        Task<T> DeleteAsync<T>(string endpoint, HttpValueCollection queryString = null, object content = null, bool authenticatedRequest = false);
    }
}

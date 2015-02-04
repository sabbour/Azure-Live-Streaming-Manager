using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using SchedulerManagerApp.Helpers.RESTService.Utility;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using ScheduleManagerApp;

namespace SchedulerManagerApp.Helpers.RESTService
{
    public class RESTService : IRESTService, IDisposable
    {

        private string _baseAddress;
        private HttpClient _httpClient;

        private HttpClient Client
        {
            get { return _httpClient; }
            set { _httpClient = value; }
        }

        public RESTService(string baseAddress)
        {
            Client = new HttpClient();
            Client.Timeout = TimeSpan.FromMinutes(20);
            _baseAddress = baseAddress;
        }

        /// <summary>
        /// Base method to initiate a request. Throws ServiceException
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="method"></param>
        /// <param name="queryString"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task<T> SendRequestAsync<T>(string endpoint, HttpMethod method, HttpValueCollection queryString, HttpContent content, bool authenticatedRequest = false)
        {
            HttpRequestMessage request = new HttpRequestMessage { Method = method, Content = content };
            if (queryString != null)
                request.RequestUri = new Uri(string.Format("{0}{1}{2}",_baseAddress,endpoint,queryString.ToString()));
            else
                request.RequestUri = new Uri(string.Format("{0}{1}",_baseAddress,endpoint));

            if (authenticatedRequest)
            {
                // Acquire a token somehow. This is left for
                var accessToken = await GetAccessToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko");


            try
            {
                var responseMessage = await _httpClient.SendAsync(request);
                var responseContent = await responseMessage.Content.ReadAsStringAsync();

                if (responseMessage.IsSuccessStatusCode)
                    return await Task.Run<T>(() => JsonConvert.DeserializeObject<T>(responseContent));
                else
                {
                    throw new ServiceException(responseMessage.StatusCode, responseMessage.ToString() + "\n" + responseContent);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine(request.RequestUri + " timed out.");
                return default(T);
                //throw new ServiceException(request.RequestUri + " timed out.");
            }
            catch (Exception e)
            {
                throw new ServiceException(request.RequestUri + ": " + e.Message);
            }
        }

        /// <summary>
        /// Override this method to obtain a Bearer access token
        /// </summary>
        /// <returns>Bearer access token</returns>
        public virtual async Task<string> GetAccessToken()
        {
            var authContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", AppSettings.Default.AADTenant));
            var clientCredential = new ClientCredential(AppSettings.Default.AADClientId, AppSettings.Default.AADSecret);
            var authResult = await authContext.AcquireTokenAsync(AppSettings.Default.AADAppIdUri, clientCredential);
            return authResult.AccessToken;
        }

        /// <summary>
        /// Get from service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(string endpoint, HttpValueCollection queryString = null, bool authenticatedRequest = false)
        {
            return await SendRequestAsync<T>(endpoint, HttpMethod.Get, queryString, null, authenticatedRequest);
        }

        /// <summary>
        /// Post an object to the service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="queryString"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<T> PostAsync<T>(string endpoint, HttpValueCollection queryString = null, object content = null, bool authenticatedRequest = false)
        {
            var json = await Task.Run(() => JsonConvert.SerializeObject(content));
            return await SendRequestAsync<T>(endpoint, HttpMethod.Post, queryString, new StringContent(json, UnicodeEncoding.UTF8, "application/json"), authenticatedRequest);
        }

        /// <summary>
        /// Put an object to the service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="queryString"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<T> PutAsync<T>(string endpoint, HttpValueCollection queryString = null, object content = null, bool authenticatedRequest = false)
        {
            var json = await Task.Run(() => JsonConvert.SerializeObject(content));
            return await SendRequestAsync<T>(endpoint, HttpMethod.Put, queryString, new StringContent(json, UnicodeEncoding.UTF8, "application/json"), authenticatedRequest);
        }

        /// <summary>
        /// Delete an object to the service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="queryString"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<T> DeleteAsync<T>(string endpoint, HttpValueCollection queryString = null, object content = null, bool authenticatedRequest = false)
        {
            var json = await Task.Run(() => JsonConvert.SerializeObject(content));
            return await SendRequestAsync<T>(endpoint, HttpMethod.Delete, queryString, new StringContent(json, UnicodeEncoding.UTF8, "application/json"), authenticatedRequest);
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}

using ALSManager.Models;
using SchedulerManagerApp.Helpers.RESTService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleManagerApp.Services
{
    public class ChannelsService : RESTService
    {
        public ChannelsService() : base(AppSettings.Default.ScheduleManagerEndpoint)
        {

        }

        public async Task<IEnumerable<MediaChannel>> GetChannels()
        {
            return await this.GetAsync<IEnumerable<MediaChannel>>("api/channels", null, false);
        }

        public async Task<MediaChannel> GetChannel(string id)
        {
            var queryString = new SchedulerManagerApp.Helpers.RESTService.Utility.HttpValueCollection("?id=" + id,true);
            return await this.GetAsync<MediaChannel>("api/channels", queryString, false);
        }
    }
}

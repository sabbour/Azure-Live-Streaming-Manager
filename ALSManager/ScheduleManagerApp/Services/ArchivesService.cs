using ALSManager.Models;
using SchedulerManagerApp.Helpers.RESTService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleManagerApp.Services
{
    public class ArchivesService : RESTService
    {
        public ArchivesService()
            : base(AppSettings.Default.ScheduleManagerEndpoint)
        {

        }

        public async Task<IEnumerable<ArchiveProgram>> GetArchives(string channnelId, long timestamp)
        {
            var queryString = new SchedulerManagerApp.Helpers.RESTService.Utility.HttpValueCollection("?channelId=" + channnelId + "&timestamp=" + timestamp, true);
            return await this.GetAsync<IEnumerable<ArchiveProgram>>("api/archives", queryString, false);
        }

        public async Task CreateArchive(SchedulerParameters schedulerParameters)
        {
            await this.PostAsync<SchedulerParameters>("api/archives", null, schedulerParameters, true);
        }
    }
}

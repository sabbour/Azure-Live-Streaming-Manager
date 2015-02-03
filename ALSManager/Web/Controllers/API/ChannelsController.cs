using ALSManager.Models;
using ALSManager.Services.ScheduleManagerServices;
using ALSManager.Web.Helpers;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ALSManager.Web.Controllers.API
{
    public class ChannelsController : BaseAPIController
    {
        private ChannelsService ChannelsService { get; set; }

        public ChannelsController()
        {
            ChannelsService = new ChannelsService(ServiceConfiguration);
        }

        // GET: api/channels
        public IEnumerable<MediaChannel> Get()
        {
            var channels = ChannelsService.GetChannels();
            var returnChannels = new List<MediaChannel>();
            foreach (var channel in channels)
            {
                returnChannels.Add(Projections.ProjectChannel(channel,ServiceConfiguration.ArchivalWindowMinutes));
            }
            return returnChannels;
        }

        // GET: api/channels/nb:cid:UUID:bf111377-83ac-4907-a5ca-5e8052eeaeef
        public MediaChannel Get(string id)
        {
            // Retrieve Channel
            var channel = ChannelsService.GetChannel(id);
            if (channel == null)
                return null;

            else
                return Projections.ProjectChannel(channel, ServiceConfiguration.ArchivalWindowMinutes);
        }
    
    }
}

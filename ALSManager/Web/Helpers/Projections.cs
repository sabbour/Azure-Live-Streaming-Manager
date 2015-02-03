using ALSManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace ALSManager.Web.Helpers
{
    public class Projections
    {
        public static ALSManager.Models.MediaChannel ProjectChannel(Microsoft.WindowsAzure.MediaServices.Client.IChannel channel, int archivalWindowMinutes)
        {
            var programs = channel.Programs.ToList();
            MediaChannel returnChannel = null;
            returnChannel = new MediaChannel
            {
                Id = channel.Id,
                Name = channel.Name,
                State = ProjectChannelState(channel.State),
                Programs = programs.OrderByDescending(p => p.Created).Select(
                p => new ArchiveProgram
                {
                    Id = p.Id,
                    Name = p.Name,
                    Channel = returnChannel,
                    State = ProjectProgramState(p.State),
                    Started = p.Created,
                    Finished = p.Created.AddMinutes(archivalWindowMinutes),
                    SmoothStreamingUrl = p.Asset.GetSmoothStreamingUri() != null ? p.Asset.GetSmoothStreamingUri().ToString() : "",
                    DashUrl = p.Asset.GetMpegDashUri() != null ? p.Asset.GetMpegDashUri().ToString() : "",
                    HLSUrl = p.Asset.GetHlsUri()!= null ? p.Asset.GetHlsUri().ToString() : ""                    
                })
            };
            return returnChannel;
        }

        public static ALSManager.Models.ProgramState ProjectProgramState(Microsoft.WindowsAzure.MediaServices.Client.ProgramState programState)
        {
            switch (programState)
            {
                case Microsoft.WindowsAzure.MediaServices.Client.ProgramState.Running:
                    return ALSManager.Models.ProgramState.Running;
                case Microsoft.WindowsAzure.MediaServices.Client.ProgramState.Starting:
                    return ALSManager.Models.ProgramState.Starting;
                case Microsoft.WindowsAzure.MediaServices.Client.ProgramState.Stopped:
                    return ALSManager.Models.ProgramState.Stopped;
                case Microsoft.WindowsAzure.MediaServices.Client.ProgramState.Stopping:
                    return ALSManager.Models.ProgramState.Stopping;
                default:
                    return ALSManager.Models.ProgramState.Running;
            }
        }

        public static ALSManager.Models.ChannelState ProjectChannelState(Microsoft.WindowsAzure.MediaServices.Client.ChannelState channelState)
        {
            switch (channelState)
            {
                case Microsoft.WindowsAzure.MediaServices.Client.ChannelState.Running:
                    return ALSManager.Models.ChannelState.Running;
                case Microsoft.WindowsAzure.MediaServices.Client.ChannelState.Starting:
                    return ALSManager.Models.ChannelState.Starting;
                case Microsoft.WindowsAzure.MediaServices.Client.ChannelState.Stopped:
                    return ALSManager.Models.ChannelState.Stopped;
                case Microsoft.WindowsAzure.MediaServices.Client.ChannelState.Stopping:
                    return ALSManager.Models.ChannelState.Stopping;
                case Microsoft.WindowsAzure.MediaServices.Client.ChannelState.Deleting:
                    return ALSManager.Models.ChannelState.Deleting;
                default:
                    return ALSManager.Models.ChannelState.Running;
            }
        }
    }
}
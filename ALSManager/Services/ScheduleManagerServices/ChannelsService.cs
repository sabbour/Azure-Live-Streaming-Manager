using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ALSManager.Services.ScheduleManagerServices
{
    public class ChannelsService : ServiceBase
    {

        IPRange allowAllIPRange = new IPRange
                         {
                             Name = "Allow All",
                             Address = IPAddress.Parse("0.0.0.0"),
                             SubnetPrefixLength = 0
                         };

        public ChannelsService(ServiceConfiguration serviceConfiguration)
            : base(serviceConfiguration)
        {

        }

        /// <summary>
        /// Creates an Access Policy to be applied on Locators
        /// </summary>
        /// <param name="name"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public async Task<IAccessPolicy> CreateAccessPolicyAsync(string name, TimeSpan duration)
        {
            var accessPolicy = await CloudMediaContext.AccessPolicies.CreateAsync
            (
                name,
                duration,
                AccessPermissions.Read
            );
            return accessPolicy;
        }

        /// <summary>
        /// Returns an Access Policy by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IAccessPolicy GetAccessPolicy(string name)
        {
            var accessPolicy = CloudMediaContext.AccessPolicies.Where(ap => ap.Name == name).FirstOrDefault();
            return accessPolicy;
        }

        /// <summary>
        /// Creates a Channel and starts it
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="streamingProtocol"></param>
        /// <returns></returns>
        public async Task<IChannel> CreateChannelIfNotExistsAsync(string name, string description, StreamingProtocol streamingProtocol)
        {
            IChannel channel;

            // Check if a channel with that name exists
            channel = GetChannelByName(name);

            if (channel == null)
            {
                // Add channel to Azure Media Service
                channel = await CloudMediaContext.Channels.CreateAsync(
                    new ChannelCreationOptions
                    {
                        Name = name,
                        Description = description,
                        Input = CreateChannelInput(streamingProtocol),
                        Preview = CreateChannelPreview(),
                        Output = CreateChannelOutput()
                    });
            }

            // Set Cross Domain policy as needed
            UpdateCrossSiteAccessPoliciesForChannelIfNeeded(channel);

            // Start the channel
            if (channel.State != ChannelState.Starting || channel.State != ChannelState.Running)
                await channel.StartAsync();

            // Return channel
            return channel;
        }

        /// <summary>
        /// Creates the DefaultProgram 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="archivalWindowMinutes"></param>
        /// <returns></returns>
        public async Task<IProgram> CreateDefaultProgramIfNotExistsAsync(IChannel channel, int archivalWindowMinutes = 360)
        {
            // Default Program details (the one that keeps on running without us touching it)
            var defaultProgram_StartTime = DateTime.UtcNow;
            var defaultProgram_ArchivalWindow = TimeSpan.FromMinutes(archivalWindowMinutes);
            var defaultProgram_EndTime = defaultProgram_StartTime.Add(defaultProgram_ArchivalWindow);
            var defaultProgram_programName = NamingHelpers.GetDefaultProgramName(channel.Name); ;
            var defaultProgram_programDescription = NamingHelpers.GetDefaultProgramDescription(channel.Name);

            // Create DefaultProgram
            return await CreateProgramIfNotExistsAsync(channel, defaultProgram_programName, defaultProgram_programDescription, defaultProgram_ArchivalWindow);
        }

        /// <summary>
        /// Creates an Archival program
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="startTime"></param>
        /// <param name="archivalWindowMinutes"></param>
        /// <returns></returns>
        public async Task<IProgram> CreateArchivalProgramAsync(IChannel channel, DateTime startTime, int archivalWindowMinutes = 60, int overlappingArchivalWindowMinutes = 2)
        {
            // First segment Archival Program (the one that records in the background for VOD playback)
            var archivalProgram_StartTime = startTime;
            var archivalProgram_ArchivalWindow = TimeSpan.FromMinutes(archivalWindowMinutes);
            var archivalProgram_EndTime = archivalProgram_StartTime.Add(archivalProgram_ArchivalWindow).AddMinutes(-1 * overlappingArchivalWindowMinutes);
            var archivalProgram_programName = NamingHelpers.GetArchivingProgramName(archivalProgram_StartTime);
            var archivalProgram_programDescription = NamingHelpers.GetArchivingProgramDescription(channel.Name, archivalProgram_StartTime, archivalProgram_EndTime);

            // Create the Archiving Program
            return await CreateProgramIfNotExistsAsync(channel, archivalProgram_programName, archivalProgram_programDescription, archivalProgram_ArchivalWindow);
        }

        /// <summary>
        /// Creates a Program 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="archiveWindowLength"></param>
        /// <returns></returns>
        public async Task<IProgram> CreateProgramIfNotExistsAsync(IChannel channel, string name, string description, TimeSpan archiveWindowLength)
        {

            System.Diagnostics.Trace.TraceInformation("Creating Program [{0}] for Channel ID [{1}] with Archival Window [{2}] minutes", name, channel.Id, archiveWindowLength.TotalMinutes);

            IProgram program;
            IAsset asset;
            ILocator locator;
            var assetName = NamingHelpers.GetArchivingAssetName(channel.Name, name);

            // Try to find an asset with this name
            System.Diagnostics.Trace.TraceInformation("Trying to find existing Archiving asset [{0}]", assetName);

            asset = GetAssetByName(assetName);

            if (asset == null)
            {
                System.Diagnostics.Trace.TraceInformation("Archiving asset [{0}] doesn't exist. Create it.", assetName);
                // Create output asset for the program (archive)
                asset = CloudMediaContext.Assets.Create(assetName, AssetCreationOptions.None);
                System.Diagnostics.Trace.TraceInformation("Archiving asset [{0}] Created [{1}]", assetName, asset.Id);
            }

            // Try to find this program first before creating it
            System.Diagnostics.Trace.TraceInformation("Trying to find existing Program [{0}]", name);
            program = GetProgramByName(channel, name);

            if (program == null)
            {
                System.Diagnostics.Trace.TraceInformation("Program [{0}] doesn't exist. Create it.", name);
                // Add program to Azure Media Service channel
                program = await channel.Programs.CreateAsync(
                    new ProgramCreationOptions
                    {
                        Name = name,
                        Description = description,
                        ArchiveWindowLength = archiveWindowLength,
                        AssetId = asset.Id
                    });
                System.Diagnostics.Trace.TraceInformation("Program [{0}] Created [{1}]", name, program.Id);
            }

            // Start the Program if it isn't already running
            System.Diagnostics.Trace.TraceInformation("Start Program [{0}] if it isn't running", name);
            if (program.State != ProgramState.Starting || program.State != ProgramState.Running)
                await program.StartAsync();

            // Create (or retrieve access policy)
            System.Diagnostics.Trace.TraceInformation("Get Access Policy [{0}]", "100 years Read Access Policy");
            var accessPolicy = GetAccessPolicy("100 years Read Access Policy");
            if (accessPolicy == null)
            {
                System.Diagnostics.Trace.TraceInformation("Access Policy [{0}] doesn't exist. Create it.", "100 years Read Access Policy");
                accessPolicy = await CreateAccessPolicyAsync("100 years Access Policy", TimeSpan.FromDays(3650));
                System.Diagnostics.Trace.TraceInformation("Access Policy [{0}] Created [{1}]", accessPolicy.Name, accessPolicy.Id);
            }
            // Try to find a Locator for the asset before creating it
            System.Diagnostics.Trace.TraceInformation("Finding OnDemandOrigin Locator for Asset ID [{0}]", asset.Id);
            locator = GetLocator(asset.Id, LocatorType.OnDemandOrigin);

            if (locator == null)
            {
                System.Diagnostics.Trace.TraceInformation("Locator for Asset ID [{0}] doesn't exist. Create it.", asset.Id);

                // Create a locator for the Asset in the Program
                locator = await CloudMediaContext.Locators.CreateLocatorAsync
                (
                    LocatorType.OnDemandOrigin,
                    asset,
                   accessPolicy
                );

                System.Diagnostics.Trace.TraceInformation("Locator for Asset ID [{0}] Created [{1}].", asset.Id, locator.Id);
            }

            // Return program
            return program;
        }

        /// <summary>
        /// Returns a Media Service Program
        /// </summary>
        /// <param name="programId"></param>
        /// <returns></returns>
        public IProgram GetProgram(string programId)
        {
            var program = CloudMediaContext.Programs
                                    .Where(p => p.Id == programId)
                                    .FirstOrDefault();
            return program;
        }

        /// <summary>
        /// Returns a Media Service Program
        /// </summary>
        /// <param name="programId"></param>
        /// <returns></returns>
        public IProgram GetProgramByName(IChannel channel, string programName)
        {
            var program = CloudMediaContext.Programs
                                    .Where(p => p.ChannelId == channel.Id && p.Name == programName)
                                    .FirstOrDefault();
            return program;
        }

        /// <summary>
        /// Gets Asset by Name
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public IAsset GetAssetByName(string assetName)
        {
            var asset = CloudMediaContext.Assets
                                    .Where(a => a.Name == assetName)
                                    .FirstOrDefault();
            return asset;
        }

        /// <summary>
        /// Gets Locator for Asset
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="locatorType"></param>
        /// <returns></returns>
        public ILocator GetLocator(string assetId, LocatorType locatorType)
        {
            var locator = CloudMediaContext.Locators
                                    .Where(l => l.AssetId == assetId && l.Type == locatorType)
                                    .FirstOrDefault();
            return locator;
        }


        /// <summary>
        /// Deletes all channels and their programs, optionally deleting its archived content as well.
        /// </summary>
        /// <param name="keepArchives"></param>
        /// <returns></returns>
        public async Task DeleteAllChannelsAsync(bool keepArchives = true)
        {
            var mediaChannels = GetChannels();
            foreach (var channel in mediaChannels)
            {
                await DeleteChannelAsync(channel.Id, keepArchives);
            }
        }

        /// <summary>
        /// Deletes a channel and its programs, optionally deleting its archived content as well.
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="keepArchives"></param>
        /// <returns></returns>
        public async Task DeleteChannelAsync(string channelId, bool keepArchives = true)
        {
            IAsset asset;

            var channel = GetChannel(channelId);
            if (channel != null)
            {
                foreach (var program in channel.Programs)
                {
                    asset = program.Asset;

                    if (program.State == ProgramState.Running)
                        await program.StopAsync();

                    await program.DeleteAsync();

                    if (asset != null && keepArchives == false)
                    {
                        foreach (var l in asset.Locators)
                            await l.DeleteAsync();

                        await asset.DeleteAsync();
                    }
                }

                if (channel.State == ChannelState.Running)
                    await channel.StopAsync();

                await channel.DeleteAsync();
            }
        }

        /// <summary>
        /// Returns a Media Service Channel
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public IChannel GetChannel(string channelId)
        {
            var channel = CloudMediaContext.Channels
                                    .Where(c => c.Id == channelId)
                                    .FirstOrDefault();
            return channel;
        }

        /// <summary>
        /// Returns a Media Service Channel by Name
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public IChannel GetChannelByName(string channelName)
        {
            var channel = CloudMediaContext.Channels
                                    .Where(c => c.Name == channelName)
                                    .FirstOrDefault();
            return channel;
        }

        /// <summary>
        /// Returns all Media Service Channels
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IChannel> GetChannels()
        {
            var channels = CloudMediaContext.Channels;
            return channels;
        }

        public void UpdateCrossSiteAccessPoliciesForChannelIfNeeded(IChannel channel)
        {

            try
            {
                var clientPolicy =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
    <access-policy>
        <cross-domain-access>
            <policy>
                <allow-from http-request-headers=""*"" http-methods=""*"">
                    <domain uri=""*""/>
                </allow-from>
                <grant-to>
                    <resource path=""/"" include-subpaths=""true""/>
                </grant-to>
            </policy>
        </cross-domain-access>
    </access-policy>";

                var xdomainPolicy =
                    @"<?xml version=""1.0"" ?>
    <cross-domain-policy>
        <allow-access-from domain=""*"" />
    </cross-domain-policy>";

                if (channel.CrossSiteAccessPolicies.ClientAccessPolicy == null)
                    channel.CrossSiteAccessPolicies.ClientAccessPolicy = clientPolicy;

                if (channel.CrossSiteAccessPolicies.CrossDomainPolicy == null)
                    channel.CrossSiteAccessPolicies.CrossDomainPolicy = xdomainPolicy;

                channel.Update();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Unable to update cross site access policy for Channel ID [{0}].\nException: {1}", channel.Id, ex);
            }
        }

        #region Private functions
        /// <summary>
        /// Creates an Ingest channel
        /// </summary>
        /// <param name="ipRangeName"></param>
        /// <returns></returns>
        private ChannelInput CreateChannelInput(StreamingProtocol streamingProtocol)
        {
            return new ChannelInput
            {
                StreamingProtocol = streamingProtocol,
                AccessControl = new ChannelAccessControl
                {
                    IPAllowList = new List<IPRange>
                    {
                        allowAllIPRange
                    }
                }
            };
        }

        /// <summary>
        /// Creates a Preview channel
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        private ChannelPreview CreateChannelPreview()
        {
            return new ChannelPreview
            {
                AccessControl = new ChannelAccessControl
                {
                    IPAllowList = new List<IPRange>
                    {
                        allowAllIPRange
                    }
                }
            };
        }

        /// <summary>
        /// Creates an Output chanel
        /// </summary>
        /// <returns></returns>
        private ChannelOutput CreateChannelOutput()
        {
            return new ChannelOutput
            {
                Hls = new ChannelOutputHls { FragmentsPerSegment = 1 }, // HLS specific settings
            };
        }
        #endregion


    }

}

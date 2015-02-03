using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALSManager.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChannelState
    {
        Stopped = 0,
        Starting = 1,
        Running = 2,
        Stopping = 3,
        Deleting = 4,
    }

    [JsonObject(IsReference = true)]
    public class MediaChannel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ChannelState State { get; set; }
        public IEnumerable<ArchiveProgram> Programs { get; set; }
        public bool IsScheduleable
        {
            get
            {
                return Programs.Where(p => p.State == ProgramState.Running).Count() < 2;
            }
        }
    }
}

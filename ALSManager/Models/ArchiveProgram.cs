using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ALSManager.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProgramState
    {
        Stopped = 0,
        Starting = 1,
        Running = 2,
        Stopping = 3,
    }

    [JsonObject(IsReference = true)]
    public class ArchiveProgram
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime Started { get; set; }
        public DateTime Finished { get; set; }
        public double DurationInSeconds
        {
            get
            {
                return (Finished - Started).TotalSeconds;
            }
        }
        public ProgramState State { get; set; }
        public string SmoothStreamingUrl { get; set; }
        public string DashUrl { get; set; }
        public string HLSUrl { get; set; }

        [JsonIgnore]
        public MediaChannel Channel { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ALSManager.Services.ScheduleManagerServices
{
    public static class NamingHelpers
    {
        public static string GetSchedulerCloudServiceName(string mediaServiceAccountName)
        {
            return string.Format("{0}-media-scheduler-service", mediaServiceAccountName.ToSlug());
        }
        public static string GetSchedulerCloudServiceLabel(string mediaServiceAccountName)
        {
            return string.Format("{0}-media-scheduler-service", mediaServiceAccountName.ToSlug());
        }
        public static string GetSchedulerCloudServiceDescription(string mediaServiceAccountName)
        {
            return string.Format("Scheduler service for {0} Media Services Account", mediaServiceAccountName);
        }
        public static string GetSchedulerJobCollectionName(string mediaServiceAccountName)
        {
            return string.Format("{0}-media-scheduler-job-collection", mediaServiceAccountName.ToSlug());
        }
        public static string GetChannelName(string channelName)
        {
            return string.Format("{0}", channelName.ToSlug());
        }
        public static string GetChannelDescription(string channelName)
        {
            return string.Format("{0} Live Streaming channel", channelName);
        }
        public static string GetDefaultProgramName(string channelName)
        {
            return "DefaultProgram";
        }
        public static string GetDefaultProgramDescription(string channelName)
        {
            return string.Format("Live Archive for {0}. Do not stop.", channelName);
        }
        public static string GetArchivingProgramName(DateTime startTime)
        {
            return string.Format("ArchiveProgram-{0:yyyy-MM-dd hh:mmK}", startTime).Replace(":", "-").Trim();
        }
        public static string GetArchivingProgramDescription(string channelName, DateTime startTime, DateTime endTime)
        {
            return string.Format("VOD Archive for {0} from {1:yyyy-MM-dd hh:mmK} to {2:yyyy-MM-dd hh:mmK}", channelName, startTime, endTime);
        }
        public static string GetArchivingAssetName(string channelName, string programName)
        {
            return string.Format("{0}-{1}-asset", channelName.ToSlug(), programName.ToSlug());
        }
        public static string GetArchivingJobName(string channelName)
        {
            return string.Format("{0}-channel-archive-job", channelName.ToSlug());
        }

        private static String ToSlug(this string text)
        {
            String value = text.Normalize(NormalizationForm.FormD).Trim();
            StringBuilder builder = new StringBuilder();

            foreach (char c in text.ToCharArray())
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);

            value = builder.ToString();

            byte[] bytes = Encoding.GetEncoding("Cyrillic").GetBytes(text);

            value = Regex.Replace(Regex.Replace(Encoding.ASCII.GetString(bytes), @"\s{2,}|[^\w]", " ", RegexOptions.ECMAScript).Trim(), @"\s+", "-");

            return value.ToLowerInvariant();
        }

    }
}

using CEK.CSharp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClovaDurableSessionSample
{
    public class AudioPayload
    {
        [JsonProperty("audioItem")]
        public AudioItem AudioItem { get; set; }
        [JsonProperty("source")]
        public Source Source { get; set; }
        [JsonProperty("playBehavior")]
        public string PlayBehavior { get; set; }
    }

    public class AudioItem
    {
        [JsonProperty("audioItemId")]
        public string AudioItemId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("artist")]
        public string Artist { get; set; }
        [JsonProperty("stream")]
        public Stream Stream { get; set; }
    }

    public class Stream
    {
        [JsonProperty("beginAtInMilliseconds")]
        public int BeginAtInMilliseconds { get; set; }
        [JsonProperty("episodeId")]
        public int EpisodeId { get; set; }
        [JsonProperty("playType")]
        public string PlayType { get; set; }
        [JsonProperty("podcastId")]
        public int PodcastId { get; set; }
        [JsonProperty("progressReport")]
        public ProgressReport ProgressReport { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("urlPlayable")]
        public bool UrlPlayable { get; set; }
    }

    public class ProgressReport
    {
        [JsonProperty("progressReportDelayInMilliseconds")]
        public object ProgressReportDelayInMilliseconds { get; set; }
        [JsonProperty("progressReportIntervalInMilliseconds")]
        public object ProgressReportIntervalInMilliseconds { get; set; }
        [JsonProperty("progressReportPositionInMilliseconds")]
        public object ProgressReportPositionInMilliseconds { get; set; }
    }

    public class Source
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("logoUrl")]
        public string LogoUrl { get; set; }
    }
}

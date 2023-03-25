namespace Dolby.Millicast
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    public partial class SimulcastInfo
    {
        [JsonProperty("active")]
        public SimulcastData[] Active { get; set; }

        [JsonProperty("inactive")]
        public SimulcastData[] Inactive { get; set; }

        [JsonProperty("layers")]
        public Layer[] Layers { get; set; }
    }

    public partial class SimulcastData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("simulcastIdx")]
        public long SimulcastIdx { get; set; }

        [JsonProperty("bitrate")]
        public long Bitrate { get; set; }

        [JsonProperty("layers")]
        public Layer[] Layers { get; set; }
    }

    public partial class Layer
    {
        [JsonProperty("simulcastIdx")]
        public long SimulcastIdx { get; set; }

        [JsonProperty("spatialLayerId")]
        public long SpatialLayerId { get; set; }

        [JsonProperty("temporalLayerId")]
        public long TemporalLayerId { get; set; }

        [JsonProperty("bitrate")]
        public long Bitrate { get; set; }

        [JsonProperty("encodingId", NullValueHandling = NullValueHandling.Ignore)]
        public string EncodingId { get; set; }
    }
    public class DataContainer
    {
        public static SimulcastInfo ParseSimulcastLayers(object mediaData)
        {
            var jsonObject = mediaData as JObject;
            var simulcastData = jsonObject.SelectToken("0").ToString();
            SimulcastInfo info = JsonConvert.DeserializeObject<SimulcastInfo>(simulcastData);
            return info;
        }
    }
}

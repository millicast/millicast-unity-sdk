namespace Dolby.Millicast {
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using System.Collections.Generic;
  using UnityEngine;

  /// <summary>
  /// Holds track information for multisource  
  /// </summary>
  public partial class Track {
    [JsonProperty("trackId")]
    public string TrackId { get; set; }
    [JsonProperty("media")]
    public string Media { get; set; }
  }

  public partial class PublisherActiveEvent {
    [JsonProperty("streamId")]
    public string StreamId { get; set; }
    [JsonProperty("sourceId")]
    public string SourceId { get; set; }
    [JsonProperty("tracks")]
    public Track[] Tracks;
  }

  public partial class PublisherInctiveEvent {
    [JsonProperty("streamId")]
    public string StreamId { get; set; }
    [JsonProperty("sourceId")]
    public string SourceId { get; set; }
  }

  public partial class SimulcastInfo {
    [JsonProperty("active")]
    public SimulcastData[] Active { get; set; }

    [JsonProperty("inactive")]
    public SimulcastData[] Inactive { get; set; }

    [JsonProperty("layers")]
    public Layer[] Layers { get; set; }
  }

  public partial class SimulcastData {
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("simulcastIdx")]
    public long SimulcastIdx { get; set; }

    [JsonProperty("bitrate")]
    public long Bitrate { get; set; }

    [JsonProperty("layers")]
    public Layer[] Layers { get; set; }
  }

  public partial class Layer {
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

  internal class ServiceResponse {
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }
    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public JToken Data { get; set; }
    [JsonProperty("transId", NullValueHandling = NullValueHandling.Ignore)]
    public string TransactionId { get; set; }
  }

  internal class LayerEvent {
    [JsonProperty("medias")]
    public Dictionary<string, SimulcastInfo> Medias;
  }

  internal class DataContainer {
    public static SimulcastInfo ParseSimulcastLayers(JToken payload) {
      var simulcastData = payload.ToObject<LayerEvent>();
      if (!simulcastData.Medias.ContainsKey("0")) {
        Debug.LogError("Invalid payload for simulcast layers event received");
        return null;
      }
      return simulcastData.Medias["0"];
    }

    public static int ParseViewerCount(JToken payload) {
      return payload["viewercount"].ToObject<int>();
    }

    public static PublisherActiveEvent ParsePublisherActiveEvent(JToken payload) {
      return payload.ToObject<PublisherActiveEvent>();
    }

    public static PublisherInctiveEvent ParsePublisherInactiveEvent(JToken payload) {
      return payload.ToObject<PublisherInctiveEvent>();
    }


  }
}

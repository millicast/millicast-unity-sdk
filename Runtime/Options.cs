using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.WebRTC;

namespace Dolby.Millicast
{
  /// <summary>
  /// An enum denoting the supported video codecs.  
  /// </summary>
  public enum VideoCodec
  {
    VP8,
    VP9,
    H264,
    AV1,
  }

  /// <summary>
  /// An enum denoting the supported audio codecs. Currently, 
  /// only Opus is supported. 
  /// </summary>
  public enum AudioCodec
  {
    OPUS
  }


  /// \internal
  [Serializable]
  public class Options
  {
    /// <summary>
    /// Whether you want to enable stereo audio or not.
    /// </summary>
    public bool stereo = false;
    /// <summary>
    /// enable discontinuous transmission on the publishing side, so audio data is only sent when a user’s voice is detected.
    /// </summary>
    public bool dtx = false;

  }

  /// <summary>
  /// This class contains settings regarding bitrate, framerate
  /// and resolution scaling. Those settings can be modified in
  /// real-time.
  /// </summary>
  [Serializable]
  public class VideoConfig
  {
    /// <summary>
    /// The Maximum bitrate that can be used for video encoding. The value
    /// is in kilobits per second.
    /// </summary>
    public uint maxBitrate = 2500;

    /// <summary>
    /// The minimum bitrate that can be used for video encoding. The value
    /// is in kilobits per second
    /// </summary>
    public uint minBitrate = 300;

    /// <summary>
    /// The max framerate that can be used.
    /// </summary>
    public uint maxFramerate = 60;

    /// <summary>
    /// A float value greater than 1.0.
    /// It can be used to scale down video resolution. For example,
    /// setting to 2.0 will scale down the resolution by 50%.
    /// </summary>
    public double resolutionDownScaling = 1.0;
  }

  /// <summary>
  /// Publisher specific options. Must be set before publishing, and cannot be changed while publishing. 
  /// </summary>
  [Serializable]
  public class PublisherOptions : Options
  {

    public VideoCodec videoCodec;

    /// <summary>
    /// Used to give your source an Id. This feature is a WIP.
    /// </summary>
    public string multiSourceId;

    /// <summary>
    /// Create the default options for publishing 
    /// </summary>
    /// <returns></returns>
    public static PublisherOptions CreateDefault()
    {
      return new PublisherOptions
      {
        videoCodec = Capabilities.GetFirstAvailableVideoCodec(),
        stereo = true,
        dtx = true
      };
    }
  }

  /// <summary>
  /// Subscriber specific options 
  /// </summary>
  [Serializable]
  public class SubscriberOptions : Options
  {
    public static SubscriberOptions CreateDefault()
    {
      // TODO: Implement this
      return new SubscriberOptions();
    }
  }

  /// <summary>
  /// This class exposes APIs necessary to query device capabilities, like codecs and 
  /// resolutions.  
  /// </summary>
  [Serializable]
  public class Capabilities
  {

    /// <summary>
    /// Returned by <see cref="GetMaximumSupportedResolution"/> to denote the maximum supported
    /// resolution for the queried codec.
    /// </summary>
    public enum SupportedResolutions
    {
      RES_720P,
      RES_1080P,
      RES_1440P,
      RES_2K,
      RES_4K,
      ANY,
    }

    private static VideoCodec[] _codecs;

    /// <summary>
    /// Returns a list of available video codecs. Device implementation dependant. 
    /// </summary>
    /// <returns>An array of <see cref="VideoCodec"/> supported by the current device</returns>
    public static VideoCodec[] GetAvailableVideoCodecs()
    {
      if (_codecs != null) return _codecs;

      var codecs = new List<VideoCodec>();
      foreach (var cap in InternalWebRTC.GetCodecCapabilities(TrackKind.Video))
      {
        if (cap.mimeType.Contains("VP8")) codecs.Add(VideoCodec.VP8);
        if (cap.mimeType.Contains("VP9")) codecs.Add(VideoCodec.VP9);
        if (cap.mimeType.Contains("H264")) codecs.Add(VideoCodec.H264);
        if (cap.mimeType.Contains("AV1X")) codecs.Add(VideoCodec.AV1);
      }
      _codecs = codecs.ToArray();
      return _codecs;
    }

    /// <summary>
    /// Returns the first available codec. 
    /// </summary>
    /// <returns> The first available <see cref="VideoCodec"/> on the platform. Generally <see cref="VideoCodec.VP8"/> </returns>
    public static VideoCodec GetFirstAvailableVideoCodec()
    {
      return GetAvailableVideoCodecs()[0];
    }

    /// <summary>
    /// Query the maximum supported resolution for the given codec on this device. 
    /// </summary>
    /// <param name="codec"> The video codec to be queried </param>
    /// <returns> The maximum <see cref="SupportedResolutions"/> on this device. </returns>
    public static SupportedResolutions GetMaximumSupportedResolution(VideoCodec codec)
    {
      var res = InternalWebRTC.GetMaximumSupportedResolution(codec);
      return res;
    }
  }
}

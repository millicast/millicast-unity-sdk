using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using Unity.WebRTC;
using System.Text;
using System.Linq;
using UnityEngine.UIElements;

namespace Dolby.Millicast {
public class TransformableFrameInfo {
  internal TransformableFrameInfo(RTCEncodedFrame frame) {
    this.ssrc = frame.Ssrc;
    this.timestamp = frame.Timestamp;
    this._transformedData = null;
    this.data = frame.GetData();
    this.length = this.data.Length;
  }

  /// <summary>
  /// the source identifier that generated the frame.
  /// This distinguishes the source that generated the frame.
  /// </summary>
  public readonly uint ssrc;

  /// <summary>
  /// The timestamp of receiving the video frame.
  /// </summary>
  public readonly uint timestamp;

  /// <summary>
  /// This contains the encoded frame data without any modification.
  ///
  /// When Publishing, this should be filled with data that will end up
  /// being attached to the frame after encoding. This does not contain
  /// The frame data.
  ///
  /// When Subscribing, this will contain data extracted from the encoded frame
  /// buffer before decoding.
  /// </summary>
  public readonly NativeArray<byte>.ReadOnly data;

  public void SetData(NativeArray<byte>.ReadOnly data, int length) {
    if (length > data.Length) {
      throw new Exception("Invalid length provided");
    }
    this.length = length;
    _transformedData = data;
  }

  /// <summary>
  /// Transformed data in case the user called SetData();
  /// </summary>
  private NativeArray<byte>.ReadOnly? _transformedData;

  /// <summary>
  /// The length of the frame to be sent down to webrtc. If the frame
  /// is not transformed, then this will reflect the original frame's size.
  /// </summary>
  internal int length { get; private set; }

  /// <summary>
  /// Internally used to either get the original frame or the transformed
  /// frame if it was set.
  /// </summary>
  /// <returns></returns>
  internal NativeArray<byte>.ReadOnly GetData() {
    if (_transformedData?.IsCreated ?? false) {
      return (NativeArray<byte>.ReadOnly)_transformedData;
    }
    return data;
  }
}

/// <summary>
/// This class contains Encoded Video Frames that are optionally
/// transformed before being packetized for sending or decoded. Use
/// this class to get information about the video frames as well as
/// optionally transform the encoded frames.
/// </summary>
public class TransformableVideoFrameInfo : TransformableFrameInfo {

  public enum FrameType { Empty, Key, Delta }
  ;

  internal TransformableVideoFrameInfo(RTCEncodedVideoFrame frame)
      : base(frame) {
    var metaData = frame.GetMetadata();
    if (metaData.frameId > 0)
      this.frameId = metaData.frameId;
    if (metaData.width > 0)
      this.width = metaData.width;
    if (metaData.height > 0)
      this.height = metaData.height;
    this.spatialIndex = metaData.spatialIndex;
    this.temporalIndex = metaData.temporalIndex;
    this.Type = ConvertToFrameType(frame.Type);
  }

  /// <summary>
  ///
  /// </summary>
  public readonly FrameType Type;

  /// <summary>
  ///
  /// </summary>
  public readonly long? frameId;

  /// <summary>
  /// The width of the encoded video frame
  /// </summary>
  public readonly ushort? width;

  /// <summary>
  /// The height of the encoded video frame
  /// </summary>
  public readonly ushort? height;

  /// <summary>
  /// This specifies the SVC/simulcast spatial layer index
  /// </summary>
  public readonly long? spatialIndex;

  /// <summary>
  /// This specifies the SVC/simulcast temporal layer index
  /// </summary>
  public readonly long? temporalIndex;

  private FrameType ConvertToFrameType(RTCEncodedVideoFrameType type) {
    switch (type) {
    case RTCEncodedVideoFrameType.Empty:
      return FrameType.Empty;
    case RTCEncodedVideoFrameType.Key:
      return FrameType.Key;
    case RTCEncodedVideoFrameType.Delta:
      return FrameType.Delta;
    default:
      return FrameType.Empty;
    };
  }
}

/// <summary>
/// This class contains Encoded Audio Frames that are optionally
/// transformed before being packetized for sending or decoded. Use
/// this class to optionally transform the encoded frames or attach metadata.
/// </summary>
public class TransformableAudioFrameInfo : TransformableFrameInfo {
  internal TransformableAudioFrameInfo(RTCEncodedAudioFrame frame)
      : base(frame) {}
}

public class FrameMetadata {
  public delegate void
  DelegateOnTransformableVideoFrame(TransformableVideoFrameInfo info);
  public delegate void
  DelegateOnTransformableAudioFrame(TransformableAudioFrameInfo info);
}
}

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.WebRTC;
using UnityEngine.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Playables;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Dolby.Millicast.RuntimeTests")]
namespace Dolby.Millicast
{
  internal abstract class IFrameTransformer {
    public abstract RTCRtpTransform GetTransform();
    public abstract void SetTransform(RTCRtpScriptTransform transform);
  }
  internal class SendFrameTransformer: IFrameTransformer {
    private RTCRtpSender _rTCRtpSender;

    public SendFrameTransformer(RTCRtpSender rTCRtpSender) {
      _rTCRtpSender = rTCRtpSender;
    }

    public override RTCRtpTransform GetTransform() {
      return _rTCRtpSender.Transform;
    }

    public override void SetTransform(RTCRtpScriptTransform transform) {
      _rTCRtpSender.Transform = transform;
    }
  }

  internal class ReceiveFrameTransformer: IFrameTransformer {
    private RTCRtpReceiver _rTCRtpReceiver;

    public ReceiveFrameTransformer(RTCRtpReceiver rTCRtpReceiver) {
      _rTCRtpReceiver = rTCRtpReceiver;
    }

    public override RTCRtpTransform GetTransform() {
      return _rTCRtpReceiver.Transform;
    }

    public override void SetTransform(RTCRtpScriptTransform transform) {
      _rTCRtpReceiver.Transform = transform;
    }
  }

  internal class VideoConfigurator {
    private IFrameTransformer _iFrameTransformer;
    private FrameMetadata.DelegateOnTransformableVideoFrame _onTransformableVideoFrame;
    internal VideoConfigurator(IFrameTransformer iFrameTransformer) {
      _iFrameTransformer = iFrameTransformer;
    }

    public void SetTransform(FrameMetadata.DelegateOnTransformableVideoFrame callback) {
      _onTransformableVideoFrame = callback;
      _iFrameTransformer.SetTransform(new RTCRtpScriptTransform(TrackKind.Video, (RTCTransformEvent e) => {
        TransformableVideoFrameInfo info = new TransformableVideoFrameInfo(e.Frame as RTCEncodedVideoFrame);
        this._onTransformableVideoFrame?.Invoke(info);
        e.Frame.SetData(info.GetData(), 0, info.length);
        _iFrameTransformer.GetTransform().Write(e.Frame);
      }));
    }
  }

  internal class AudioConfigurator {
    private IFrameTransformer _iFrameTransformer;
    private FrameMetadata.DelegateOnTransformableAudioFrame _onTransformableAudioFrame;
    internal AudioConfigurator(IFrameTransformer iFrameTransformer) {
      _iFrameTransformer = iFrameTransformer;
    }
    public void SetTransform(FrameMetadata.DelegateOnTransformableAudioFrame callback) {
      _onTransformableAudioFrame = callback;
      _iFrameTransformer.SetTransform(new RTCRtpScriptTransform(TrackKind.Audio, (RTCTransformEvent e) => {
        TransformableAudioFrameInfo info = new TransformableAudioFrameInfo(e.Frame as RTCEncodedAudioFrame);
        this._onTransformableAudioFrame?.Invoke(info);
        e.Frame.SetData(info.GetData(), 0, info.length);
        _iFrameTransformer.GetTransform().Write(e.Frame);
      }));
    }
  }
}



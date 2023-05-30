using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;

using Unity.WebRTC;
using Newtonsoft.Json;

namespace Dolby.Millicast
{


  /// <summary>
  /// reference: https://source.chromium.org/chromium/chromium/src/+/main:third_party/webrtc/api/video_codecs/h264_profile_level_id.h
  /// All values are equal to ten times the level number, except level 1b which is
  /// special. Reference   
  /// </summary>
  internal enum H264Level
  {
    kLevel1_b = 0,
    kLevel1 = 10,
    kLevel1_1 = 11,
    kLevel1_2 = 12,
    kLevel1_3 = 13,
    kLevel2 = 20,
    kLevel2_1 = 21,
    kLevel2_2 = 22,
    kLevel3 = 30,
    kLevel3_1 = 31,
    kLevel3_2 = 32,
    kLevel4 = 40,
    kLevel4_1 = 41,
    kLevel4_2 = 42,
    kLevel5 = 50,
    kLevel5_1 = 51,
    kLevel5_2 = 52
  };

  internal class InternalWebRTC
  {

    // For level_idc=11 and profile_idc=0x42, 0x4D, or 0x58, the constraint set3
    // flag specifies if level 1b or level 1.1 is used.
    private static int kConstraintSet3Flag = 0x10;

    public static RTCRtpCodecCapability[] GetCodecCapabilities(TrackKind kind)
    {
      var capabilities = RTCRtpSender.GetCapabilities(kind);
      return capabilities.codecs;
    }

    private static H264Level ParseProfileLevelIdFromString(string profileLevelId)
    {
      int level_idc = Convert.ToInt32(profileLevelId.Substring(4, 2), 16);
      int profile_iop = Convert.ToInt32(profileLevelId.Substring(2, 2), 16);
      int profile_idc = Convert.ToInt32(profileLevelId.Substring(0, 2), 16);

      // Parse level based on level_idc and constraint set 3 flag.
      H264Level level;
      switch ((H264Level)level_idc)
      {
        case H264Level.kLevel1_1:
          level = (H264Level)(profile_iop & kConstraintSet3Flag) != 0 ? H264Level.kLevel1_b : H264Level.kLevel1_1;
          break;
        case H264Level.kLevel1:
        case H264Level.kLevel1_2:
        case H264Level.kLevel1_3:
        case H264Level.kLevel2:
        case H264Level.kLevel2_1:
        case H264Level.kLevel2_2:
        case H264Level.kLevel3:
        case H264Level.kLevel3_1:
        case H264Level.kLevel3_2:
        case H264Level.kLevel4:
        case H264Level.kLevel4_1:
        case H264Level.kLevel4_2:
        case H264Level.kLevel5:
        case H264Level.kLevel5_1:
        case H264Level.kLevel5_2:
          level = (H264Level)level_idc;
          break;
        default:
          // Unrecognized level_idc.
          throw new Exception("Error parsing H264 level: Unrecognized profile level id");
      }
      return level;
    }

    public static Capabilities.SupportedResolutions GetMaximumSupportedResolution(VideoCodec codec)
    {
      if (codec == VideoCodec.VP8 || codec == VideoCodec.VP9 || codec == VideoCodec.AV1)
        return Capabilities.SupportedResolutions.ANY;

      // Only H264 has restrictive profile constraints
      var caps = GetCodecCapabilities(TrackKind.Video);

      // H264 profile level to resolution extraction.
      var capsH264 = Array.FindAll(caps, (cap) => cap.mimeType.Contains(codec.ToString()));
      if (capsH264.Length == 0) throw new Exception($"Unsupported codec: {codec.ToString()}");

      Capabilities.SupportedResolutions maximumSupportedRes = Capabilities.SupportedResolutions.ANY;

      foreach (var cap in capsH264)
      {
        var pattern = @"profile-level-id=(.+)?";
        var profileLevelId = Regex
            .Matches(cap.sdpFmtpLine, pattern)
            .Cast<Match>()
            .Select(x => x.Groups[1].Value)
            .First();

        // profile level id has to be 3 bytes in hex format (i.e. 6 characters)
        if (profileLevelId.Length != 6)
          throw new Exception($"Invalid profile level string{profileLevelId}");

        H264Level level = InternalWebRTC.ParseProfileLevelIdFromString(profileLevelId);

        if (level < H264Level.kLevel4_2)
          maximumSupportedRes = Capabilities.SupportedResolutions.RES_2K;

        if (level < H264Level.kLevel4)
          maximumSupportedRes = Capabilities.SupportedResolutions.RES_720P;

      }

      return maximumSupportedRes;
    }
  }

  internal class PeerConnection
  {


    // This is a specific coroutine runner, to prevent us
    // from instantiating PeerConnection as a component
    public delegate Coroutine DelegateOnCoroutineRunRequested(IEnumerator iEnumerator);
    public event DelegateOnCoroutineRunRequested OnCoroutineRunRequested;

    // Event fired when the remote peer adds a @ref{MediaStreamTrack}
    public event DelegateOnTrack OnTrack;

    // Event fired when the Peer connection has been established.
    // When received, the Peer Connection is ready to receive communication.
    public delegate void DelegateOnConnected(PeerConnection pc);
    public event DelegateOnConnected OnConnected;

    //// Event fired when the Peer connection has been disconnected.
    //public delegate void DelegateOnDisconnected(PeerConnection pc);
    //public event DelegateOnDisconnected OnDisconnected;

    public delegate RTCSessionDescription DelegateOnSdpMunge(RTCSessionDescription desc);
    // Event fired when the local Sdp is ready for munging. 
    public event DelegateOnSdpMunge OnLocalSdpMunge;
    // Event fired when the remote Sdp is ready for munging. 
    public event DelegateOnSdpMunge OnRemoteSdpMunge;


    public delegate void DelegateOnSdpReady(RTCSessionDescription desc);
    public event DelegateOnSdpReady OnLocalSdpReady;

    // Event fired when the Peer connection has been established.
    // When received, the Peer Connection is ready to receive communication.
    public delegate void DelegateOnError(PeerConnection pc, String error);
    public event DelegateOnError OnError;

    public RTCSessionDescription localSdp { get; private set; }
    public RTCSessionDescription remoteSdp { get; private set; }


    private RTCPeerConnection _pc;
    private ISignaling _signaling;
    private StatsParser parser;


    private void OnIceConnectionChange(RTCIceConnectionState state)
    {
      Debug.Log($"IceConnectionState: {state}");

      if (state == RTCIceConnectionState.Connected || state == RTCIceConnectionState.Completed)
      {
        // TODO: Start stats reporting
        //StartCoroutine(CheckStats(pc));
      }
    }

    private void OnTrackEvent(RTCTrackEvent e)
    {
      OnTrack?.Invoke(e);
    }

    private IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
    {
      Debug.Log($"Offer created:\n{desc.sdp}");
      Debug.Log($"SetLocalDescription start");

      // Munging

      if (OnLocalSdpMunge != null)
      {
        desc = OnLocalSdpMunge(desc);
      }

      var op = _pc.SetLocalDescription(ref desc);
      yield return op;

      if (!op.IsError)
      {
        OnSetLocalSuccess(desc);
      }
      else
      {
        var error = op.Error;
        OnSetSessionDescriptionError(ref error);
        yield break;
      }

    }

    private void OnSetLocalSuccess(RTCSessionDescription desc)
    {
      localSdp = desc;
      Debug.Log($"SetLocalDescription complete");
      OnLocalSdpReady?.Invoke(desc);
    }

    private void OnSetSessionDescriptionError(ref RTCError error)
    {
      Debug.LogError($"Error Detail Type: {error.message}");
      HandleError(error.message);
    }

    private void HandleError(String message)
    {
      _pc.Close();
      _pc.Dispose();
      if (OnError != null)
      {
        OnError(this, message);
      }
    }

    private void OnSetRemoteSuccess(RTCSessionDescription desc)
    {
      remoteSdp = desc;
      Debug.Log($"SetRemoteDescription complete");
      OnConnected?.Invoke(this);
    }

    private IEnumerator OnCreateAnswerSuccess(RTCSessionDescription desc)
    {
      Debug.Log($"Answer created:\n{desc.sdp}");
      Debug.Log($"SetLocalDescription start");

      if (OnLocalSdpMunge != null)
      {
        desc = OnLocalSdpMunge(desc);
      }

      var op = _pc.SetLocalDescription(ref desc);
      yield return op;

      if (!op.IsError)
      {
        OnSetLocalSuccess(desc);
      }
      else
      {
        var error = op.Error;
        OnSetSessionDescriptionError(ref error);
      }
    }

    private IEnumerator OnRemoteAnswer(RTCSessionDescription desc)
    {
      Debug.Log($"Remote answer received: {desc.sdp}");

      if (OnRemoteSdpMunge != null)
      {
        desc = OnRemoteSdpMunge(desc);
      }

      var op = _pc.SetRemoteDescription(ref desc);
      yield return op;
      if (!op.IsError)
      {
        OnSetRemoteSuccess(desc);
      }
      else
      {
        var error = op.Error;
        OnSetSessionDescriptionError(ref error);
      }
    }
  
    public void CheckStats()
    {
        parser = new StatsParser(_pc);
        OnCoroutineRunRequested?.Invoke(LoopStatsCoroutine());
    }
    private IEnumerator LoopStatsCoroutine()
    {
        do
        {
            yield return OnCoroutineRunRequested?.Invoke(parser.CheckStats());
            yield return new WaitForSeconds(1f);
        }
        while(true);
    }

    public int getInboundChannelCount 
    { 
      get 
          {
            if(parser != null) 
                return parser.inboundAudioStreamChannelCount; 
            return -1;
          }
    }
    public int[] getChannelMap 
    {
       get
        {
          if(parser != null)
             return parser.ChannelMap; 
          return null;
        }
      }

    private void OnCreateSessionDescriptionError(RTCError error)
    {
      Debug.LogError($"Error Detail Type: {error.message}");
      HandleError(error.message);
    }

    IEnumerator PeerNegotiationNeeded()
    {
      var op = _pc.CreateOffer();
      yield return op;

      if (!op.IsError)
      {
        if (_pc.SignalingState != RTCSignalingState.Stable)
        {
          Debug.LogError("signaling state is not stable.");
          yield break;
        }

        yield return OnCoroutineRunRequested.Invoke(OnCreateOfferSuccess(op.Desc));
      }
      else
      {
        OnCreateSessionDescriptionError(op.Error);
      }
    }

    private void OnNegotiationNeeded()
    {
      OnCoroutineRunRequested?.Invoke(PeerNegotiationNeeded());
    }

    private RTCSessionDescription ParseAnswer(ServiceResponseData payload)
    {
      RTCSessionDescription answer;
      answer.type = RTCSdpType.Answer;
      answer.sdp = payload.sdp;
      return answer;
    }

    /// <summary>
    /// Establish the peer connection with the peer sitting behind
    /// Signaling.
    /// </summary>
    private void EstablishEvents()
    {
      if (OnCoroutineRunRequested == null)
      {
        throw new Exception("You must implement the OnCoroutineRunRequested event");
      }

      // Set the callbacks
      _signaling.OnEvent += (e, data) =>
      {
        switch (e)
        {
          case ISignaling.Event.RESPONSE:
            if(data != null)
              OnCoroutineRunRequested.Invoke(OnRemoteAnswer(ParseAnswer(data)));
            break;
        }
      };
    }

    /// <summary>
    /// This method should be called before anything else.
    /// </summary>
    /// <param name="signaling"></param>
    /// <param name="configuration"></param>
    public void SetUp(ISignaling signaling, RTCConfiguration configuration, bool addTransceiver = false)
    {
      _signaling = signaling;
      EstablishEvents();
      _pc = new RTCPeerConnection(ref configuration);
      _pc.OnNegotiationNeeded += OnNegotiationNeeded;
      _pc.OnTrack += OnTrackEvent;
      if(addTransceiver)
      {
        _pc.AddTransceiver(TrackKind.Video);
        _pc.AddTransceiver(TrackKind.Audio);
      }
    }

    public void Disconnect()
    {
      _pc?.Close();
    }

    public RTCRtpSender AddTrack(MediaStreamTrack track)
    {
      return _pc.AddTrack(track);
    }
    public RTCRtpTransceiver AddTransceiver(MediaStreamTrack track, RTCRtpTransceiverInit init=null)
    {
      return _pc.AddTransceiver(track, init);
    }


    public void RemoveTrack(MediaStreamTrack track)
    {
      if (track == null) return;

      foreach (var transceiver in _pc.GetTransceivers())
      {
        var senderTrack = transceiver.Sender.Track;
        if (senderTrack != null && senderTrack.Equals(track))
        {
          _pc.RemoveTrack(transceiver.Sender);
        }
      }
    }

    public IEnumerable<RTCRtpTransceiver> GetTransceivers()
    {
      return _pc?.GetTransceivers();
    }
  }
}


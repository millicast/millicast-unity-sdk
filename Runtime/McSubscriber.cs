﻿using System.Collections;
using Dolby.Millicast;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.WebRTC;
using UnityEngine.UI;
using UnityEngine;
using Newtonsoft.Json;

namespace Dolby.Millicast
{

  /// <summary>
  /// The Millicast subscriber class. Allows users to subscribe to Millicast streams.
  /// </summary>
  public class McSubscriber : MonoBehaviour
  {

    private HttpAuthenticator _httpAuthenticator;
    private ISignaling _signaling = null;
    private PeerConnection _pc;
    private RTCConfiguration _rtcConfiguration;
    private bool checkForStream = false;
    private bool _localSdpSent = false;
    private bool isUpdateStarted = false;

    // This dictionary maps source ids (in case of
    // multisource) to RawImages for rendering
    // Those source ids may not be set.
    private Dictionary<string, RawImage> _receiveImages;

    private WrapperRenderer _renderer = new WrapperRenderer();
    /// <summary>
    /// This boolean indicates whether the subscriber
    /// is currently subscribing to a stream.
    /// </summary>
    public bool isSubscribing { get; private set; } = false;


    [SerializeField]
    private string _streamName;
    /// <summary>
    /// The stream name to subscribe to. 
    /// </summary>
    public string streamName
    {
      get => _streamName;
      set => _streamName = value;
    }

    public Credentials credentials { get; set; } = null;

    [SerializeField]
    /// <summary>
    /// You have to set the publishing credentials
    /// before <c>Subscribe</c> is called.
    /// </summary>
    private McCredentials _credentials;

    /// <summary>
    /// Subscribe as soon as the script starts.
    /// </summary>
    [SerializeField]
    private bool _subscribeOnStart = false;
    public bool subscribeOnStart
    {
      get => this._subscribeOnStart;
      set { this._subscribeOnStart = value; }
    }

    /// <summary>
    /// If the current object contains a mesh renderer,
    /// enabling this will rename the mesh renderer's material
    /// to the stream name, and render the incoming stream on it.
    /// </summary>
    [SerializeField]
    [Tooltip("Enabling will render incoming stream onto mesh renderer's material, if it exists.")]
    private bool _updateMeshRendererMaterial = false;
    public bool updateMeshRendererMaterial
    {
      get => this._updateMeshRendererMaterial;
      set
      {
        this._updateMeshRendererMaterial = value;
        UpdateMeshRendererMaterial();
      }
    }

    [SerializeField]
    [Tooltip("Add your Materials here to rendering incoming video streams")]
    private List<Material> _renderMaterials = new List<Material>();
    /// <summary>
    /// Manually add materials to render the incoming video stream on.
    /// is used when you want utilise the Unity Inspector UI. Use this
    /// only to render onto 3D objects.
    /// </summary>
    public List<Material> renderMaterials
    {
      get => this._renderMaterials;
    }


    [SerializeField]
    [Tooltip("Add your RawImages here for rendering incoming video streams. For UI rendering")]
    private List<RawImage> _renderImages = new List<RawImage>();
    /// <summary>
    /// Manually add images to render the incoming video stream on.
    /// is used when you want utilise the Unity Inspector UI. This
    /// is to render in a GUI application only.
    /// </summary>
    public List<RawImage> renderImages
    {
      get => _renderImages;
    }


    /// <summary>
    /// Manually set the audio sources to render to. This
    /// is used when you want utilise the Unity Inspector UI.
    /// </summary>
    [Tooltip("Add your AudioSources here to render incoming audio streams")]
    [SerializeField]
    private List<AudioSource> _renderAudioSources = new List<AudioSource>();
    public List<AudioSource> renderAudioSources
    {
      get => this._renderAudioSources;
    }


    public delegate void DelegateSubscriber(McSubscriber subscriber);
    /// <summary>
    /// Event called when the publisher is publishing
    /// (i.e. media content is being delivered to the Millicast service.)
    /// </summary>
    public event DelegateSubscriber OnSubscribing;

    public delegate void DelegateOnViewerCount(McSubscriber subscriber, int count);
    /// <summary>
    /// Event called when the viewer count has been updated.
    /// </summary>
    public event DelegateOnViewerCount OnViewerCount;

    public delegate void DelegateOnConnectionError(McSubscriber subscribe, string message);
    /// <summary>
    /// Event called when the there is a connection error to the service.
    /// </summary>
    public event DelegateOnConnectionError OnConnectionError;


    /// <summary>
    /// Munge the remote sdp for subscribing.
    /// </summary>
    /// <param name="desc"></param>
    /// <returns></returns>
    private RTCSessionDescription MungeRemoteSdp(RTCSessionDescription desc)
    {
      // For compatibility with old webrtc versions
      var pattern = @"AV1";
      desc.sdp = Regex.Replace(desc.sdp, pattern, "AV1X");
      return desc;
    }

    /// <summary>
    /// Called when the local sdp is ready to be signalled.
    /// </summary>
    /// <param name="desc"></param>
    private IEnumerator SendLocalSdp(RTCSessionDescription desc)
    {

      if (desc.sdp == null || _localSdpSent || !_signaling.IsConnected()) yield break;
      _localSdpSent = true;

      // rename AV1X to AV1 for compatibility newer chromium versions. 
      var pattern = @"AV1X";
      desc.sdp = Regex.Replace(desc.sdp, pattern, "AV1");

      Debug.Log("Sending Local offer to peer");
      var payload = new Dictionary<string, dynamic>();

      // TODO: Need to add the following events later: vad, layers
      // TODO: Need to also add excludedSourceIds and pinnedSourceIds
      payload["streamId"] = streamName;
      payload["sdp"] = desc.sdp;
      payload["events"] = new string[] { "active", "inactive", "stopped", "viewercount" };

      yield return _signaling?.Send(ISignaling.Event.SUBSCRIBE, payload);
    }

    private IEnumerator AwaitSignalingMessages()
    {
      yield return _signaling.Connect();
    }

    /// <summary>
    /// Create the signaling channel.
    /// </summary>
    /// <param name="ws_url"> The url received from http authentication </param>
    /// <param name="token"> The token required to authenticate the WS connection </param>
    private void EstablishSignalingConnection(string ws_url, string token)
    {
      _signaling = new SignalingImpl(ws_url, token);
      _signaling.OnOpen += () =>
      {
        if (_pc != null) StartCoroutine(SendLocalSdp(_pc.localSdp));
      };
      _signaling.OnError += msg => Debug.Log($"[Signaling] Error from the media server: {msg}");

      _signaling.OnEvent += (e, payload) =>
      {
        switch (e)
        {
          case ISignaling.Event.VIEWER_COUNT:
            OnViewerCount?.Invoke(this, payload.viewercount);
            break;
        }
      };

      StartCoroutine(AwaitSignalingMessages());
    }

    /// <summary>
    /// Returns the codec capabilities as requested by the user's codec
    /// preference
    /// </summary>
    /// <returns></returns>
    private RTCRtpCodecCapability[] GetVideoCodecCapabilities()
    {
      return InternalWebRTC.GetCodecCapabilities(TrackKind.Video);
    }

    /// <summary>
    /// Create the Peer Connection given the ice servers.
    /// </summary>
    /// <param name="iceServers"></param>
    /// <exception cref="PublishingException"></exception>
    private void EstablishPeerConnection(ref RTCIceServer[] iceServers)
    {
      _rtcConfiguration = new RTCConfiguration();
      _rtcConfiguration.iceServers = iceServers;
      _pc = new PeerConnection();
      _pc.OnError += (_, msg) => { Debug.Log("[PeerConnection] " + msg); };
      _pc.OnLocalSdpReady += (sdp) => StartCoroutine(SendLocalSdp(sdp));
      _pc.OnRemoteSdpMunge += MungeRemoteSdp;
      _pc.OnConnected += (_) =>
      {
        Debug.Log("[PeerConnection] established connection.");
        isSubscribing = true;
        OnSubscribing?.Invoke(this);
      };
      _pc.OnCoroutineRunRequested += (e) =>
      {
        return StartCoroutine(e);
      };
      _pc.OnTrack += (e) =>
      {
        if (e.Track is VideoStreamTrack track)
        {
          Debug.Log("[Subscriber] Received video track");
          track.OnVideoReceived += (tex) =>
                {
                  _renderer.SetTexture(tex);
                };
        }

        if (e.Track is AudioStreamTrack audioTrack)
        {
          Debug.Log("[Subscriber] Received audio track");
          _renderer.SetAudioTrack(audioTrack);
        }
      };
      _pc.SetUp(_signaling, _rtcConfiguration);
    }

    void Awake()
    {
      // Http Authentication
      _httpAuthenticator = new HttpAuthenticator();
      bool checkForStreamCoroutineCalled = false;
      _httpAuthenticator.OnError += (msg) =>
      {
        Debug.Log("[HTTPAuthenticator] " + msg + "\nWaiting for incoming Stream...");
        OnConnectionError?.Invoke(this, msg);
        checkForStream = true;
        if (checkForStreamCoroutineCalled) return;
        checkForStreamCoroutineCalled = true;
        StartCoroutine(CheckForIncomingStream());
      };
      _httpAuthenticator.OnWebsocketInfo += EstablishSignalingConnection;
      _httpAuthenticator.OnIceServers += EstablishPeerConnection;
    }

    IEnumerator CheckForIncomingStream()
    {
      while (!isSubscribing && (_signaling == null || !_signaling.IsConnected()))
      {
        if (checkForStream)
        {
          yield return new WaitForSeconds(1f);
          checkForStream = false;
          Subscribe();
        }
        yield return new WaitForSeconds(5f);
      }

      yield return null;
    }

    void Start()
    {
      if (subscribeOnStart)
      {
        Subscribe();
      }
    }
    private void Update()
    {
      _signaling?.Dispatch();
    }

    private void OnDestroy()
    {
      _signaling?.Disconnect();
      Reset();
    }

    /// <summary>
    /// Reset the Peer connection and state, keeping
    /// the signaling channel open for further publishing.
    /// </summary>
    private void Reset()
    {
      isSubscribing = false;
      _localSdpSent = false;
      _signaling = null;
      _pc?.Disconnect();
      _pc = null;
    }

    private bool CheckValidCredentials(McCredentials credentials)
    {
      if (credentials == null) return false;

      return !string.IsNullOrEmpty(streamName) &&
             !string.IsNullOrEmpty(credentials.subscribe_url) &&
             !string.IsNullOrEmpty(credentials.accountId);
    }
    private bool CheckValidCredentials(Credentials credentials)
    {
      if (credentials == null) return false;

      return !string.IsNullOrEmpty(streamName) &&
             !string.IsNullOrEmpty(credentials.url) &&
             !string.IsNullOrEmpty(credentials.accountId);

    }

    private void AddRenderTargets()
    {
      foreach (var material in _renderMaterials)
      {
        AddVideoRenderTarget(material);
      }

      foreach (var image in _renderImages)
      {
        AddVideoRenderTarget(image);
      }

      foreach (var audioSource in _renderAudioSources)
      {
        AddRenderAudioSource(audioSource);
      }
    }


    /// <summary>
    /// This will render incoming streams onto the mesh renderer
    /// of this object, if it exists. 
    /// </summary>
    private void UpdateMeshRendererMaterial()
    {
      if (_updateMeshRendererMaterial && !string.IsNullOrEmpty(streamName))
      {
        // If the current object contains a mesh renderer, we will 
        // update its material with the incoming stream.
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
          Material mat = meshRenderer.materials[0];
          if (mat != null)
          {
            mat.name = streamName;
            AddVideoRenderTarget(mat);
          }
        }
      }
    }

    /// <summary>
    /// Subscribe to a stream.
    /// </summary>
    public void Subscribe()
    {

      // Reset if currently subscribing
      Reset();

      // Prioritise UI credentials
      if (CheckValidCredentials(_credentials))
      {
        credentials = new Credentials(_credentials, true);
      }
      else if (!CheckValidCredentials(credentials))
      {
        throw new Exception("You need to provide valid credentials and stream name.");
      }
      AddRenderTargets();
      UpdateMeshRendererMaterial();

      if (!isUpdateStarted)
      {
        isUpdateStarted = true;
        StartCoroutine(WebRTC.Update());
      }
      // Http Authentication
      _httpAuthenticator.credentials = credentials;
      StartCoroutine(_httpAuthenticator.Connect(streamName));
    }

    /// <summary>
    /// UnSubscribe from a stream.
    /// </summary>
    public void UnSubscribe()
    {
      Reset();
    }

    /// <summary>
    /// Add a material to display the incoming remote video stream on. The material's main texture
    /// will be replaced with the remote video stream texture when it is available. Using this
    /// to replace a GUI component's material will not work. Use the RawImage overload instead.  
    /// </summary>
    /// <param name="material">A Unity <see cref="Material"/>.</param>
    public void AddVideoRenderTarget(Material material)
    {
      _renderer.AddVideoTarget(material);
    }

    /// <summary>
    /// Stop rendering the remote video stream on the previously given material
    /// </summary>
    /// <param name="material">A Unity <see cref="Material"/>. </param>
    public void RemoveVideoRenderTarget(Material material)
    {
      _renderer.RemoveVideoTarget(material);
    }

    /// <summary>
    /// Stop rendering the remote video stream on the previously given RawImage.
    /// </summary>
    /// <param name="image">A Unity <see cref="RawImage"/>. </param>
    public void RemoveVideoRenderTarget(RawImage image)
    {
      _renderer.RemoveVideoTarget(image);
    }

    /// <summary>
    /// Add a UI RawImage to display the remote video stream on. The RawImage's texture
    /// will be replaced with the remote video stream texture when it is available. Use this
    /// when you want to use render the remote stream in a GUI.
    /// </summary>
    /// <param name="image">A Unity <see cref="RawImage"/>.</param>
    public void AddVideoRenderTarget(RawImage image)
    {
      _renderer.AddVideoTarget(image);
    }

    /// <summary>
    /// Add an audio source that will render the received audio stream.
    /// </summary>
    /// <param name="source"> A Unity <see cref="AudioSource"/> instance. </param>
    public void AddRenderAudioSource(AudioSource source)
    {
      _renderer.AddAudioTarget(source);
    }

    /// <summary>
    /// Remove an audio source so that it stops rendering.
    /// </summary>
    /// <param name="source"> A previously added Unity <see cref="AudioSource"/> instance.</param>
    public void RemoveRenderAudioSource(AudioSource source)
    {
      _renderer.RemoveAudioTarget(source);
    }
  }

}
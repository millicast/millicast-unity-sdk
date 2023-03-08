﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using Newtonsoft.Json;
using UnityEngine.Experimental.Rendering;

namespace Dolby.Millicast
{
    /// <summary>
    /// Optionally used in <see cref="SetVideoSource"/> to set the capture stream resolution.
    /// </summary>
    [Serializable]
    public class StreamSize
    {
        public int width = 1280;
        public int height = 720;

    }
    public enum VideoSourceType
    {
        Camera,
        RenderTexture
    }

    /// <summary>
    /// The Millicast publisher. This class allows its users to publish audio and video media
    /// to the Millicast service. 
    /// </summary>
    [Serializable]
    public class McPublisher : MonoBehaviour
    {
        private const string PREVIEW_URL = "https://viewer.millicast.com/?streamId={{accountId}}/{{streamName}}";
        private HttpAuthenticator _httpAuthenticator;
        private ISignaling _signaling = null;
        private PeerConnection _pc;
        private RTCConfiguration _rtcConfiguration;
        private bool _localSdpSent = false;
        private readonly List<RTCRtpSender> _rtpSenders = new List<RTCRtpSender>();
        private VideoStreamTrack _videoTrack;
        private Camera _publishingCamera = null;

        private AudioStreamTrack _audioTrack;

        private WrapperRenderer _renderer = new WrapperRenderer();

        private bool isUpdateStarted = false;
        private const string CopyCameraName = "mcPublisherCam";

        public delegate void DelegatePublisher(McPublisher publisher);
        /// <summary>
        /// Event called when the publisher is publishing
        /// (i.e. media content is being delivered to the Millicast service.)
        /// </summary>
        public event DelegatePublisher OnPublishing;

        public delegate void DelegateOnViewerCount(McPublisher publisher, int count);
        /// <summary>
        /// Event called when the viewer count has been updated.
        /// </summary>
        public event DelegateOnViewerCount OnViewerCount;



        public delegate void DelegateOnConnectionError(McPublisher publisher, string message);
        /// <summary>
        /// Event called when the there is a connection error to the service.
        /// </summary>
        public event DelegateOnConnectionError OnConnectionError;

        /// <summary>
        /// A boolean to reflect if the publisher is currently
        /// publishing. 
        /// </summary>
        public bool isPublishing { get; private set; } = false;

        [SerializeField]
        private string _streamName;
        /// <summary>
        /// The stream name to publish to. 
        /// </summary>
        public string streamName
        {
            get => _streamName;
            set => _streamName = value;
        }

        [SerializeField]
        /// <summary>
        /// You have to set the publishing credentials
        /// before <c>Publish</c> is called.
        /// </summary>
        private McCredentials _credentials;
        [Header("Video Configuration Settings")]
        [Tooltip("Assign VideoConfiguration Scriptable Object reference here.")]
        [SerializeField] private VideoConfiguration _videoConfigData;
        public VideoConfiguration videoConfigData { get => _videoConfigData; }


        public Credentials credentials { get; set; } = null;

        public VideoSourceType videoSourceType;
        /// <summary>
        /// Whether or not to use the audio listener as a source to publishing. This
        /// is a UI setting. If the game object does not contain an AudioListener, 
        /// the option will have no effect. 
        /// </summary>
        [Tooltip("Only use this if the object contains an AudioListener")]
        public bool _useAudioListenerAsSource = false;
        [HideInInspector] public AudioSource _audioSource;
        //visibility will be controller by the EditorScript=> MyEditorClass
        [HideInInspector]public Camera _videoSourceCamera;
         //visibility will be controller by the EditorScript=> MyEditorClass
        [HideInInspector]public RenderTexture _videoSourceRenderTexture;
        private VideoConfig _videoConfig;
        private PublisherOptions _options = new PublisherOptions();
        /// <summary>
        /// You have to set the publisher options
        /// before <c>Publish</c> is called.
        /// </summary>
        public PublisherOptions options
        {
            get => this._options;
            set { if (!isPublishing) this._options = value; }
        }

        [SerializeField]
        [Tooltip("Publish as soon as the script start")]
        private bool _publishOnStart = false;

        /// <summary>
        /// Munge the local sdp for publishing.
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        private RTCSessionDescription MungeLocalSdp(RTCSessionDescription desc)
        {
            string sdpSearchSubstr = "minptime=10;useinbandfec=1";
            string sdpAllModifications = sdpSearchSubstr;
            if (options.stereo) sdpAllModifications += ";stereo=1";
            if (options.dtx) sdpAllModifications += ";usedtx=1";

            var idx = desc.sdp.IndexOf(sdpSearchSubstr);
            if (idx != -1)
            {
                desc.sdp.Remove(idx, sdpSearchSubstr.Length);
                desc.sdp.Insert(idx, sdpAllModifications);
            }
            return desc;
        }

        /// <summary>
        /// Munge the remote sdp for publishing.
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        private RTCSessionDescription MungeRemoteSdp(RTCSessionDescription desc)
        {
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
            if (options.videoCodec == VideoCodec.AV1)
            {
                var pattern = @"AV1X";
                desc.sdp = Regex.Replace(desc.sdp, pattern, "AV1");
            }

            Debug.Log("Sending Local offer to peer");
            var payload = new Dictionary<string, dynamic>();

            payload["name"] = streamName;
            payload["sdp"] = desc.sdp;
            payload["events"] = new string[] { "active", "inactive", "viewercount" };

            var codecName = _options.videoCodec.ToString();
            payload["codec"] = codecName;

            if (_options.multiSourceId?.Length != 0)
            {
                payload["sourceId"] = _options.multiSourceId;
            }

            yield return _signaling?.Send(ISignaling.Event.PUBLISH, payload);
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
            var capabilities = InternalWebRTC.GetCodecCapabilities(TrackKind.Video);

            var selectedCapabilities = Array.FindAll(capabilities, (e) =>
            {
                return e.mimeType.Contains(options.videoCodec.ToString()) ||
                 e.mimeType.Contains("red") ||
                 e.mimeType.Contains("rtx") ||
                 e.mimeType.Contains("ulpfec");
            });
            return selectedCapabilities;
        }

        /// <summary>
        /// Create the Peer Connection given the ice servers.
        /// </summary>
        /// <param name="iceServers"></param>
        /// <exception cref="Exception"></exception>
        private void EstablishPeerConnection(ref RTCIceServer[] iceServers)
        {
            _rtcConfiguration = new RTCConfiguration();
            _rtcConfiguration.iceServers = iceServers;
            _pc = new PeerConnection();
            _pc.OnError += (_, msg) => { Debug.Log("[PeerConnection] " + msg); };
            _pc.OnLocalSdpMunge += MungeLocalSdp;
            _pc.OnRemoteSdpMunge += MungeRemoteSdp;
            _pc.OnLocalSdpReady += (sdp) => StartCoroutine(SendLocalSdp(sdp));
            _pc.OnConnected += (_) =>
            {
                Debug.Log("[PeerConnection] established connection.");
                Debug.Log("[Preview Link] " + GetPreviewURL());
                isPublishing = true;
                UpdatePeerConnectionParameters();
                OnPublishing?.Invoke(this);
            };
            _pc.OnCoroutineRunRequested += (e) =>
            {
                return StartCoroutine(e);
            };

            _pc.SetUp(_signaling, _rtcConfiguration);
            _rtpSenders.Clear();
            foreach (var track in new MediaStreamTrack[] { _videoTrack, _audioTrack })
            {
                if (track != null)
                    _rtpSenders.Add(_pc.AddTrack(track));
            }

            foreach (var transceiver in _pc.GetTransceivers())
            {
                if (_rtpSenders.Contains(transceiver.Sender) &&
                    transceiver.Sender.Track is VideoStreamTrack &&
                    _options.videoCodec != VideoCodec.VP8)
                {
                    transceiver.SetCodecPreferences(GetVideoCodecCapabilities());
                    transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;
                }
            }
        }

        // Use this for initialization
        void Awake()
        {
            // Http Authentication
            _httpAuthenticator = new HttpAuthenticator();
            _httpAuthenticator.OnError += (msg) =>
            {
                Debug.Log("[HTTPAuthenticator] " + msg);
                OnConnectionError?.Invoke(this, msg);
            };

            _httpAuthenticator.OnWebsocketInfo += EstablishSignalingConnection;
            _httpAuthenticator.OnIceServers += EstablishPeerConnection;
            CheckVideoSettings();
        }

        /// <summary>
        /// Reset the Peer connection and state, keeping
        /// the signaling channel open for further publishing.
        /// </summary>
        private void Reset()
        {
            isPublishing = false;
            _localSdpSent = false;

            _pc?.Disconnect();

            _pc = null;
            _signaling?.Disconnect();
            _rtpSenders.Clear();
            Destroy(gameObject.GetComponent<AudioSender>());
        }

        private bool CheckValidCredentials(McCredentials credentials)
        {
            if (credentials == null) return false;

            return !string.IsNullOrEmpty(streamName) &&
                    !string.IsNullOrEmpty(credentials.publish_token) &&
                    !string.IsNullOrEmpty(credentials.publish_url) &&
                    !string.IsNullOrEmpty(credentials.accountId);

        }
        private bool CheckValidCredentials(Credentials credentials)
        {
            return !string.IsNullOrEmpty(streamName) &&
                   !string.IsNullOrEmpty(credentials.url) &&
                   !string.IsNullOrEmpty(credentials.token) &&
                   !string.IsNullOrEmpty(credentials.accountId);
        }
        private string GetCredentialsErrorMessage(Credentials credentials)
        {
            string message = "";
            if(string.IsNullOrEmpty(streamName))
                return  "Stream Name cannot be Empty.Please add Stream Name from Inspector";
             if(string.IsNullOrEmpty(credentials.accountId))
                message = "Stream Account ID";
            if(string.IsNullOrEmpty(credentials.url))
                message += string.IsNullOrEmpty(message) ? "Publish URL" : ", Publish URL";
            if(string.IsNullOrEmpty(credentials.token))
                message += string.IsNullOrEmpty(message) ? "Publish token" : ", Publish token";
           
            return message + " can't be Empty. Please configure in Credentials Scriptable Object";
        }


        /// <summary>
        /// Remove all existing audio tracks associated with senders 
        /// </summary>
        private void RemoveAudioTracks()
        {
            foreach (var sender in _rtpSenders)
            {
                if (sender.Track is AudioStreamTrack)
                {
                    _pc.RemoveTrack(sender.Track);
                }
            }
            _rtpSenders.Clear();
        }

        /// <summary>
        /// Returns the url link for the preview video of the published content 
        /// </summary>
        private string GetPreviewURL()
        {
            string url = PREVIEW_URL.Replace("{{accountId}}", credentials.accountId);
            url = url.Replace("{{streamName}}", streamName);
            string link = $"<a href=\"{url}\">{url}</a>";
            return link;
        }

        /// <summary>
        /// <c>Used to create a streaming camera copy which will be used to capture video stream</c> must be called before passing the camera to webRTC.
        /// </summary>
        private Camera CopyCamera(Camera cam)
        {
            if (_publishingCamera != null)
                Destroy(_publishingCamera.gameObject);
            Camera tempCam = new GameObject(CopyCameraName).AddComponent<Camera>();
            tempCam.CopyFrom(cam);
            tempCam.transform.SetParent(cam.transform);
            return tempCam;
        }

        /// <summary>
        /// Set the video source for capture. Call with null as the source to remove
        /// the currently set video source.
        /// </summary>
        /// <param name="source">The camera source to capture from</param>
        /// <param name="resolution">The capturing resolution</param>
        public void SetVideoSource(Camera source, StreamSize resolution = null)
        {
            if (resolution == null)
            {
                resolution = _videoConfigData.pStreamSize;
            }

            // Remove all senders
            if (source == null)
            {
                // We will also replace the old track if it exists
                foreach (var sender in _rtpSenders)
                {
                    if (sender.Track is VideoStreamTrack)
                    {
                        _pc.RemoveTrack(sender.Track);
                    }
                }
                _rtpSenders.Clear();
                return;
            }
            videoSourceType = VideoSourceType.Camera;
            _publishingCamera = CopyCamera(source);

            _videoTrack = _publishingCamera.CaptureStreamTrack(resolution.width, resolution.height);
            _renderer.SetTexture(_publishingCamera.targetTexture);

            // We will also replace the old track if it exists
            foreach (var sender in _rtpSenders)
            {
                if (sender.Track is VideoStreamTrack)
                {
                    sender.ReplaceTrack(_videoTrack);
                }
            }
        }

        /// <summary>
        /// Set the video source for capture. Call with null as the source to remove
        /// the currently set video source.
        /// </summary>
        /// <param name="source">The Target Render Texture source to capture from</param>
        /// <param name="resolution">The capturing resolution</param>
        public void SetVideoSource(RenderTexture source, StreamSize resolution = null)
        {
            if (resolution == null)
            {
                resolution = _videoConfigData.pStreamSize;
            }
            // Remove all senders
            if (source == null)
            {
                // We will also replace the old track if it exists
                foreach (var sender in _rtpSenders)
                {
                    if (sender.Track is VideoStreamTrack)
                    {
                        _pc.RemoveTrack(sender.Track);
                    }
                }
                _rtpSenders.Clear();
                return;
            }
            videoSourceType = VideoSourceType.RenderTexture;
            _videoTrack = CreateRenderTextureStreamTrack(source);

            // We will also replace the old track if it exists
            foreach (var sender in _rtpSenders)
            {
                if (sender.Track is VideoStreamTrack)
                {
                    sender.ReplaceTrack(_videoTrack);
                }
            }
        }

        protected VideoStreamTrack CreateRenderTextureStreamTrack(RenderTexture targetTexture)
        {
            RenderTexture rt = null;

            if (targetTexture != null)
            {
                rt = targetTexture;
                RenderTextureFormat supportFormat = WebRTC.GetSupportedRenderTextureFormat(SystemInfo.graphicsDeviceType);
                GraphicsFormat graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(supportFormat, RenderTextureReadWrite.Default);
                GraphicsFormat compatibleFormat = SystemInfo.GetCompatibleFormat(graphicsFormat, FormatUsage.Render);
                GraphicsFormat format = graphicsFormat == compatibleFormat ? graphicsFormat : compatibleFormat;

                if (rt.graphicsFormat != format)
                {
                    Debug.LogWarning(
                        $"This color format:{rt.graphicsFormat} not support in unity.webrtc. Change to supported color format:{format}.");
                    rt.Release();
                    rt.graphicsFormat = format;
                    rt.Create();
                }
            }
            else
            {
                RenderTextureFormat format = WebRTC.GetSupportedRenderTextureFormat(SystemInfo.graphicsDeviceType);
                rt = new RenderTexture(_videoConfigData.pStreamSize.width, _videoConfigData.pStreamSize.height, 0, format)
                {
                    antiAliasing = 1
                };
                rt.Create();
            }
            _renderer.SetTexture(rt);
            return new VideoStreamTrack(rt);
        }

        /// <summary>
        /// Set the audio source to be captured when publishing. This will replace any
        /// previously set AudioSource through <see cref="SetAudioSource"/>
        /// </summary>
        /// <param name="source"> A Unity <see cref="AudioSource"/> instance</param>
        public void SetAudioSource(AudioSource source)
        {
            // Remove all senders
            if (source == null)
            {
                RemoveAudioTracks();
                return;
            }

            // Source already added.
            if (_audioTrack?.Source == source) return;

            _audioTrack = new AudioStreamTrack(source);
            _audioSource = source;
            _useAudioListenerAsSource = false;

            _renderer.SetAudioTrack(_audioTrack);

            // We will also replace the old track if it exists
            foreach (var sender in _rtpSenders)
            {
                if (sender.Track is AudioStreamTrack)
                {
                    sender.ReplaceTrack(_audioTrack);
                }
            }
        }

        /// <summary>
        /// Set the current AudioListener in the scene as an audio input to publishing. 
        /// Resets previously set AudioSource through <see cref="SetAudioSource"/>.
        /// Throws an Exception if the game object does not contain an AudioListener.
        /// </summary>
        public void SetAudioListenerAsSource()
        {

            if (gameObject.GetComponent<AudioListener>() == null)
            {
                throw new Exception("The current gameobject object does not contain an AudioListener");
            }

            _audioTrack = new AudioStreamTrack();
            gameObject.AddComponent<AudioSender>().SetAudioTrack(_audioTrack);
            _useAudioListenerAsSource = true;
            _renderer.SetAudioTrack(_audioTrack);

            // We will also replace the old track if it exists
            foreach (var sender in _rtpSenders)
            {
                if (sender.Track is AudioStreamTrack)
                {
                    sender.ReplaceTrack(_audioTrack);
                }
            }
        }

        private void UpdatePeerConnectionParameters()
        {
            if (_videoConfig == null) return;

            if (_rtpSenders != null)
            {
                foreach (var rtpSender in _rtpSenders)
                {
                    // Skip audio tracks
                    if (rtpSender.Track is AudioStreamTrack) continue;

                    var parameters = rtpSender.GetParameters();
                    foreach (var encoding in parameters.encodings)
                    {
                        if (_videoConfig.maxBitrate > 0)
                        {
                            encoding.maxBitrate = _videoConfig.maxBitrate * 1000;
                        }
                        if (_videoConfig.minBitrate >= 0)
                        {
                            encoding.minBitrate = _videoConfig.minBitrate * 1000;
                        }
                        if (_videoConfig.maxFramerate > 0)
                        {
                            encoding.maxFramerate = _videoConfig.maxFramerate;
                        }
                        if (_videoConfig.resolutionDownScaling >= 1.0)
                        {
                            encoding.scaleResolutionDownBy = _videoConfig.resolutionDownScaling;
                        }
                    }
                    rtpSender.SetParameters(parameters);
                }
            }
        }

        private void UpdateVideoQualitySettings()
        {
            if (_videoConfig == null)
                _videoConfig = new VideoConfig();
            _videoConfig.maxBitrate = (uint)_videoConfigData.pQualitySettings.pMaxBitrate;
            _videoConfig.minBitrate = (uint)_videoConfigData.pQualitySettings.pMinBitrate;
            _videoConfig.maxFramerate = (uint)_videoConfigData.pQualitySettings.pFramerateOption;
            _videoConfig.resolutionDownScaling = (double)_videoConfigData.pQualitySettings.pScaleDownOption;
            options.videoCodec = _videoConfigData.pCodecType;
            //stream size will be taken from video settings in SetVideoSource method
        }

        /// <summary>
        /// Update video configuration. Can be called
        /// while publishing; config will be applied
        /// in real-time even while publishing.
        /// <param name="config"> Video Configuration.</param>
        /// </summary>
        public void SetVideoConfig(VideoConfig config)
        {
            _videoConfig = config;

            if (_pc != null)
            {
                UpdatePeerConnectionParameters();
            }
        }

        void Start()
        {
            if (_publishOnStart)
            {
                Publish();
            }

        }
        private void CheckAudioVideoSource()
        {
            if (!_useAudioListenerAsSource && _audioSource == null)
            {
                Debug.Log("Video being published without Audio..");
            }
            if (_videoTrack == null && ((videoSourceType == VideoSourceType.Camera && _videoSourceCamera == null) || 
                (videoSourceType == VideoSourceType.RenderTexture && _videoSourceRenderTexture == null)))
                throw new Exception("Please assign Video Stream Source in Insector");

        }

        private void CheckVideoSettings()
        {
            if (_videoConfigData == null)
            {
                _videoConfigData = ScriptableObject.CreateInstance<VideoConfiguration>();
            }
            UpdateVideoQualitySettings();
        }

        /// <summary>
        /// Publish a stream.
        /// </summary>
        public void Publish()
        {

            // Reset the state before publishing
            Reset();
            CheckAudioVideoSource();
            videoConfigData?.ValidateResolution();

            if (videoSourceType == VideoSourceType.Camera && _videoSourceCamera != null)
            {
                SetVideoSource(_videoSourceCamera);
            }
            else if (videoSourceType == VideoSourceType.RenderTexture && _videoSourceRenderTexture != null)
            {
                SetVideoSource(_videoSourceRenderTexture);
            }

            // Preference for AudioListener first, unless AudioSource is set.
            if (_useAudioListenerAsSource)
            {
                SetAudioListenerAsSource();
            }
            else if (_audioSource != null)
            {
                SetAudioSource(_audioSource);
            }

            // Prioritise UI creedntials

            if (CheckValidCredentials(_credentials) || credentials == null)
            {
                credentials = new Credentials(_credentials, false);
            }
            if (!CheckValidCredentials(credentials))
            {
                throw new Exception(GetCredentialsErrorMessage(credentials));
            }

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
        /// UnPublish a stream.
        /// </summary>
        public void UnPublish()
        {
            _signaling?.Send(ISignaling.Event.UNPUBLISH);
            Reset();
        }

        /// <summary>
        /// Add a material to display the local video stream on. The material's main texture
        /// will be replaced with the local video stream texture. Using this
        /// to replace a GUI component's material will not work. Use the RawImage overload instead.  
        /// </summary>
        /// <param name="material">A Unity <see cref="Material"/>.</param>
        public void AddVideoRenderTarget(Material material)
        {
            _renderer.AddVideoTarget(material);
        }

        /// <summary>
        /// Stop rendering the local stream on the previously given material
        /// </summary>
        /// <param name="material">A Unity <see cref="Material"/>. </param>
        public void RemoveVideoRenderTarget(Material material)
        {
            _renderer.RemoveVideoTarget(material);
        }

        /// <summary>
        /// Stop rendering the local stream on the previously given RawImage.
        /// </summary>
        /// <param name="image">A Unity <see cref="RawImage"/>. </param>
        public void RemoveVideoRenderTarget(RawImage image)
        {
            _renderer.RemoveVideoTarget(image);
        }

        /// <summary>
        /// Add a UI RawImage to display the local video stream on. The RawImage's texture
        /// will be replaced with the local video stream texture when it is available. Use this
        /// when you want to use render the remote stream in a GUI.
        /// </summary>
        /// <param name="image">A Unity <see cref="RawImage"/>.</param>
        public void AddVideoRenderTarget(RawImage image)
        {
            _renderer.AddVideoTarget(image);
        }


        /// <summary>
        /// Add an audio source that will render the local audio stream.
        /// </summary>
        /// <param name="source"> A Unity <see cref="AudioSource"/> instance. </param>
        public void AddRenderAudioSource(AudioSource source)
        {
            _renderer.AddAudioTarget(source);
        }

        /// <summary>
        /// Remove an audio source so that it stops rendering the local stream.
        /// </summary>
        /// <param name="source"> A previously added Unity <see cref="AudioSource"/> instance. </param>
        public void RemoveRenderAudioSource(AudioSource source)
        {
            _renderer.RemoveAudioTarget(source);
        }

        // Update is called once per frame
        private void Update()
        {
            _signaling?.Dispatch();
        }

        private void OnDestroy()
        {
            Debug.Log("Millicast Application ending after " + Time.time + " seconds in OnDestroy");
            _signaling?.Disconnect();
            Reset();
            if (_publishingCamera != null)
                Destroy(_publishingCamera.gameObject);
        }
    }
}
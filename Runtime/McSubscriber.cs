using System.Collections;
using Dolby.Millicast;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.WebRTC;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace Dolby.Millicast
{
    [System.Serializable]
    public class SimulcastEvent : UnityEvent<McSubscriber, SimulcastInfo>
    {

    }

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
        [Tooltip("Subscribe as soon as the script starts")]
        private bool _subscribeOnStart = false;

        /// <summary>
        /// If the current object contains a mesh renderer,
        /// enabling this will rename the mesh renderer's material
        /// to the stream name, and render the incoming stream on it.
        /// </summary>
        [Header("\nVideo Settings: \n")]
        [SerializeField]
        [Tooltip("Enabling will render incoming stream onto mesh renderer's material, if it exists.")]
        private bool _updateMeshRendererMaterial = false;

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

        [Header("\nAudio Settings :\n")]

        public AudioOutputType audioOutputType;

        [Tooltip("default Audio configuration.")]
        [SerializeField][DrawIf("audioOutputType", AudioOutputType.Auto)]
        private AudioConfiguration defaultAudioConfiguration;

        [Tooltip("Audio Anchor Transform sets the position within the scene where the audio audio rendering will be attached. This can be camera, or a game object, the audio rendering will be pinned to that game object as it moves within the scene")]
        [SerializeField][DrawIf("audioOutputType", AudioOutputType.Auto)]
        private Transform audioAnchorTransform;
        [System.Serializable]
        public class RenderAudioSources
        {
            public List<AudioSource> audioSources;
        }

	private string getPrefabName(int channelCount)
	{
	    switch (channelCount)
	    {
	        case 2:
	    	return "Stereo_Speakers";
	        case 6: 
	    	return "Five_One_Speakers"; 
	        default:
	    	return "";
	    }
	}

        /// <summary>
        /// Manually set the audio sources to render to. This
        /// is used when you want utilise the Unity Inspector UI.
        /// </summary>
        [Tooltip("Add your AudioSources here to render incoming audio streams, will work only for stereo incoming audio types.")]
        [SerializeField][DrawIf("audioOutputType", AudioOutputType.AudioSource)]
        public RenderAudioSources OutputAudioSources;

        [SerializeField][DrawIf("audioOutputType", AudioOutputType.VirtualSpeakers)]
        public VirtualAudioSpeaker OutputAudioSpeakers;

        private VirtualAudioSpeaker _defaultAudioSpeaker;
        private AudioSource _defaultAudioSource;
        private List<AudioSource> _renderAudioSources => OutputAudioSources.audioSources;
        public List<AudioSource> renderAudioSources
        {
            get => this._renderAudioSources;
        }

        [Header("\nEvent Listeners :\n")]
        [SerializeField] private SimulcastEvent simulcastEvent;
        private SimulcastInfo simulCastInfo;


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
        public delegate void DelegateOnLayerEvent(McSubscriber subscribe, SimulcastInfo info);
        /// <summary>
        /// Event called when the Simulcast Layers data event triggered.
        /// </summary>
        public event DelegateOnLayerEvent OnSimulcastlayerInfo;
        /// <summary>
        /// Event called when the there is a connection error to the service.
        /// </summary>
        public event DelegateOnConnectionError OnConnectionError;
        private int channelsCount = -1;


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
            payload["events"] = new string[] { "active", "inactive", "stopped", "viewercount", "layers" };

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
                    case ISignaling.Event.LAYERS:
                        try
                        {
                            simulCastInfo = DataContainer.ParseSimulcastLayers(payload.medias);
                            OnSimulcastlayerInfo?.Invoke(this, simulCastInfo);
                            simulcastEvent?.Invoke(this, simulCastInfo);
                        }
                        catch (System.Exception exception)
                        {
                            Debug.LogError("Failed to parse the Simulcast layers data: " + exception.Message);
                        }
                        break;
                }
            };
            StartCoroutine(AwaitSignalingMessages());
        }
        /// <summary>
        /// Returns the simulcast layers available for the incoming video stream if its a simulcast stream.
        /// Returns null if the stream is not simulcast.
        /// </summary>
        public Layer[] GetSimulcastLayers()
        {
            if (simulCastInfo != null)
                return simulCastInfo.Layers;
            return null;
        }

        private void OnSimulcastLayerEvent(McSubscriber subscriber, SimulcastInfo info)
        {
            string text = "Active Simulcast Data: \n";
            foreach (var item in info.Active)
            {
                text += "simulcast Id: " + item.Id + " , Bitrate: " + item.Bitrate;
                foreach (var layer in item.Layers)
                {
                    text += "\n\t layer: " + layer.TemporalLayerId + ", Bitrate: " + layer.Bitrate + ", temporal layer id: " + layer.TemporalLayerId + ", spatial layer id: " + layer.SpatialLayerId;
                }
                text += "\n";
            }
            Debug.Log(text);
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
        /// <exception cref="Exception"></exception>
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
                _pc.CheckStats();
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
            _pc.SetUp(_signaling, _rtcConfiguration, true);
        }

        void Awake()
        {
            // Http Authentication
            _httpAuthenticator = new HttpAuthenticator();
            bool checkForStreamCoroutineCalled = false;
            _httpAuthenticator.OnError += (msg) =>
            {
                if (msg.Contains("Unauthorized"))
                {
                    Debug.Log("[HTTPAuthenticator] " + msg);
                }
                else
                {
                    Debug.Log("[HTTPAuthenticator] " + msg + "\nWaiting for incoming Stream...");
                }
                OnConnectionError?.Invoke(this, msg);
                if (msg.Contains("Unauthorized")) return;
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
            OnSimulcastlayerInfo += OnSimulcastLayerEvent;
            if (_subscribeOnStart)
            {
                Subscribe();
            }
        }
        private void Update()
        {
            _signaling?.Dispatch();
            if (_pc != null && _pc.getInboundChannelCount > 0 && channelsCount != _pc.getInboundChannelCount)
            {
                channelsCount = _pc.getInboundChannelCount;
                AddAudioRenderer(channelsCount);
            }
        }

        private void AddAudioRenderer(int channelCount)
        {
            bool needsVirtualizing = channelCount > AudioHelpers.GetAudioSpeakerModeIntFromEnum(AudioSettings.driverCapabilities) ?
                true : false;

            switch (audioOutputType)
            {
                case AudioOutputType.Auto:
                    if (needsVirtualizing)
                    {
                        if (_defaultAudioSource != null)
                        {
                            RemoveRenderAudioSource(_defaultAudioSource);
                        }

                        VirtualAudioSpeaker defaultSpeaker = CreateVirtualSpeaker(channelCount);
                        if (defaultSpeaker == null)
                        {
                            if (channelCount == 6)
                                defaultSpeaker.SetChannelMap(_pc.getChannelMap);

                            if (defaultSpeaker.GetChannelCount() > channelCount)
                            {
                                defaultSpeaker.StopAll();
                            }

                            _renderer.AddVirtualAudioSpeaker(defaultSpeaker, _pc.getInboundChannelCount);
                        }

                        defaultSpeaker.SetChannelMap(_pc.getChannelMap);
                        _renderer.AddVirtualAudioSpeaker(defaultSpeaker, _pc.getInboundChannelCount);
                    } 
                    else
                    {
                        if (_defaultAudioSource == null)
                        {
                            var audioAnchorObject = audioAnchorTransform != null ? audioAnchorTransform.gameObject : gameObject;
                            _defaultAudioSource = audioAnchorObject.AddComponent<AudioSource>();
                            if (defaultAudioConfiguration != null)
                            {
                                defaultAudioConfiguration.LoadData(_defaultAudioSource);
                            }
                        }

                        if (_defaultAudioSpeaker != null)
                        {
                            _defaultAudioSpeaker.StopAll();
                        }

                        AddRenderAudioSource(_defaultAudioSource);
                    }
                    break;

                case AudioOutputType.AudioSource:
                    if (_renderAudioSources == null || _renderAudioSources.Count < 1)
                        throw new Exception("Audio Source not mapped");

                    // Audio cannot be played out as is, so we throw an exception
                    if (needsVirtualizing)
                    {
                        throw new Exception("Audio Driver capabilities cannot play out incoming channel count");
                    }

                    foreach (var audioSource in _renderAudioSources)
                        AddRenderAudioSource(audioSource);
                    break;

                case AudioOutputType.VirtualSpeakers:
                    if (OutputAudioSpeakers == null)
                    {
                        throw new Exception("Virtual Speaker not mapped");
                    }

                    if (OutputAudioSpeakers.GetChannelCount() < channelCount)
                    {
                        throw new Exception($"Virtual Speaker cannot play incoming channel count {channelCount}");
                    }

                    if (OutputAudioSpeakers.GetChannelCount() > channelCount)
                    {
                        OutputAudioSpeakers.StopAll();
                    }

                    if (channelCount == 6)
                    {
                        OutputAudioSpeakers.SetChannelMap(_pc.getChannelMap);
                    }
                    _renderer.AddVirtualAudioSpeaker(OutputAudioSpeakers, _pc.getInboundChannelCount);
                    break;
            }
        }

        private VirtualAudioSpeaker CreateVirtualSpeaker(int channelCount)
        {
            // No need to recreate an audio speaker if
            // there is an existing one that supports the incoming
            // channel count
            if (_defaultAudioSpeaker != null && channelCount <= _defaultAudioSpeaker.GetChannelCount())
                return _defaultAudioSpeaker;
            else
                GameObject.Destroy(_defaultAudioSpeaker);

            if (audioAnchorTransform == null)
                audioAnchorTransform = transform;
            
            string prefabName = getPrefabName(channelCount);
            
            if(string.IsNullOrEmpty(prefabName))
                return null;

            GameObject obj = Instantiate(Resources.Load(prefabName) as GameObject, audioAnchorTransform);
            _defaultAudioSpeaker = obj.GetComponent<VirtualAudioSpeaker>();
            if (defaultAudioConfiguration != null)
                _defaultAudioSpeaker.UpdateAudioConfiguration(defaultAudioConfiguration);
            return _defaultAudioSpeaker;
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
            channelsCount = -1;
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
        private string GetCredentialsErrorMessage(Credentials credentials)
        {
            string message = "";
            if (string.IsNullOrEmpty(streamName))
                return "Stream Name cannot be Empty.Please add Stream Name from Inspector";

            if (string.IsNullOrEmpty(credentials.accountId))
                message = "Stream Account ID";
            
            if (string.IsNullOrEmpty(credentials.url))
                message += string.IsNullOrEmpty(message) ? "Subscriber URL" : ", Subscriber URL";

            return message + " can't be Empty. Please configure in Credentials Scriptable Object";
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
        }


        /// <summary>
        /// This will render incoming streams onto the mesh renderer
        /// of this object, if it exists. 
        /// </summary>
        public void UpdateMeshRendererMaterial()
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
        /// This will clear all render material Targets for the incoming video stream
        /// </summary>
        public void ClearRenderMaterials()
        {
          _renderMaterials.Clear();
          _renderer.Clear();
        }
        /// <summary>
        /// Subscribe to a stream.
        /// </summary>
        public void Subscribe()
        {

            // Reset if currently subscribing
            Reset();

            // Prioritise UI credentials
            if (CheckValidCredentials(_credentials) || credentials == null)
            {
                credentials = new Credentials(_credentials, true);
            }
            if (!CheckValidCredentials(credentials))
            {
                throw new Exception(GetCredentialsErrorMessage(credentials));
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
        private SimulcastInfo GetSimulcastInfo()
        {
            return simulCastInfo;
        }
        /// <summary>
        /// Set a simulcast layer
        /// </summary>
        /// <param name="layer"> Expects Layer object which can be found in Layers class in SimulcastInfo.  </param>
        public void SetSimulcastLayer(Layer layer)
        {
            if(layer != null)
            {
                var layerpayload = new Dictionary<string, dynamic>();
                var payload = new Dictionary<string, dynamic>();
                payload["encodingId"] = layer.EncodingId;
                payload["spatialLayerId"] = layer.SpatialLayerId;
                payload["temporalLayerId"] = layer.TemporalLayerId;
                layerpayload["layer"] = payload;
                _signaling.Send(ISignaling.Event.SELECT, layerpayload);
            }
            else
                Debug.Log("Selected Layer not found");
        }
    }

}

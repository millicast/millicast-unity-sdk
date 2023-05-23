﻿using System.Collections;
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
    [System.Serializable]
    public class ActiveSourceEvent : UnityEvent<McSubscriber, ProjectionData>
    {

    }

    public class ProjectionData
    {
        public string sourceId;
        public List<TrackData> tracks = new List<TrackData>();        
    }

    public class TrackData
    {
        public string TrackId;

        public string Mid;

        public string Media;

        public bool waitForTrack;
        public RTCRtpTransceiver transceiver;
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


        [SerializeField] public MediaRenderers defaultMediaRenderer = new MediaRenderers();

        [SerializeField] private List<MultiSourceMediaRenderer> multiSourceMediaList = new List<MultiSourceMediaRenderer>();
       // public AdvancedAudioConfig AdvancedAudioConfiguration;
       
        [SerializeField] private SimulcastEvent simulcastEvent;
        [SerializeField] private ActiveSourceEvent activeSourceEvent;

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
        public delegate void DelegateOnActiveEvent(McSubscriber subscribe, ProjectionData projectionData);
        /// <summary>
        /// Event called when the Simulcast Layers data event triggered.
        /// </summary>
        public event DelegateOnLayerEvent OnSimulcastlayerInfo;
        public event DelegateOnActiveEvent OnActiveEventInfo;
        /// <summary>
        /// Event called when the there is a connection error to the service.
        /// </summary>
        public event DelegateOnConnectionError OnConnectionError;
        private int channelsCount = -1;
        private List<ProjectionData> activeProjections = new List<ProjectionData>();
        private ProjectionData activeProjection;
        private Queue<ProjectionData> incomingStreamQue = new Queue<ProjectionData>();

        private MultiSourceMediaRenderer GetMediaRenderer(string sourceId)
        {
            return multiSourceMediaList.Find(x => x.sourceId.Equals(sourceId));
        }
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
            payload["events"] = new string[] { "active", "inactive", "stopped", "viewercount","layers" };

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
                    case ISignaling.Event.ACTIVE:
                        Debug.Log("Active Event received:"+payload.sourceId);
                        if(!string.IsNullOrEmpty(payload.sourceId))
                        {
                            ProjectionData projectionData = OnActiveEvent(payload.sourceId, payload.tracks);
                            if(projectionData != null)
                            {
                                OnActiveEventInfo?.Invoke(this, projectionData);
                                activeSourceEvent?.Invoke(this, projectionData);
                            }  
                        } 
                        break;
                    case ISignaling.Event.INACTIVE:
                        Debug.Log("In Active Event received:"+payload.sourceId);
                        if(!string.IsNullOrEmpty(payload.sourceId))
                           OnInActiveEvent(payload.sourceId);
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
                            Debug.LogError("Failed to parse the Simulcast layers data: "+exception.Message);
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
        private Layer[] GetSimulcastLayers()
        {
            if(simulCastInfo != null)
                return simulCastInfo.Layers;
            return null;
        }

        private void OnSimulcastLayerEvent(McSubscriber subscriber, SimulcastInfo info)
        {
            string text = "Active Simulcast Data: \n";
            foreach (var item in info.Active)
            {
                text += "simulcast Id: "+item.Id+" , Bitrate: "+item.Bitrate;
                foreach (var layer in item.Layers)
                {
                    text += "\n\t layer: "+layer.TemporalLayerId+", Bitrate: "+layer.Bitrate+", temporal layer id: "+layer.TemporalLayerId+", spatial layer id: "+layer.SpatialLayerId;
                }  
                text +="\n";
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
                string sourceId = activeProjection != null ? activeProjection.sourceId : "";
                if (e.Track is VideoStreamTrack track)
                {
                    Debug.Log("[Subscriber] Received video track: "+track.Id);
                    UpdateMediaID(track.Id, "video");
                    track.OnVideoReceived += (tex) =>
                    {
                        Debug.Log("[Subscriber] Received video track texture");
                        SetTexture(tex, sourceId);
                    };
                }
                if (e.Track is AudioStreamTrack audioTrack)
                {
                    UpdateMediaID(audioTrack.Id, "audio");
                    SetAudioTrack(audioTrack, sourceId);
                }
            };
            _pc.SetUp(_signaling, _rtcConfiguration, true);
        }


        private void UpdateMediaID(string id, string mediaType)
        {
            if(activeProjection != null)
                AddVideoStreamMediaId(id, mediaType);
        }

        private void SetAudioTrack(AudioStreamTrack track, string sourceId)
        {
            if(string.IsNullOrEmpty(sourceId))
                defaultMediaRenderer.SetAudioTrack(track);
            else
            {
                MultiSourceMediaRenderer media = GetMediaRenderer(sourceId);
                if(media != null)
                    media.SetAudioTrack(track);
            }
        }

        private void SetTexture(Texture texture, string sourceId)
        {
            if(string.IsNullOrEmpty(sourceId))
                defaultMediaRenderer.SetTexture(texture);
            else
            {
                MultiSourceMediaRenderer media = GetMediaRenderer(sourceId);
                if(media != null)
                {
                    media.SetTexture(texture);
                }
            }
        }

        private void AddStream(string sourceId)
        {
            MultiSourceMediaRenderer media = GetMediaRenderer(sourceId);
            if(media != null)
            {
                media.AddStream(sourceId);
                media.AddRenderTargets();
                media.AddAudioRenderTargets();
            }
            else
                Debug.LogError("Failed to get mediaRenderer fr the source:"+sourceId);
        }

        private void AddVideoStreamMediaId(string sourceId, string type)
        {
            foreach (var item in activeProjection.tracks)
            {
                if(sourceId.Equals(item.transceiver.Receiver.Track.Id))
                {
                    item.Mid = item.transceiver.Mid;
                    item.waitForTrack = false;
                }    
            }
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
            OnActiveEventInfo += OnProjectInfoAvailable;
            if (_subscribeOnStart)
            {
                Subscribe();
            }
        }
        private void Update()
        {
            _signaling?.Dispatch();
            if(_pc != null && _pc.getInboundCannelCount > 0 && channelsCount != _pc.getInboundCannelCount)
            {
                channelsCount = _pc.getInboundCannelCount;
                MediaRenderers mediaRenderer = activeProjection != null ? GetMediaRenderer(activeProjection.sourceId) : defaultMediaRenderer;
                VirtualAudioSpeaker speaker = GetVirtualAudioSpeaker(mediaRenderer);
                if(speaker != null)
                {
                    speaker.SetChannelMap(_pc.getChannelMap);
                    mediaRenderer.AddVirtualAudioSpeaker(speaker, _pc.getInboundCannelCount);
                }
                else
                {
                    mediaRenderer.AddRenderAudioSource();
                }
                
            }   
        }

        private VirtualAudioSpeaker GetVirtualAudioSpeaker(MediaRenderers mediaRenderer)
        {
            // In the case where the hardware is capable of playing
            // the incoming number of channels, no need to virtualize.

            bool needsVirtualization = false;
            int hardwareSupportedChannelCount = AudioHelpers.GetAudioSpeakerModeIntFromEnum(AudioSettings.driverCapabilities);
            if ( hardwareSupportedChannelCount < _pc.getInboundCannelCount)
            {
                needsVirtualization = true;
            }
            Debug.Log($"Inbound channel count: {_pc.getInboundCannelCount}");
            Debug.Log($"Hardware supported channel count: {hardwareSupportedChannelCount}");
            Debug.Log($"Need to virtualize audio: {needsVirtualization}");
            switch (_pc.getInboundCannelCount)
            {
                case 2:
                    return mediaRenderer.virtualAudioSpeakers.Find( x => x.audioChannelType == VirtualSpeakerMode.Stereo);
                case 6:
                    VirtualAudioSpeaker speaker6 = mediaRenderer.virtualAudioSpeakers.Find( x => x.audioChannelType == VirtualSpeakerMode.Mode5point1);
                    if(speaker6 == null && needsVirtualization)
                    {
                        GameObject obj = Instantiate (Resources.Load("Five_One_Speaker") as GameObject, transform);
                        speaker6 = obj.GetComponent<VirtualAudioSpeaker>();
                    }
                    return speaker6;
                default:
                    return null;
            }
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

       


        /// <summary>
        /// This will render incoming streams onto the mesh renderer
        /// of this object, if it exists. 
        /// </summary>
        public void UpdateMeshRendererMaterial()
        {
            if(activeProjection == null)
                defaultMediaRenderer.UpdateMeshRendererMaterial(streamName, this.gameObject);
        }
        /// <summary>
        /// This will clear all render material Targets for the incoming video stream
        /// </summary>
        public void ClearRenderMaterials()
        {
          defaultMediaRenderer.ClearRenderMaterials();
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
            defaultMediaRenderer.AddRenderTargets();
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

       
        private SimulcastInfo GetSimulcastInfo()
        {
            return simulCastInfo;
        }

        public void AddVideoRenderTarget(Material material)
        {
            defaultMediaRenderer.AddVideoRenderTarget(material);
        }

        public void AddVideoRenderTarget(RawImage image)
        {
            defaultMediaRenderer.AddVideoRenderTarget(image);
        }

         public void AddRenderAudioSource(AudioSource source)
        {
            defaultMediaRenderer.AddRenderAudioSource(source);
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

        private void OnProjectInfoAvailable(McSubscriber subscriberInstance, ProjectionData projectionData)
        {
            if(activeProjection != null)
                incomingStreamQue.Enqueue(projectionData);
            else
                ProcessStream(projectionData);
        }

        private void ProcessPendingStreams()
        {
            if(activeProjection != null)
                return;
            
            if(incomingStreamQue.TryDequeue(out ProjectionData projectionData))
                ProcessStream(projectionData);
        }

        private ProjectionData OnActiveEvent(string sourceId, List<TrackInfo> tracks)
        {
            
            ProjectionData projectionData = new ProjectionData();
            projectionData.sourceId = sourceId;
             foreach(var track in tracks)
            {
               TrackData data = new TrackData();
                data.Media = track.media;
                data.TrackId = track.trackId;
                data.Mid = "";
                projectionData.tracks.Add(data);
            }
            return projectionData;
        }
        private void ProcessStream(ProjectionData projectionData)
        {
            if(GetMediaRenderer(projectionData.sourceId) == null)
                return;
             AddStream(projectionData.sourceId);
             RTCRtpTransceiverInit init = new RTCRtpTransceiverInit();
             init.direction = RTCRtpTransceiverDirection.RecvOnly;
            foreach(var track in projectionData.tracks)
            {
                track.Mid = "";
                if(track.Media.ToLower().Equals("audio"))
                {
                   track.transceiver =  _pc.AddTransceiver(TrackKind.Audio, init);
                   track.waitForTrack = true;
                    Debug.Log("added audio transceiver: "+track.transceiver.Receiver.Track.Id);
                }
                else if(track.Media.ToLower().Equals("video"))
                {
                   track.transceiver = _pc.AddTransceiver(TrackKind.Video, init);
                   track.waitForTrack = true;
                    Debug.Log("added video transceiver: "+track.transceiver.Receiver.Track.Id);
                }
            }
            activeProjection = projectionData;
            StartCoroutine(CheckForTracks());
        }

        IEnumerator CheckForTracks()
        {
             while(activeProjection.tracks[0].waitForTrack)
                yield return new WaitForSeconds(1);

              while(activeProjection.tracks[1].waitForTrack)
                yield return new WaitForSeconds(1);

            /*TrackData audioTrackData = activeProjection.tracks.Find(x => x.TrackId.Equals("audio"));
             if(audioTrackData.transceiver.Receiver.Track is AudioStreamTrack audioTrack)
                {
                    SetAudioTrack(audioTrack, activeProjection.sourceId);
                    Debug.Log("MID for audio:"+audioTrackData.transceiver.Mid);
                }  
                TrackData videoTrackData = activeProjection.tracks.Find(x => x.TrackId.Equals("video"));
                if(videoTrackData.transceiver.Receiver.Track is VideoStreamTrack videoTrack)
                {
 
                    Debug.Log("MID for video:"+videoTrackData.transceiver.Mid);
                    videoTrack.OnVideoReceived += (tex) =>
                    {
                        Debug.Log("[Subscriber] Received video track texture");
                        SetTexture(tex, activeProjection.sourceId);
                    };
                }  

            Debug.Log("*********** Check Successfull *************");
            */
            Project(activeProjection);
        }

        private void OnInActiveEvent(string sourceId)
        {
            foreach(var projectionData in activeProjections)
            {
                if(!projectionData.sourceId.Equals(sourceId))
                    return;
                List<string> mids = new List<string>();
                foreach (var item in projectionData.tracks)
                {
                    mids.Add(item.Mid);
                }
                UnProject(mids);
                break;
            }
        }

        private void UnProject(List<string> mids)
        {
             var unprojectData = new Dictionary<string, dynamic>();
             unprojectData["mediaIds"] = mids.ToArray();
            _signaling.Send(ISignaling.Event.UNPROJECT, unprojectData);
        }

         public void Project(ProjectionData projectionData)
        {
            if(projectionData == null || string.IsNullOrEmpty(projectionData.sourceId) ||  projectionData.tracks == null)
                return;
           
            var projectionDatapayload = new Dictionary<string, dynamic>();
            List<Dictionary<string, string>> projectionsList = new List<Dictionary<string, string>>();
            foreach(var data in projectionData.tracks)
            {
                var info = new Dictionary<string, string>();
                info["trackid"] = data.TrackId;
                info["mediaId"] = data.Mid;
                info["media"] = data.Media;
                Debug.Log("Source Id:"+projectionData.sourceId+", track Id:"+data.TrackId+", mediaId:"+data.transceiver.Mid+", media: "+data.Media);
                projectionsList.Add(info);
            }
            
            projectionDatapayload["sourceId"] = projectionData.sourceId;
            projectionDatapayload["mapping"] = projectionsList.ToArray();
            activeProjections.Add(projectionData);
            Debug.Log("project added:"+projectionData.sourceId);
            _signaling.Send(ISignaling.Event.PROJECT, projectionDatapayload);
            activeProjection = null;
            ProcessPendingStreams();
        }

        void OnDisable()
        {
            foreach (var projectdata in activeProjections)
            {
               OnInActiveEvent(projectdata.sourceId);
            }
        }
    }

}
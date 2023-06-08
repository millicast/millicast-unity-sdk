using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Text;
using System.Threading.Tasks;

namespace Dolby.Millicast
{


  /// <summary>
  /// Holds track information for multisource  
  /// </summary>
  internal class TrackInfo
  {
    public string trackId { get; set; }
    public string media { get; set; }
  }

  /// <summary>
  /// This holds the possible data the service might return. 
  /// </summary>
  internal class ServiceResponseData
  {
    public string sdp { get; set; }
    public int viewercount { get; set; }
    public string streamId { get; set; }
    public string sourceId { get; set; }
    public object medias { get; set; }
    public List<TrackInfo> tracks;
  }
  internal class ServiceResponse
  {

    public string type { get; set; }
    public string name { get; set; }

  }

  internal class ErrorServiceResponse : ServiceResponse
  {
    public string data { get; set; }
  }
  internal class SuccessServiceResponse : ServiceResponse
  {
    public ServiceResponseData data { get; set; }
  }

  internal class SignalingImpl : ISignaling
  {
    private readonly static System.Random rnd = new System.Random();

    private readonly WebSocket _websocket;
    private readonly String _url;
    private readonly String _token;

    public event ISignaling.DelegateOnEvent OnEvent;
    public event ISignaling.DelegateOnOpen OnOpen;
    public event ISignaling.DelegateOnClose OnClose;
    public event ISignaling.DelegateOnError OnError;


    private ISignaling.Event ConvertToEventEnum(string e)
    {
      switch (e)
      {
        case "viewercount": return ISignaling.Event.VIEWER_COUNT;
        case "active": return ISignaling.Event.ACTIVE;
        case "inactive": return ISignaling.Event.INACTIVE;
        case "vad": return ISignaling.Event.VAD;
        case "layers": return ISignaling.Event.LAYERS;
        case "stopped": return ISignaling.Event.STOPPED;
        case "response": return ISignaling.Event.RESPONSE;

        default:
          throw new ArgumentException($"Invalid string name {e}");
      }
    }

    private void Setup()
    {

      _websocket.OnOpen += () =>
      {
        Debug.Log("WebSocket Connection Open.");
        OnOpen?.Invoke();
      };

      _websocket.OnError += (e) =>
      {
        Debug.Log("WebSocket Error " + e);
        OnError?.Invoke(e);
      };

      _websocket.OnClose += (e) =>
      {
        Debug.Log("WebSocket Connection Closed. Reason: " + e.ToString());
        OnClose?.Invoke(e.ToString());
      };

      _websocket.OnMessage += (bytes) =>
      {
        Debug.Log("WebSocket Bytes received:  " + bytes.Length + " bytes.");
        Debug.Log("\t data received:  " + bytes);

        // Forward to specific events
        string json = System.Text.Encoding.UTF8.GetString(bytes);
        JObject obj = JObject.Parse(json);
        ServiceResponse response = null;
        if (obj["data"] is JValue && ((string)obj["type"] == "error"))
        {
          response = JsonConvert.DeserializeObject<ErrorServiceResponse>(json);
        }
        else
        {
          response = JsonConvert.DeserializeObject<SuccessServiceResponse>(json);
        }
        // var dict = JsonConvert.DeserializeObject<ServiceResponse<Dictionary<string, string>>(json);

        switch (response.type)
        {
          //TODO: We should separate both
          case "response":
          case "event":
            HandleEvent((SuccessServiceResponse)response);
            break;
          case "error":
            HandleError((ErrorServiceResponse)response);
            break;
          default:
            throw new WebSocketException("Server responded with invalid message type");
        };

      };

    }

    private void HandleEvent(SuccessServiceResponse response)
    {

      switch (response.type)
      {
        case "response":
          OnEvent?.Invoke(ISignaling.Event.RESPONSE, response.data);
          break;
        case "event":
          OnEvent?.Invoke(ConvertToEventEnum(response.name), response.data);
          break;
        default:
          throw new WebSocketException($"Unhandled message type received from server: {response.type}");
      }
    }

    private void HandleError(ErrorServiceResponse response)
    {
      OnError?.Invoke(response.data);
    }

    private Dictionary<string, object> PreparePayload(string name)
    {
      var payload = new Dictionary<string, object>();
      payload["type"] = "cmd";
      payload["transId"] = rnd.Next(1000);
      payload["name"] = name;
      return payload;
    }

    private Dictionary<string, object> PreparePayload(string name, ref Dictionary<string, object> data)
    {
      var payload = new Dictionary<string, object>();
      payload["type"] = "cmd";
      payload["transId"] = rnd.Next(1000);
      payload["name"] = name;
      payload["data"] = data;

      return payload;
    }

    public SignalingImpl(string url, string token)
    {
      _url = url;
      _token = token;
      var fullUrl = _url + "?token=" + _token;
      string os_info = SystemInfo.operatingSystem;
      string os_name = OSDetails.GetOSName(os_info);
      string os_version = OSDetails.GetOSVersion(os_info);
      string plugin_version = Millicast.GetPackageVersion();
      string device_model = SystemInfo.deviceModel;
      //<SDK Name>/<SDK version> (<OS>/<OS Version>; <extra info>;<Unity/version>)
      var userAgent = $"UnitySDK/{plugin_version} ({os_name}/{os_version}; {device_model}; Unity/{Application.unityVersion})";
      var websocketHeaders = new Dictionary<string, string>() { { "User-Agent", userAgent } };

      _websocket = new WebSocket(fullUrl, websocketHeaders);
      Setup();
    }

    public Task Connect()
    {
      return _websocket?.Connect();
    }

    public Task Disconnect()
    {
      return _websocket?.Close();
    }

    public Task Send(ISignaling.Event e, Dictionary<string, object> data)
    {

      if (_websocket?.State != WebSocketState.Open)
      {
        Debug.Log($"Error sending to Websocket. Connection closed.");
        return null;
      }

      Dictionary<string, object> payload;
      switch (e)
      {
        case ISignaling.Event.PUBLISH:
          if (data == null)
            throw new Exception("Must provide data with publish event.");
          payload = PreparePayload("publish", ref data);
          break;
        case ISignaling.Event.UNPUBLISH:
          payload = PreparePayload("unpublish");
          break;
        case ISignaling.Event.SUBSCRIBE:
          payload = PreparePayload("view", ref data);
          break;
         case ISignaling.Event.SELECT:
          payload = PreparePayload("select", ref data);
          break;
        default:
          //TODO: Implement other events
          throw new NotImplementedException();
      }

      var jsonString = JsonConvert.SerializeObject(payload);
      return _websocket.SendText(jsonString);
    }

    bool ISignaling.IsConnected()
    {
      return _websocket?.State == WebSocketState.Open;
    }

    public void Dispatch()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
      _websocket.DispatchMessageQueue();
#endif
    }

    public Task Send(ISignaling.Event e)
    {
      return Send(e, null);
    }
  }
}



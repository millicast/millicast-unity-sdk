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
  internal class SignalingImpl : ISignaling
  {
    private readonly static System.Random rnd = new System.Random();

    private readonly WebSocket _websocket;
    private readonly string _url;
    private readonly string _token;
    private readonly Dictionary<string, ServiceResponse> _transactions = new();

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
        Debug.Log($"WebSocket Json received: {json}");
        ServiceResponse response = JObject.Parse(json).ToObject<ServiceResponse>();
        switch (response.Type)
        {
          case "response":
            HandleResponse(response);
            break;
          case "event":
            HandleEvent(response);
            break;
          case "error":
            HandleError(response);
            break;
          default:
            Debug.LogError($"Server responded with invalid message type {response.Type}");
            return;
        };
      };

    }

    private void HandleResponse(ServiceResponse responseData) {
      lock(_transactions) {
        _transactions.Add(responseData.TransactionId, responseData);
      }
    }

    private void HandleEvent(ServiceResponse response)
    {
      OnEvent?.Invoke(ConvertToEventEnum(response.Name), response.Data);
    }

    private void HandleError(ServiceResponse response)
    {
      OnError?.Invoke(response.Data.ToString());
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

    public Task<JToken> Send(ISignaling.Event e, Dictionary<string, object> data)
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
      var sendTask = _websocket.SendText(jsonString);
      return sendTask.ContinueWith((x) => {
        string transId = payload["transId"].ToString() ?? throw new Exception("No valid transaction ID");

        ServiceResponse response = null;
        bool hasValue = false;
        while (!hasValue) {
          lock(_transactions) {
            hasValue = _transactions.TryGetValue(transId, out response);
          }
        }

        if (response.Type == "error") {
          throw new Exception(response.Data.ToString());
        }
        return response.Data;
      });
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

    public Task<JToken> Send(ISignaling.Event e)
    {
      return Send(e, null);
    }
  }
}



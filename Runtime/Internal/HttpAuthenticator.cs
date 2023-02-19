using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using Unity.WebRTC;

using UnityEngine.Networking;

namespace Dolby.Millicast
{
  internal class HttpAuthenticator
  {
    private class IceServer
    {
      public string credential { get; set; }
      public string username { get; set; }
      public List<string> urls { get; set; }
    }

    private class ResponseData
    {
      public string jwt { get; set; }
      public List<string> urls { get; set; }
      public List<RTCIceServer> iceServers { get; set; }
      public string message { get; set; }
    }

    private class Response
    {
      public ResponseData data { get; set; }
    }

    public Credentials credentials { set; get; } = null;
    //private HttpClient _client = new HttpClient();

    public delegate void DelegateOnError(string errorMessage);
    public event DelegateOnError OnError;

    public delegate void DelegateOnWebsocketInfo(string url, string token);
    public event DelegateOnWebsocketInfo OnWebsocketInfo;

    public delegate void DelegateOnIceServers(ref RTCIceServer[] iceServers);
    public event DelegateOnIceServers OnIceServers;

    public HttpAuthenticator() { }

    public IEnumerator Connect(string streamName)
    {

      Dictionary<string, string> payload = new Dictionary<string, string>();

      if (streamName == null)
      {
        Debug.Log("Invalid streamName. Should not be null");
        yield break;
      }

      payload["streamName"] = streamName;
      if (credentials.accountId != null)
      {
        payload["streamAccountId"] = credentials.accountId;
      }

      UnityWebRequest www = new UnityWebRequest(credentials.url, "POST");
      // Set authorization request header
      if (credentials.token != null && credentials.token.Length > 0)
      {
        www.SetRequestHeader("Authorization", "Bearer " + credentials.token);
      }
      else
      {
        payload["unauthorizedSubscribe"] = "true";
        www.SetRequestHeader("Authorization", "NoAuth");
      }

      byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
      www.SetRequestHeader("Content-Type", "application/json");
      www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
      www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();


      yield return www.SendWebRequest();

      if (www.result == UnityWebRequest.Result.ConnectionError)
      {
        OnError?.Invoke(www.error);
        yield break;
      }
      string responseBody = www.downloadHandler.text;
      www.Dispose();

      // extract the Json object
      var dict = JsonConvert.DeserializeObject<Response>(responseBody);

      var data = dict.data;

      if (data.message != null)
      {
        OnError?.Invoke(data.message);
        yield break;
      }

      // Handle websocket data
      string ws_url = data.urls[0];
      string ws_token = data.jwt;
      OnWebsocketInfo?.Invoke(ws_url, ws_token);

      var iceServers = data.iceServers.ToArray();
      OnIceServers?.Invoke(ref iceServers);
    }
  }
}



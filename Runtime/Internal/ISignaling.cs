using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dolby.Millicast
{

  internal interface ISignaling
  {
    enum Event
    {
      /// <summary>
      /// Sent events
      /// </summary>
      PUBLISH,
      UNPUBLISH,
      SUBSCRIBE,
      PROJECT,
      UNPROJECT,
      SELECT,

      /// <summary>
      /// Response events
      /// </summary>
      RESPONSE, // In response to a one of the sent events

      /// <summary>
      /// Received events
      /// </summary>
      ACTIVE, // TODO: Document what this does
      INACTIVE, // TODO: Document what this does
      VIEWER_COUNT, // When a new Viewer connects, the count is updated

      /// <summary>
      /// Viewer specific received events
      /// TODO: Document what they do.
      /// </summary>
      STOPPED,
      VAD,
      LAYERS,
    };

    /// <summary>
    /// Connect to the signaling service
    /// </summary>
    public Task Connect();

    /// <summary>
    /// Return whether the signaling client is connected.
    /// </summary>
    /// <returns></returns>
    public bool IsConnected();

    /// <summary>
    /// Disconnect to the signaling service
    /// </summary>
    public Task Disconnect();

    /// <summary>
    /// Called to dispatch receiving messages on a separate queue.
    /// </summary>
    public void Dispatch();

    /// <summary>
    /// Send an event to the other remote peer.
    /// </summary>
    /// <param name="e"> The Event name </param>
    /// <param name="data"> Additional data </param>
    /// <returns> An awaitable task</returns>
    public Task Send(Event e, Dictionary<string, object> data);

    /// <summary>
    /// An overload of Send which does not require data.
    /// </summary>
    /// <param name="e"> The event name </param>
    /// <returns> An awaitable task</returns>
    public Task Send(Event e);

    delegate void DelegateOnEvent(Event e, ServiceResponseData data);
    event DelegateOnEvent OnEvent;

    public delegate void DelegateOnOpen();
    public event DelegateOnOpen OnOpen;

    public delegate void DelegateOnClose(String reason);
    public event DelegateOnClose OnClose;

    public delegate void DelegateOnError(String message);
    public event DelegateOnError OnError;
  }
}



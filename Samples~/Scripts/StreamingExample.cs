using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Dolby.Millicast;

public class StreamingExample : MonoBehaviour
{
  private McPublisher _publisher;
  private McSubscriber _subscriber;

  [SerializeField] private Camera cam;
  [SerializeField] private AudioSource audioSource;

  [SerializeField] private McCredentials _credentials;

  [SerializeField] private string streamName;



  [SerializeField] private RawImage subscribeImage;
  [SerializeField] private RawImage sourceImage;

  // This is the receiving audio source.
  [SerializeField] private AudioSource subscribeAudioSource;

  [SerializeField] private Button publishButton;
  [SerializeField] private Button subscribeButton;

  [SerializeField] private Transform rotateObject;

  /// <summary>
  /// <c>Millicast.Initialize</c> must be called here.
  /// </summary>
  void Awake()
  {
    // Setting up buttons
    publishButton.onClick.AddListener(Publish);
    subscribeButton.onClick.AddListener(Subscribe);
    publishButton.interactable = true;
    subscribeButton.interactable = false;
  }

  void Start()
  {

    if (cam == null || audioSource == null)
    {
      Debug.Log("Must create Camera and AudioSource");
      return;
    }

    if (audioSource.clip != null)
    {
      audioSource.loop = true;
    }

    _publisher = gameObject.AddComponent<McPublisher>();
    _publisher.credentials = new Credentials(_credentials, false);
    _publisher.streamName = streamName;


    var codecs = Capabilities.GetAvailableVideoCodecs();
    if (Array.BinarySearch(codecs, 0, codecs.Length, VideoCodec.H264) >= 0)
      _publisher.options.videoCodec = VideoCodec.H264;

    _publisher.options.dtx = true;
    _publisher.options.stereo = true;

    // Not necessary to implement those callbacks.
    _publisher.OnPublishing += (publisher) =>
    {
      Debug.Log($"{publisher} is publishing!");

      // Subscriber setup.
      if (_subscriber == null)
      {
        _subscriber = gameObject.AddComponent<McSubscriber>();
        _subscriber.credentials = new Credentials(_credentials, true);
        _subscriber.streamName = streamName;
        _subscriber.AddVideoRenderTarget(subscribeImage);
        _subscriber.AddRenderAudioSource(subscribeAudioSource);
      }

      // Setup the buttons for unpublishing and subcribing
      publishButton.onClick.RemoveAllListeners();
      publishButton.onClick.AddListener(UnPublish);
      publishButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop";
      publishButton.interactable = true;
      subscribeButton.interactable = !_subscriber.isSubscribing;
    };

    _publisher.OnViewerCount += (publisher, count) =>
    {
      Debug.Log($"{publisher} viewer count is currently: {count}");
    };
  }

  void Subscribe()
  {
    subscribeButton.interactable = false;
    _subscriber.Subscribe();
  }

  void Publish()
  {
    if (!audioSource.isPlaying)
    {
      audioSource.Play();
    }

    _publisher.SetAudioSource(audioSource);
    _publisher.SetVideoSource(cam);
    _publisher.AddVideoRenderTarget(sourceImage);

    publishButton.interactable = false;
    _publisher.Publish();
  }

  void UnPublish()
  {
    publishButton.onClick.RemoveAllListeners();
    publishButton.onClick.AddListener(Publish);
    publishButton.GetComponentInChildren<TextMeshProUGUI>().text = "Publish";
    _publisher.UnPublish();
  }

  // Update is called once per frame
  void Update()
  {
    if (rotateObject != null)
    {
      float t = Time.deltaTime;
      rotateObject.Rotate(100 * t, 200 * t, 300 * t);
    }
  }

  /// <summary>
  /// <c>Millicast.Destroy</c> must be called here.
  /// </summary>
  void OnDestroy()
  {
  }
}



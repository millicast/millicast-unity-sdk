using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dolby.Millicast;

public class WebCamPublisherExample : MonoBehaviour {
  private McPublisher publisher;
  private WebCamTexture webCamTexture;
  private RenderTexture renderTexture;
  private WebCamDevice[] devices;

  [SerializeField]
  private McCredentials credentials;

  [SerializeField]
  private string streamName;

  void Start() {
    publisher = gameObject.AddComponent<McPublisher>();
    devices = WebCamTexture.devices;
    if (devices.Length == 0) {
      throw new System.Exception("No WebCam Devices found!");
    }

    Resolution resolution = new Resolution { width = 1920, height = 1080 };
    if (devices[0].availableResolutions != null && devices[0].availableResolutions.Length != 0) {
      resolution = devices[0].availableResolutions[0];
    }

    webCamTexture = new WebCamTexture(devices[0].name, resolution.width, resolution.height);
    renderTexture = new RenderTexture(resolution.width, resolution.height, 16, RenderTextureFormat.BGRA32);
    webCamTexture.Play();

    publisher.credentials = new Credentials(credentials, false);
    publisher.streamName = streamName;

    publisher.SetVideoSource(renderTexture);
    publisher.Publish();

  }

  // Update is called once per frame
  void Update() {
    Graphics.Blit(webCamTexture, renderTexture);
  }
}

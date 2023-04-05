using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Dolby.Millicast;
using System.Collections.Generic;
using System.Linq;

public class VideoConfigExample : MonoBehaviour
{

    private McPublisher _publisher;
    private McSubscriber _subscriber;

    [SerializeField] private Camera cam;
    [SerializeField] private AudioSource audioSource;
    [Header("Credential Settings")]
    [Tooltip("Assign Credentials Scriptable Object reference here.")]
    [SerializeField] private McCredentials _credentials;

    [SerializeField] private string streamName;
    [Header("UI References")]
    [SerializeField] private RawImage subscribeImage;
    [SerializeField] private RawImage sourceImage;

    // This is the receiving audio source.
    [SerializeField] private AudioSource subscribeAudioSource;

    [SerializeField] private Button publishButton;
    [SerializeField] private Button subscribeButton;

    [SerializeField] private Transform rotateObject;
    [SerializeField] private GameObject qualitySettingsUI, simulcastSettingsUI;
    [SerializeField] private Toggle simulcastToggle;


    private readonly VideoConfig _videoConfig = new VideoConfig();
    private StreamSize _streamSize = null;
    private ResolutionData resolutionData;
    private VideoQualitySettings qualitySettings;
    private bool simulcast;

    // UI Settings for video configuration
    [SerializeField] private TMP_Dropdown maxBitrateSelector;
    [SerializeField] private TMP_Dropdown minBitrateSelector;
    [SerializeField] private TMP_Dropdown maxFramerateSelector;
    [SerializeField] private TMP_Dropdown resolutionDownScalingSelector;
    [SerializeField] private TMP_Dropdown publishResolutionSelector;

    [SerializeField] private TMP_Dropdown videoCodecSelector;
    [SerializeField] private Button updateSettingsButton;

    void Awake()
    {
        resolutionData = new ResolutionData();
        qualitySettings = new VideoQualitySettings();

        // Setting up dropdowns and buttons
        publishButton.onClick.AddListener(Publish);
        subscribeButton.onClick.AddListener(Subscribe);
        updateSettingsButton.onClick.AddListener(CommitVideoConfigChange);

        publishButton.interactable = true;
        subscribeButton.interactable = false;
        simulcast = simulcastToggle.isOn;
        simulcastSettingsUI.GetComponent<SimulcastUI>().onUpdateSimulcastData += OnUpdateSimulcastValues;

        maxBitrateSelector.options = qualitySettings.bandwidthOptions
            .Select(pair => new TMP_Dropdown.OptionData { text = pair.Key })
            .ToList();
        maxBitrateSelector.onValueChanged.AddListener(ChangeMaxBitrate);

        minBitrateSelector.options = qualitySettings.bandwidthOptions
            .Select(pair => new TMP_Dropdown.OptionData { text = pair.Key })
            .ToList();
        minBitrateSelector.onValueChanged.AddListener(ChangeMinBitrate);

        resolutionDownScalingSelector.options = qualitySettings.scaleResolutionDownOptions
            .Select(pair => new TMP_Dropdown.OptionData { text = pair.Key })
            .ToList();
        resolutionDownScalingSelector.onValueChanged.AddListener(ChangeResolutionDownScaling);

        maxFramerateSelector.options = qualitySettings.framerateOptions
            .Select(pair => new TMP_Dropdown.OptionData { text = pair.Key })
            .ToList();
        maxFramerateSelector.onValueChanged.AddListener(ChangeMaxFramerate);

        publishResolutionSelector.options = resolutionData.GetResolutionOptions()
            .Select(label => new TMP_Dropdown.OptionData { text = label })
            .ToList();
        publishResolutionSelector.onValueChanged.AddListener(ChangePublishingResolution);

        videoCodecSelector.options = qualitySettings.videoCodecOptions
            .Select(pair => new TMP_Dropdown.OptionData { text = pair.Key })
            .ToList();
        videoCodecSelector.onValueChanged.AddListener(ChangeVideoCodec);
    }

    void Start()
    {
        if (cam == null && audioSource == null)
        {
            throw new Exception("Must create Camera or AudioSource");
        }

        if (audioSource.clip != null)
        {
            audioSource.loop = true;
        }

        _publisher = gameObject.AddComponent<McPublisher>();
        _publisher.credentials = new Credentials(_credentials, false);
        _publisher.streamName = streamName;

        _publisher.options.dtx = true;
        _publisher.options.stereo = true;
        UpdateDefaultValues();
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
    /// <summary>
    /// Extract videoconfiguration data from scriptable object and update the UI with default values
    /// </summary>
    private void UpdateDefaultValues()
    {
        VideoConfiguration videoConfigData = _publisher.videoConfigData;
        if (videoConfigData != null)
        {
            publishResolutionSelector.value = GetDefaultResolutionIndex(videoConfigData.pResolution);
            videoCodecSelector.value = GetDefaultVideoCodecIndex(videoConfigData.pCodecType);
            maxBitrateSelector.value = GetDefaultBitRateIndex(maxBitrateSelector, qualitySettings.pMaxBitrate);
            minBitrateSelector.value = GetDefaultBitRateIndex(minBitrateSelector, qualitySettings.pMinBitrate);
            maxFramerateSelector.value = GetDefaultFrameRateIndex(qualitySettings.pFramerateOption);
            resolutionDownScalingSelector.value = GetDefaultScaleDownIndex(qualitySettings.pScaleDownOption);
        }
    }

    #region UI Event Listeners
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
        CommitVideoConfigChange();
        _publisher.SetAudioSource(audioSource);
        _publisher.SetVideoSource(cam, _streamSize);
        _publisher.AddVideoRenderTarget(sourceImage);

        publishButton.interactable = false;
        videoCodecSelector.interactable = false;
        simulcastToggle.interactable = false;
        _publisher.Publish();
    }

    void UnPublish()
    {
        publishButton.onClick.RemoveAllListeners();
        publishButton.onClick.AddListener(Publish);
        publishButton.GetComponentInChildren<TextMeshProUGUI>().text = "Publish";
        _publisher.UnPublish();
        videoCodecSelector.interactable = true;
        subscribeButton.interactable = false;
        simulcastToggle.interactable = true;
    }
    void CommitVideoConfigChange()
    {
        _publisher.SetVideoConfig(_videoConfig, simulcast);
    }



    void ChangeMaxBitrate(int index)
    {
        _videoConfig.maxBitrate = (uint)qualitySettings.bandwidthOptions.Values.ElementAt(index);
    }

    void ChangeMinBitrate(int index)
    {
        _videoConfig.minBitrate = (uint)qualitySettings.bandwidthOptions.Values.ElementAt(index);

    }

    void ChangeMaxFramerate(int index)
    {
        _videoConfig.maxFramerate = (uint)qualitySettings.framerateOptions.Values.ElementAt(index);
    }


    void ChangeResolutionDownScaling(int index)
    {
        _videoConfig.resolutionDownScaling = (double)qualitySettings.scaleResolutionDownOptions.Values.ElementAt(index);
    }

    void ChangeVideoCodec(int index)
    {
        var codec = qualitySettings.videoCodecOptions.Values.ElementAt(index);
        _publisher.options.videoCodec = codec;

        var options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData { text = resolutionData.GetResolutionLabel(ResolutionData.SupportedResolutions.RES_720P) });

        Capabilities.SupportedResolutions maxRes = Capabilities.GetMaximumSupportedResolution(codec);

        if (maxRes > Capabilities.SupportedResolutions.RES_1080P)
        {
            options.Add(new TMP_Dropdown.OptionData { text = resolutionData.GetResolutionLabel(ResolutionData.SupportedResolutions.RES_1080P) });
        }

        if (maxRes > Capabilities.SupportedResolutions.RES_1440P)
        {
            options.Add(new TMP_Dropdown.OptionData { text = resolutionData.GetResolutionLabel(ResolutionData.SupportedResolutions.RES_1440P) });
        }

        if (maxRes > Capabilities.SupportedResolutions.RES_2K)
        {
            options.Add(new TMP_Dropdown.OptionData { text = resolutionData.GetResolutionLabel(ResolutionData.SupportedResolutions.RES_2K) });
        }

        if (maxRes > Capabilities.SupportedResolutions.RES_4K)
        {
            options.Add(new TMP_Dropdown.OptionData { text = resolutionData.GetResolutionLabel(ResolutionData.SupportedResolutions.RES_4K) });
        }
        publishResolutionSelector.options = options;
         if(codec == VideoCodec.VP8)
        {
            simulcastToggle.interactable = true;
        }
        else
        {
            simulcastToggle.isOn = false;
            simulcastToggle.interactable = false;
        }
    }

    void ChangePublishingResolution(int index)
    {
        var key = publishResolutionSelector.options.ElementAt(index).text;
        _streamSize = resolutionData.GetStreamSize(key);
        _publisher.SetVideoSource(cam, _streamSize);
    }
    public void OnToggleSimulcast(Toggle toggleBtn)
    {
        simulcast = toggleBtn.isOn;
        CommitVideoConfigChange();
        qualitySettingsUI.SetActive(!toggleBtn.isOn);
        simulcastSettingsUI.SetActive(toggleBtn.isOn);
    }

    public void OnUpdateSimulcastValues(SimulcastLayers layersInfo)
    {
        _publisher.SetSimulcastData(layersInfo);
    }
    #endregion

    #region helper methods
    private int GetDefaultResolutionIndex(ResolutionData.SupportedResolutions defaultRes)
    {
        for (int i = 0; i < publishResolutionSelector.options.Count; i++)
        {
            ResolutionData.SupportedResolutions type = resolutionData.GetResolutionType(publishResolutionSelector.options[i].text);
            if (defaultRes == type)
                return i;
        }
        return 0;
    }

    private int GetDefaultVideoCodecIndex(VideoCodec codectype)
    {
        for (int i = 0; i < videoCodecSelector.options.Count; i++)
        {
            VideoCodec type = qualitySettings.videoCodecOptions[videoCodecSelector.options[i].text];
            if (codectype == type)
                return i;
        }
        return 0;
    }
    private int GetDefaultBitRateIndex(TMP_Dropdown dropdown, VideoQualitySettings.BandwidthOption bandwidth)
    {
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            VideoQualitySettings.BandwidthOption option = qualitySettings.bandwidthOptions[dropdown.options[i].text];
            if (bandwidth == option)
                return i;
        }
        return 0;
    }
    private int GetDefaultFrameRateIndex(VideoQualitySettings.FramerateOption framerate)
    {
        for (int i = 0; i < maxFramerateSelector.options.Count; i++)
        {
            VideoQualitySettings.FramerateOption option = qualitySettings.framerateOptions[maxFramerateSelector.options[i].text];
            if (framerate == option)
                return i;
        }
        return 0;
    }
    private int GetDefaultScaleDownIndex(VideoQualitySettings.ScaleDownOption scaledown)
    {
        for (int i = 0; i < resolutionDownScalingSelector.options.Count; i++)
        {
            VideoQualitySettings.ScaleDownOption option = qualitySettings.scaleResolutionDownOptions[resolutionDownScalingSelector.options[i].text];
            if (option == scaledown)
                return i;
        }
        return 0;
    }
    #endregion


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
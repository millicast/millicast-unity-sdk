using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
namespace Dolby.Millicast
{
    /// <summary>
    /// A Scriptable Object that can be used to configure video configuration details which will be used for publishing. 
    /// More information on where to get those details from can be found in 
    /// https://docs.dolby.io/streaming-apis/docs/getting-started
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "Video Configuration", menuName = "Millicast/Video Configuration")]
    public partial class VideoConfiguration : ScriptableObject
    {
        //All serializable propertis are drawn from the VideoConfigurationEditor script
        [SerializeField] private VideoCodec codecType;
        [SerializeField] private ResolutionData.SupportedResolutions resolution;
        public bool simulcast;
        [SerializeField] private ResolutionData resolutionData = new ResolutionData();
        [SerializeField] [DrawIf("simulcast", false)] private VideoQualitySettings qualitySettings = new VideoQualitySettings();
        [SerializeField] [DrawIf("simulcast", true)] private SimulcastLayers simulcastLayers = new SimulcastLayers();
        public ResolutionData.SupportedResolutions pResolution { get { return resolution; } }
        public VideoCodec pCodecType { get { return codecType; } }

        public VideoQualitySettings pQualitySettings
        {
            get
            {
                if (qualitySettings == null)
                    qualitySettings = new VideoQualitySettings();
                return qualitySettings;
            }
        }
        public SimulcastLayers pSimulcastSettings
        {
            get
            {
                if (simulcastLayers == null)
                    simulcastLayers = new SimulcastLayers();
                return simulcastLayers;
            }
        }
        public ResolutionData pScreenResolutionData
        {
            get
            {
                if (resolutionData == null)
                    resolutionData = new ResolutionData();
                return resolutionData;
            }
        }
        public StreamSize pStreamSize
        {
            get
            {
                if (resolutionData == null)
                    resolutionData = new ResolutionData();
                return resolutionData.GetStreamSize(pResolution);
            }
        }

        public void ValidateResolution()
        {
            int maxRes = (int)Capabilities.GetMaximumSupportedResolution(codecType);
            if (maxRes < (int)resolution)
            {
                Debug.LogWarning($"{resolution.ToString()} not supported for {codecType}");
                resolution = (ResolutionData.SupportedResolutions)maxRes;
                Debug.Log($"Switched to Max Supported resolution => {resolution.ToString()}");
            }
        }
    }

}
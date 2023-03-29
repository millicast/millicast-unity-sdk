
using System;
using UnityEngine;
using System.Collections.Generic;
namespace Dolby.Millicast
{
    /// <summary>
    /// This class that is used in VideoConfig scriptable object to configure publish video settings regarding bitrate, framerate
    /// and resolution scaling. Those settings can be modified in the scriptable object
    /// </summary>
    [System.Serializable]
    public class VideoQualitySettings
    {
        public enum BandwidthOption
        {
            [InspectorName("10000")] B_10K = 10000,
            [InspectorName("6000")] B_6K = 6000,
            [InspectorName("2500")] B_2dot5K = 2500,
            [InspectorName("1000")] B_1K = 1000,
            [InspectorName("500")] B_500 = 500,
            [InspectorName("125")] B_125 = 125
        }
        public enum ScaleDownOption
        {
            [InspectorName("Not scaling")] No_Scale = 1,
            [InspectorName("Down scale by 2.0")] SD_2 = 2,
            [InspectorName("Down scale by 4.0")] SD_4 = 4,
            [InspectorName("Down scale by 8.0")] SD_8 = 8,
            [InspectorName("Down scale by 16.0")] SD_16 = 16,
        }

        public enum FramerateOption
        {
            [InspectorName("Not Set")] Not_Set = 0,
            [InspectorName("60")] FR_60 = 60,
            [InspectorName("30")] FR_30 = 30,
            [InspectorName("20")] FR_20 = 20,
            [InspectorName("10")] FR_10 = 10,
            [InspectorName("5")] FR_5 = 8,

        }
        [HideInInspector] public Dictionary<string, BandwidthOption> bandwidthOptions;
        [HideInInspector] public Dictionary<string, FramerateOption> framerateOptions;
        [HideInInspector] public Dictionary<string, ScaleDownOption> scaleResolutionDownOptions;


        [HideInInspector]
        public Dictionary<string, VideoCodec> videoCodecOptions =
        new Dictionary<string, VideoCodec>
        {
                { "VP8", VideoCodec.VP8 },
                { "VP9", VideoCodec.VP9 },
                { "H264", VideoCodec.H264 },
                { "AV1", VideoCodec.AV1 }
        };
        [SerializeField] private BandwidthOption maxBitrate = BandwidthOption.B_2dot5K;
        [SerializeField] private BandwidthOption minBitrate = BandwidthOption.B_500;
        [SerializeField] private FramerateOption framerateOption = FramerateOption.FR_60;
        [SerializeField] private ScaleDownOption scaleDownOption = ScaleDownOption.No_Scale;


        public BandwidthOption pMaxBitrate { get { return maxBitrate; } }
        public BandwidthOption pMinBitrate { get { return minBitrate; } }
        public ScaleDownOption pScaleDownOption { get { return scaleDownOption; } }
        public FramerateOption pFramerateOption { get { return framerateOption; } }

        public VideoQualitySettings()
        {
            InitializeBandwidthData();
            InitializeScaleDownData();
            InitializeFramerateData();
        }

        private void InitializeBandwidthData()
        {
            bandwidthOptions = new Dictionary<string, BandwidthOption>();
            bandwidthOptions.Add("10000", BandwidthOption.B_10K);
            bandwidthOptions.Add("6000", BandwidthOption.B_6K);
            bandwidthOptions.Add("2500", BandwidthOption.B_2dot5K);
            bandwidthOptions.Add("1000", BandwidthOption.B_1K);
            bandwidthOptions.Add("500", BandwidthOption.B_500);
            bandwidthOptions.Add("125", BandwidthOption.B_125);
        }
        private void InitializeScaleDownData()
        {
            scaleResolutionDownOptions = new Dictionary<string, ScaleDownOption>();
            scaleResolutionDownOptions.Add("Not scaling", ScaleDownOption.No_Scale);
            scaleResolutionDownOptions.Add("Down scale by 2.0", ScaleDownOption.SD_2);
            scaleResolutionDownOptions.Add("Down scale by 4.0", ScaleDownOption.SD_4);
            scaleResolutionDownOptions.Add("Down scale by 8.0", ScaleDownOption.SD_8);
            scaleResolutionDownOptions.Add("Down scale by 16.0", ScaleDownOption.SD_16);
        }
        private void InitializeFramerateData()
        {
            framerateOptions = new Dictionary<string, FramerateOption>();
            framerateOptions.Add("Not set", FramerateOption.Not_Set);
            framerateOptions.Add("60", FramerateOption.FR_60);
            framerateOptions.Add("30", FramerateOption.FR_30);
            framerateOptions.Add("20", FramerateOption.FR_20);
            framerateOptions.Add("10", FramerateOption.FR_10);
            framerateOptions.Add("5", FramerateOption.FR_5);
        }
    }
    [System.Serializable]
    public class LayerData
    {
        public ulong maxBitrateKbps;
        //public VideoQualitySettings.ScaleDownOption resolutionScaleDown;

        public LayerData(ulong bitrate, VideoQualitySettings.ScaleDownOption option)
        {
            this.maxBitrateKbps = bitrate;
           // resolutionScaleDown = option;
        }
    }
    [System.Serializable]
    public class SimulcastLayers
    {
        public LayerData High = new LayerData(4000, VideoQualitySettings.ScaleDownOption.No_Scale);
        public LayerData Medium = new LayerData(900, VideoQualitySettings.ScaleDownOption.SD_2);
        public LayerData Low = new LayerData(400, VideoQualitySettings.ScaleDownOption.SD_4);
    }
}

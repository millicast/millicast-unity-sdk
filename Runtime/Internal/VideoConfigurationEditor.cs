#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Dolby.Millicast
{
    public partial class VideoConfiguration
    {
        //inner Editor class which can access the outer class private variables
        [CustomEditor(typeof(VideoConfiguration))]
        [CanEditMultipleObjects]
        public class VideoConfigurationEditor : Editor
        {
            private ResolutionData.SupportedResolutions resolution;
            private VideoCodec codecType;
            private bool simulcast;
            public VideoConfiguration videoConfig;
            private void Awake()
            {
                if (videoConfig == null)
                    videoConfig = target as VideoConfiguration;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                if (resolution == videoConfig.pResolution && codecType == videoConfig.pCodecType)
                    return;
                videoConfig.ValidateResolution();
                resolution = videoConfig.pResolution;
                codecType = videoConfig.pCodecType;
            }
        }
    }
}
#endif
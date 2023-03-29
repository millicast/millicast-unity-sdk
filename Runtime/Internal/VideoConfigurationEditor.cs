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
                Init();
            }
            public SerializedProperty
                _codecType,
                _resolution,
                _simulcast,
                _qualitySettings,
                _simulcastSettings;

            void Init()
            {
                _codecType = serializedObject.FindProperty(nameof(VideoConfiguration.codecType));
                _resolution = serializedObject.FindProperty(nameof(VideoConfiguration.resolution));
                _simulcast = serializedObject.FindProperty(nameof(VideoConfiguration.simulcast));
                _qualitySettings = serializedObject.FindProperty(nameof(VideoConfiguration.qualitySettings));
                _simulcastSettings = serializedObject.FindProperty(nameof(VideoConfiguration.simulcastLayers));
            }
            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(_codecType, true);
                EditorGUILayout.PropertyField(_resolution, true);
                if(videoConfig.codecType == VideoCodec.VP8)
                    EditorGUILayout.PropertyField(_simulcast, true);
                else
                    videoConfig.simulcast = false;
                if (videoConfig.simulcast)
                    EditorGUILayout.PropertyField(_simulcastSettings, true);
                else
                    EditorGUILayout.PropertyField(_qualitySettings, true);
                serializedObject.ApplyModifiedProperties();

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
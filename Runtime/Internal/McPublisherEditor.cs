#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Dolby.Millicast
{
    [CustomEditor(typeof(McPublisher))]
    [CanEditMultipleObjects]
    public class McPublisherEditor : Editor
    {
        public SerializedProperty
            videoSourceCamera,
            videoSourceRenderTexture,
            audioListenerAsSource,
            videoSourceType,
            videoConfigData,
            audioSource;
        private McPublisher myPublisher;

        void Init()
        {
            videoSourceCamera = serializedObject.FindProperty("_videoSourceCamera");
            videoSourceRenderTexture = serializedObject.FindProperty("_videoSourceRenderTexture");
            audioSource = serializedObject.FindProperty("_audioSource");
            audioListenerAsSource = serializedObject.FindProperty("_useAudioListenerAsSource");
            videoSourceType = serializedObject.FindProperty("videoSourceType");
            videoConfigData = serializedObject.FindProperty("_videoConfigData");
            myPublisher = target as McPublisher;
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            try
            {
                if(myPublisher.streamType == StreamType.Video || myPublisher.streamType == StreamType.Both)
                {
                    EditorGUILayout.PropertyField(videoConfigData, new GUIContent("Video Configuration Data"));
                    EditorGUILayout.PropertyField(videoSourceType, new GUIContent("Video Source Type"));
                    if (myPublisher.videoSourceType == VideoSourceType.Camera)
                    {
                        EditorGUILayout.PropertyField(videoSourceCamera, new GUIContent("Video Source Camera"));
                    }
                    if (myPublisher.videoSourceType == VideoSourceType.RenderTexture)
                    {
                        EditorGUILayout.PropertyField(videoSourceRenderTexture, new GUIContent("Video Source Rendertexture"));
                    }
                }
                if(myPublisher.streamType == StreamType.Audio || myPublisher.streamType == StreamType.Both)
                {
                    EditorGUILayout.PropertyField(audioListenerAsSource, new GUIContent("Use AudioListener As Source"));
                    if (!myPublisher._useAudioListenerAsSource)
                        EditorGUILayout.PropertyField(audioSource, new GUIContent("Audio Source"));
                } 
                
                serializedObject.ApplyModifiedProperties();
            }
            catch (System.Exception)
            {
                Init();
            }
           
        }
    }
}
#endif
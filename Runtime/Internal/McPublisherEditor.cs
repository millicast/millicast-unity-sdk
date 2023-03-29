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
            audioSource;
        private McPublisher myPublisher;

        void Init()
        {
            videoSourceCamera = serializedObject.FindProperty("_videoSourceCamera");
            videoSourceRenderTexture = serializedObject.FindProperty("_videoSourceRenderTexture");
            audioSource = serializedObject.FindProperty("_audioSource");
            audioListenerAsSource = serializedObject.FindProperty("_useAudioListenerAsSource");
            myPublisher = target as McPublisher;
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if(videoSourceCamera == null)
                Init();

            if (myPublisher.videoSourceType == VideoSourceType.Camera)
            {
                EditorGUILayout.PropertyField(videoSourceCamera, new GUIContent("Video Source Camera"));
            }
            if (myPublisher.videoSourceType == VideoSourceType.RenderTexture)
            {
                EditorGUILayout.PropertyField(videoSourceRenderTexture, new GUIContent("Video Source Rendertexture"));
            }
            if (!myPublisher._useAudioListenerAsSource)
                EditorGUILayout.PropertyField(audioSource, new GUIContent("Audio Source"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
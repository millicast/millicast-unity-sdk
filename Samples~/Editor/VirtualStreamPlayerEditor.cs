using UnityEngine;
using UnityEditor;
namespace Dolby.Millicast.Samples
{
#if UNITY_EDITOR
    [CustomEditor(typeof(VirtualStreamPlayer))]
    public class VirtualStreamPlayerEditor : Editor
    {

        private MeshResolutionSelector.AspectRatio aspectRatio;
        private float scaleFactor;
        public VirtualStreamPlayer streamPlayer;
        private MeshResolutionSelector.VideoMode videoMode;

        private void Awake()
        {
            if (streamPlayer == null)
                streamPlayer = target as VirtualStreamPlayer;
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (streamPlayer == null)
                streamPlayer = target as VirtualStreamPlayer;
            if (aspectRatio == streamPlayer.desiredAspectRatio && scaleFactor == streamPlayer.scaleFactor && videoMode == streamPlayer.videoMode)
                return;
            aspectRatio = streamPlayer.desiredAspectRatio;

            scaleFactor = streamPlayer.scaleFactor * 0.001f;

            videoMode = streamPlayer.videoMode;
            float width = 1920;
            float height = 1080;
            switch (aspectRatio)
            {
                case MeshResolutionSelector.AspectRatio.AR1://1024x576
                    width = 1024;
                    height = 576;
                    break;
                case MeshResolutionSelector.AspectRatio.AR2://800x600
                    width = 800;
                    height = 600;
                    break;
                case MeshResolutionSelector.AspectRatio.AR3://1080 x 1080
                    width = 1080;
                    height = 1080;
                    break;
            }

            if (videoMode == MeshResolutionSelector.VideoMode.Portrait)
                streamPlayer.SetResolution(height, width, scaleFactor);
            else
                streamPlayer.SetResolution(width, height, scaleFactor);
        }
    }
#endif
}

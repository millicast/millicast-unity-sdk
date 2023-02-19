using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[RequireComponent(typeof(MeshRenderer))]
public class MeshResolutionSelector : MonoBehaviour
{
    public enum AspectRatio
    {
        [InspectorName("16:9")] AR1,
        [InspectorName("4:3")] AR2,
        [InspectorName("1:1")] AR3,
    }
    public enum VideoMode
    {
        Landscape,
        Portrait
    }

    public AspectRatio desiredAspectratio;
    public float scaleFactor = 1.0f;

    public VideoMode videoMode;
    public bool flipHorizontal;
    public bool flipVertical;
    private Material renderMaterial;
#if UNITY_EDITOR
    private bool flipHor, flipVert;
#endif
    private void Start() {
#if UNITY_EDITOR

        flipHor = flipHorizontal;
        flipVert = flipVertical;
#endif
    }

    private void Update()
    {
    #if UNITY_EDITOR
        if(flipHor != flipHorizontal || flipVert != flipVertical)
           FlipTexture(flipHorizontal, flipVertical);
    #endif
    }
    /// <summary>
    /// Used to Flip the streaming texture horiontally and/or vertically 
    /// </summary>
    public void FlipTexture(bool horizontal, bool vertical)
    {
#if UNITY_EDITOR
        flipHor = horizontal;
        flipVert = vertical;
#endif
        GetComponent<MeshRenderer>().materials[0].mainTextureScale = new Vector2(horizontal?-1 : 1,vertical? -1 : 1);
    }

    public void SetResolution(float width, float height, float scale)
    {
        transform.localScale = new Vector3(width * scale, 1, height * scale);
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(MeshResolutionSelector))]
public class MeshResolutionSelectorEditor : Editor
{
    private MeshResolutionSelector.AspectRatio aspectRatio;
    private float scaleFactor;
    public MeshResolutionSelector resolutionSelector;
    private MeshResolutionSelector.VideoMode videoMode;
    private bool fliphor, flipvert;
    private void Awake()
    {
        if (resolutionSelector == null)
            resolutionSelector = target as MeshResolutionSelector;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (aspectRatio == resolutionSelector.desiredAspectratio && scaleFactor == resolutionSelector.scaleFactor && videoMode == resolutionSelector.videoMode)
            return;
        aspectRatio = resolutionSelector.desiredAspectratio;

        scaleFactor = resolutionSelector.scaleFactor * 0.001f;

        videoMode = resolutionSelector.videoMode;
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

        if(videoMode == MeshResolutionSelector.VideoMode.Portrait)
            resolutionSelector.SetResolution(height, width, scaleFactor);
        else
            resolutionSelector.SetResolution(width, height, scaleFactor);
    }
}
#endif
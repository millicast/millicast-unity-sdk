
using System;
using UnityEditor;
using UnityEngine;
using Dolby.Millicast;

[RequireComponent(typeof(AudioSource))]
[ExecuteInEditMode]
public class CustomAudioSource : MonoBehaviour
{

    public enum ConfigType
    {
        Manual,
        [InspectorName("Use Audio Configuration")] Use_Audio_Configuration
    }

    public ConfigType audioConfigurationType;
    private AudioSource audioSource;

    [SerializeField]
    [DrawIf("audioConfigurationType", ConfigType.Use_Audio_Configuration)]
    public Dolby.Millicast.AudioConfiguration audioConfiguration;
       
    [SerializeField]
    [DrawIf("audioConfigurationType", ConfigType.Use_Audio_Configuration)][Tooltip("If enabled, changes done to Audio Source will be updated in the Audio Configuration")]
    public bool allowChangesToAudioConfig;
    

    [SerializeField]
    [DrawIf("audioConfigurationType", ConfigType.Manual)][Tooltip("Min Distance")]
    [Range(1f, 500f)]
    private float minDistance = 1f;

    public float MinDistance
    {
        get { return minDistance; }
        set
        {
            minDistance = value;
            audioSource.minDistance = minDistance;
        }
    }

    [SerializeField]
    [DrawIf("audioConfigurationType", ConfigType.Manual)][Tooltip("Max Distance")]
    [Range(1f, 500f)]
    private float maxDistance = 50f;

    public float MaxDistance
    {
        get { return maxDistance; }
        set
        {
            maxDistance = value;
            audioSource.maxDistance = maxDistance;
        }
    }

    [SerializeField]
    [Tooltip("Spread")]
    [DrawIf("audioConfigurationType", ConfigType.Manual)][Range(0f, 360f)]
    private float spread = 0f;
    private Texture2D PlayIcon, DefaultIcon;
    private float minSpectrumVal = 0.001f;
    private int spectrumMultiplier = 100;
    private bool isPlaying = false;
    private float spectrumVal;

    public float Spread
    {
        get { return spread; }
        set
        {
            spread = value;
            audioSource.spread = spread;
        }
    }

    [SerializeField]
    [DrawIf("audioConfigurationType", ConfigType.Manual)][Tooltip("Volume")]
    [Range(0f, 1f)]
    private float volume = 1f;

    public float Volume
    {
        get { return volume; }
        set
        {
            volume = value;
            audioSource.volume = volume;
        }
    }
    #if UNITY_EDITOR
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.spread = spread;
        audioSource.volume = volume;
        PlayIcon = Resources.Load<Texture2D>("Icons/speaker-playing");
        DefaultIcon = Resources.Load<Texture2D>("Icons/speaker-default");
    }
    
    private void DrawGizmos()
    {
        // Draw max and min distance spheres
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, maxDistance);

        Gizmos.color = new Color(0f, 0f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, minDistance);

    }

    public void LoadValuesFromAudioSource()
    {
        minDistance = audioSource.minDistance;
        maxDistance = audioSource.maxDistance;
        volume = audioSource.volume;
        spread = audioSource.spread;
    }

    public void UpdateAudioSourceValues()
    {
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.volume = volume;
        audioSource.spread = spread;
    }

    public void UpdateAudioSourceValuesFromConfig()
    {
        if(audioConfiguration != null)
        {
            audioConfiguration.LoadData(audioSource);
        }
    }

    public void OverrideConfigData()
    {
        if(audioConfiguration != null)
            audioConfiguration.OverrideData(audioSource);
    }


    private void updateSpectrumData()
    {
        float[] sample = new float[128];
        audioSource.GetSpectrumData(sample, 0, FFTWindow.Blackman);
        if(sample != null)
            spectrumVal = sample[0]* spectrumMultiplier;
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            DrawGizmos();
        }
        else
        {
            updateSpectrumData();
            if(spectrumVal > minSpectrumVal && !isPlaying)
            {
                isPlaying = true;
                SetIcon(true);
            }
            if(isPlaying && spectrumVal < minSpectrumVal)
            {
                isPlaying = false;
                SetIcon(false);            
            }
                
        }
    }

     private void SetIcon(bool isplaying)
     {
        Texture2D icon = isplaying ? PlayIcon : DefaultIcon;
        EditorGUIUtility.SetIconForObject(gameObject, (Texture2D) icon);
     }
     #endif
}
#if UNITY_EDITOR
[CustomEditor(typeof(CustomAudioSource))]
 public class CustomAudioSourceEditor : Editor 
 {
    public CustomAudioSource customAudioSource;
    private void Awake() 
    {
        if (customAudioSource == null)
            customAudioSource = target as CustomAudioSource;
    }
     public override void OnInspectorGUI() 
     {
        if(customAudioSource.audioConfigurationType == CustomAudioSource.ConfigType.Use_Audio_Configuration)
        {
            if(customAudioSource.allowChangesToAudioConfig)
            {
                customAudioSource.OverrideConfigData();
                GUILayout.Label("Changes done to Audio Source will be updated in the Audio Configuration");
            }    
             else
            {
                GUILayout.Label("Changes done to Audio Source will not be updated in the Audio Configuration");
            }
        }  
        else
            customAudioSource.LoadValuesFromAudioSource();
        base.OnInspectorGUI();
        
        if(customAudioSource.audioConfigurationType == CustomAudioSource.ConfigType.Manual)
            customAudioSource.UpdateAudioSourceValues();
        else if(customAudioSource.audioConfigurationType == CustomAudioSource.ConfigType.Use_Audio_Configuration)
            customAudioSource.UpdateAudioSourceValuesFromConfig();    
     }
 }
 #endif

using System;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[ExecuteInEditMode]
public class CustomAudioSource : MonoBehaviour
{
    private AudioSource audioSource;

    [SerializeField]
    [Tooltip("Min Distance")]
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
    [Tooltip("Max Distance")]
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
    [Range(0f, 360f)]
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
    [Tooltip("Volume")]
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
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.spread = spread;
            audioSource.volume = volume;

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
}
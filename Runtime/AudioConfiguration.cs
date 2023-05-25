using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
namespace Dolby.Millicast
{

    /// <summary>
    /// A Scriptable Object that can be used to configure Audio configuration details which will be used for Subscribing. 
    /// More information on where to get those details from can be found in 
    /// https://docs.dolby.io/streaming-apis/docs/getting-started
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "Audio Configuration", menuName = "Millicast/Audio Configuration")]
    public class AudioConfiguration : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Min Distance")]
        [Range(1f, 500f)]
        private float minDistance = 1f;


        [SerializeField]
        [Tooltip("Max Distance")]
        [Range(1f, 500f)]
        private float maxDistance = 50f;

        [SerializeField]
        [Tooltip("Spread")]
        [Range(0f, 360f)]
        private float spread = 0f;

        [SerializeField]
        [Tooltip("Volume")]
        [Range(0f, 1f)]
        private float volume = 1f;

        public void LoadData(AudioSource targetAudioSource)
        {
            targetAudioSource.volume = volume;
            targetAudioSource.spread = spread;
            targetAudioSource.minDistance = minDistance;
            targetAudioSource.maxDistance = maxDistance;
        }
    }

    [System.Serializable]
    public class StereoAudio
    {
        public AudioSource _left;
        public AudioSource _right;
        public AudioSource[] getSpeakers()
        {
           AudioSource[] speakers =  {_left, _right};
           return speakers;
        }
    }
    [System.Serializable]
    public class FiveOneAudio
    {
        public AudioSource _left;
        public AudioSource _surroundLeft;
        public AudioSource _right;
        public AudioSource _lfe;
        public AudioSource _surroundRight;
        public AudioSource _center;

        public AudioSource[] getSpeakers()
        {
           AudioSource[] speakers =  {_left, _surroundLeft, _right, _center, _lfe, _surroundRight};
           return speakers;
        }
    }
    
    [System.Serializable]
    public class CustomAudio
    {
        public List<AudioSource> speakers;
    }
}
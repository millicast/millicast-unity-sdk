using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
namespace Dolby.Millicast
{

    public enum AudioOutputType
    {
        Auto,
        AudioSource,
        VirtualSpeakers
    }

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

         private bool isUpdated(AudioSource targetAudioSource)
        {
            if(targetAudioSource == null)
                return false;
            return  minDistance != targetAudioSource.minDistance ||
                    maxDistance != targetAudioSource.maxDistance ||
                    volume != targetAudioSource.volume ||
                    spread != targetAudioSource.spread;
        }

        public void LoadData(AudioSource targetAudioSource)
        {
            if(!isUpdated(targetAudioSource))
                return;
            targetAudioSource.volume = volume;
            targetAudioSource.spread = spread;
            targetAudioSource.minDistance = minDistance;
            targetAudioSource.maxDistance = maxDistance;
        }

        public void OverrideData(AudioSource sourceAudio)
        {
            if(!isUpdated(sourceAudio))
                return;
            volume = sourceAudio.volume;
            spread = sourceAudio.spread;
            minDistance = sourceAudio.minDistance;
            maxDistance = sourceAudio.maxDistance;
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
        public AudioSource _right;
        public AudioSource _center;
        public AudioSource _lfe;
        public AudioSource _surroundLeft;
        public AudioSource _surroundRight;

        public AudioSource[] getSpeakers()
        {
           AudioSource[] speakers =  {_left, _surroundLeft, _right, _center, _lfe, _surroundRight};
           return speakers;
        }
    }
}
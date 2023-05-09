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
    public partial class AudioConfiguration : ScriptableObject
    {
        public AdvancedAudioConfig AdvancedAudioConfiguration;
    }
    [System.Serializable]
    public class AdvancedAudioConfig
    {
        public VirtualSpeakerMode audioChannelType;
        public bool addSpeakers;
        [DrawIf("audioChannelType", VirtualSpeakerMode.Mono)] public AudioSource speaker;
        [DrawIf("audioChannelType", VirtualSpeakerMode.Stereo)] public StereoAudio StereoSpeakers;
        [DrawIf("audioChannelType", VirtualSpeakerMode.Mode5point1)] public FiveOneAudio FiveOneAudioSpeakers;
        [DrawIf("audioChannelType", VirtualSpeakerMode.Mode7point1)] public CustomAudio CustomAudioSpeakers;

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
    public class SevenOneAudio
    {
        public AudioSource _frontLeft;
        public AudioSource _frontRight;
        public AudioSource _center;
        public AudioSource _left;
        public AudioSource _right;
        public AudioSource _rearLeft;
        public AudioSource _rearRight;
        public AudioSource _lfe;
        public AudioSource[] getSpeakers()
        {
           AudioSource[] speakers =  {_frontLeft, _frontRight, _center, _left, _right, _rearLeft, _rearRight, _lfe};
           return speakers;
        }

    }
    [System.Serializable]
    public class CustomAudio
    {
        public List<AudioSource> speakers;
    }
}
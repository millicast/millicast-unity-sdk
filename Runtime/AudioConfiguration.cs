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
        public AudioSpeakerMode audioChannelType;
        public bool addSpeakers;
        [DrawIf("audioChannelType", AudioSpeakerMode.Mono)] public AudioSource speaker;
        [DrawIf("audioChannelType", AudioSpeakerMode.Stereo)] public StereoAudio StereoSpeakers;
        [DrawIf("audioChannelType", AudioSpeakerMode.Mode5point1)] public FiveOneAudio FiveOneAudioSpeakers;
        [DrawIf("audioChannelType", AudioSpeakerMode.Mode7point1)] public CustomAudio CustomAudioSpeakers;

    }
    [System.Serializable]
    public class StereoAudio
    {
        public AudioSource _left;
        public AudioSource _right;
    }
    [System.Serializable]
    public class FiveOneAudio
    {
        public AudioSource _left;
        public AudioSource _surroundLeft;
        public AudioSource _right;
        public AudioSource lfe;
        public AudioSource _surroundRight;
        public AudioSource _center;
    }
    [System.Serializable]
    public class CustomAudio
    {
        public List<AudioSource> speakers;
    }
}
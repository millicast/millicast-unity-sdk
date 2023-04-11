using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
namespace Dolby.Millicast
{
    public enum ChannelType
    {
        [InspectorName("Mono")]  MONO,
        [InspectorName("Stereo")]  STEREO,
        [InspectorName("5.1")]  FIVE_1,
        [InspectorName("Custom")]  CUSTOM
    }

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
        public ChannelType audioChannelType;
        public bool addSpeakers;
        [DrawIf("audioChannelType", ChannelType.MONO)] public AudioSource speaker;
        [DrawIf("audioChannelType", ChannelType.STEREO)] public StereoAudio StereoSpeakers;
        [DrawIf("audioChannelType", ChannelType.FIVE_1)] public FiveOneAudio FiveOneAudioSpeakers;
        [DrawIf("audioChannelType", ChannelType.CUSTOM)] public CustomAudio CustomAudioSpeakers;

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
        public AudioSource _right;
        public AudioSource _center;
        public AudioSource _surroundLeft;
        public AudioSource _surroundRight;
    }
    [System.Serializable]
    public class CustomAudio
    {
        public List<AudioSource> speakers;
    }
}
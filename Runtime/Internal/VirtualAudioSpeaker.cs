using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Dolby.Millicast
{
    public class VirtualAudioSpeaker : MonoBehaviour
    {
        public AudioSpeakerMode audioChannelType;
        [DrawIf("audioChannelType", AudioSpeakerMode.Mono)] public AudioSource speaker;
        [DrawIf("audioChannelType", AudioSpeakerMode.Stereo)] public StereoAudio StereoSpeakers;
        [DrawIf("audioChannelType", AudioSpeakerMode.Mode5point1)] public FiveOneAudio FiveOneAudioSpeakers;

        public AudioSource[] getAudioSpeakers()
        {
            switch(audioChannelType)
            {
                case AudioSpeakerMode.Mono :
                    return getMonoSpeakers();
                case AudioSpeakerMode.Stereo:
                    return getStereoSpeakers();
                case AudioSpeakerMode.Mode5point1:
                    return getFiveOneSpeakers();
                default:
                    return getStereoSpeakers();
            }
        }

        private AudioSource[] getMonoSpeakers()
        {
            AudioSource[] speakers = {speaker};
            return speakers;
        }

        private AudioSource[] getStereoSpeakers()
        {
            AudioSource[] speakers = {StereoSpeakers._left, StereoSpeakers._right};
            return speakers;
        }
            /*
                0-Left Surround
                1-Right Surround
                2-Left
                3-Right
                4-Center
                5 -LFE
            */
        private AudioSource[] getFiveOneSpeakers()
        {
            AudioSource[] speakers = {FiveOneAudioSpeakers._center, FiveOneAudioSpeakers._left, FiveOneAudioSpeakers._right, FiveOneAudioSpeakers._surroundLeft, FiveOneAudioSpeakers._surroundRight, FiveOneAudioSpeakers._lfe};
            return speakers;
        }
    }
}

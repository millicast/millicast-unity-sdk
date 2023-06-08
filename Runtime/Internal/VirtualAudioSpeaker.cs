using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Dolby.Millicast
{

    public enum VirtualSpeakerMode
    {
        Mono,
        Stereo,
        [InspectorName("5.1")]
        Mode5point1

    }



    public class VirtualAudioSpeaker : MonoBehaviour
    {

        public VirtualSpeakerMode audioChannelType;
        [DrawIf("audioChannelType", VirtualSpeakerMode.Mono)] public AudioSource speaker;
        [DrawIf("audioChannelType", VirtualSpeakerMode.Stereo)] public StereoAudio StereoSpeakers;
        [DrawIf("audioChannelType", VirtualSpeakerMode.Mode5point1)] public FiveOneAudio FiveOneAudioSpeakers;

        private int[] channelMap;
        public AudioSource[] getAudioSpeakers(int channelCount)
        {
            switch(audioChannelType)
            {
                case VirtualSpeakerMode.Mono :
                    return getMonoSpeakers();
                case VirtualSpeakerMode.Stereo:
                    return StereoSpeakers.getSpeakers();
                case VirtualSpeakerMode.Mode5point1:
                    if (channelCount == 2)
                    {
                        return getStereoSpeakers();
                    } else if (channelCount == 1)
                    {
                        return getMonoSpeakers();
                    }
                    return getFiveOneSpeakers();
                default:
                    return getMonoSpeakers();
            }
        }

        public int GetChannelCount()
        {
            switch (audioChannelType)
            {
                case VirtualSpeakerMode.Mono:
                    return 1;
                case VirtualSpeakerMode.Stereo:
                    return 2;
                case VirtualSpeakerMode.Mode5point1:
                    return 6;
                default:
                    return -1;
            }
        }

        public void SetChannelMap(int[] channelMap)
        {
            this.channelMap = channelMap;
        }

        public void UpdateAudioConfiguration(AudioConfiguration audioConfig)
        {
            var speakers = transform.GetComponentsInChildren<AudioSource>();
            foreach (var speaker in speakers)
            {
                audioConfig.LoadData(speaker);
            }
        }

        private AudioSource[] getMonoSpeakers()
        {
            AudioSource[] speakers = {speaker};
            return speakers;
        }

        private AudioSource[] getStereoSpeakers()
        {
            if (audioChannelType == VirtualSpeakerMode.Mode5point1)
            {
                AudioSource[] speakers = { FiveOneAudioSpeakers._left, FiveOneAudioSpeakers._right };
                //updateSpeakerName(speakers);
                return speakers;
            } else
            {
                AudioSource[] speakers = {StereoSpeakers._left, StereoSpeakers._right};
                updateSpeakerName(speakers);
                return speakers;
            }
        }


        private void updateSpeakerName(AudioSource[] sourceArray)
        {
            // Rendering stereo to 
            if (sourceArray.Length == 2 && audioChannelType == VirtualSpeakerMode.Mode5point1)
            {
                sourceArray[0].gameObject.name = "left";
                sourceArray[1].gameObject.name = "right";
                return;
            }

            for(int i =0; i < sourceArray.Length; i++)
            {
                sourceArray[i].gameObject.name = getspeakername(i);
            }
        }

        private int getChannelIndex(int orderIndex)
        {
            if(audioChannelType == VirtualSpeakerMode.Mode5point1)
            {
                return channelMap[orderIndex];
            }
            return orderIndex;
        }
            /*
            left -right
            right - center
            surround right - left
                0-Left Surround
                1-Right Surround
                2-Left
                3-Right
                4-Center
                5 -LFE
            */
        private AudioSource[] getFiveOneSpeakers()
        {
            AudioSource[] speakers = FiveOneAudioSpeakers.getSpeakers();
            //updateSpeakerName(speakers);
            AudioSource[] indexedSpeakers = new AudioSource[speakers.Length];

            for(int i =0; i< indexedSpeakers.Length; i++)
            {
                indexedSpeakers[getChannelIndex(i)] = speakers[i];
            }
            string text = "";
            for(int i =0; i< channelMap.Length; i++)
                text += channelMap[i];
             for(int i =0; i< indexedSpeakers.Length; i++)
                text += indexedSpeakers[i].gameObject.name+",";
            Debug.Log(text);
            //left, right, center, lfe, sleft, sright
            return indexedSpeakers;
        }
        private string getspeakername(int channelIndex)
        {
            //channel map:0,4,1,2,3,5 
            if(audioChannelType == VirtualSpeakerMode.Mode5point1)
            {
                switch (channelIndex)
                {
                    case 0:
                        return "left";
                    case 1:
                        return "surround-left";
                    case 2:
                        return "right";
                    case 3:
                        return "center";
                    case 4:
                        return "lfe";
                    case 5:
                        return "surround-right";
                    default:
                        return "invalid";
                }
            }
            else if(audioChannelType == VirtualSpeakerMode.Stereo)
            {
                switch (channelIndex)
                {
                    case 0:
                        return "left";
                    case 1:
                        return "right";
                    default:
                        return "invalid";
                }
            }
            return "invalid";
        }

        internal void StopAll()
        {
            Func<AudioSource[]> speakers = () => 
            {
                switch(audioChannelType)
                {
                    case VirtualSpeakerMode.Stereo:
                        return StereoSpeakers.getSpeakers();
                    case VirtualSpeakerMode.Mode5point1:
                        return FiveOneAudioSpeakers.getSpeakers();
                    default:
                        return getMonoSpeakers();
                }
            };

            foreach(var audioSource in speakers())
            {
                audioSource.Stop();
            }

        }
    }
}

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

        private int[] channelMap;
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
        public void SetChannelMap(int[] channelMap)
        {
            this.channelMap = channelMap;
        }

        private AudioSource[] getMonoSpeakers()
        {
            AudioSource[] speakers = {speaker};
            return speakers;
        }

        private AudioSource[] getStereoSpeakers()
        {
            AudioSource[] speakers = {StereoSpeakers._left, StereoSpeakers._right};
            updateSpeakerName(speakers);
            return speakers;
        }

        private void updateSpeakerName(AudioSource[] sourceArray)
        {
             for(int i =0; i < sourceArray.Length; i++)
            {
                sourceArray[i].gameObject.name = getspeakername(i);
            }
        }
        private int getChannelIndex(int orderIndex)
        {
            if(audioChannelType == AudioSpeakerMode.Mode5point1)
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
            AudioSource[] speakers = {FiveOneAudioSpeakers._left, FiveOneAudioSpeakers._surroundLeft, FiveOneAudioSpeakers._right, FiveOneAudioSpeakers._center, FiveOneAudioSpeakers.lfe, FiveOneAudioSpeakers._surroundRight};
            updateSpeakerName(speakers);
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
	            if(audioChannelType == AudioSpeakerMode.Mode5point1)
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
	            else if(audioChannelType == AudioSpeakerMode.Stereo)
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
    }
}

using System.Collections;
using System.Collections.Generic;
namespace Unity.WebRTC
{
    public class AudioSplitHandler
    {
        public Dictionary<int, AudioTrackFilter> audioTrackDictionary = new Dictionary<int, AudioTrackFilter>();
        private int channelCount;
        private int hardwarespeakersCount = 2;
        private int outputBufferSize;

        public AudioSplitHandler(int count, int bufferSize)
        {
            channelCount = count;
            outputBufferSize = hardwarespeakersCount * bufferSize;
        }

        public void AddTrack(int index)
        {
            audioTrackDictionary.Add(index, new AudioTrackFilter(outputBufferSize));
            
        }
        public void SetAudioTrack(int index, float[] data)
        {
            audioTrackDictionary[index].SetAudioTrack(data);
        }
        public bool isBufferEmpty()
        {
            foreach(var key in audioTrackDictionary.Keys)
            {
                if(audioTrackDictionary[key] != null && audioTrackDictionary[key].buffereAvailable)
                    return false;
            }
            return true;

        }
        public float[] GetAudioTrack(int index)
        {
            if(audioTrackDictionary[index] != null)
                return audioTrackDictionary[index].GetAudioTrack();
            return null;
        }

        private float[] GetChannelData(float[] data, int channelIndex)
        {
            List<float> filteredData = new List<float>();
            for(int i = channelIndex; i < data.Length; i+=channelCount)//0,6,12,...
            {
                filteredData.Add(data[i]);
            }
            return filteredData.ToArray();//1048
        }

        public void SetfilteredData(float[] data)
        {
            foreach (var key in audioTrackDictionary.Keys)
            {
                float[] list = GetChannelData(data, key);
                int index = 0;
                float[] cachebuffer = new float[outputBufferSize];
                for(int i = 0; i< list.Length ; i++)//fill all the hardware speakers
                {
                    for(int j =0; j < hardwarespeakersCount; j++)
                        cachebuffer[index+j] = list[i];
                    index +=hardwarespeakersCount;
                }
                SetAudioTrack(key, cachebuffer);
            }
        }
    }

    public class AudioTrackFilter
    {
        public bool buffereAvailable;
        float[] data;

        public AudioTrackFilter(int length)
        {
            this.data = new float[length];
        }
        public void SetAudioTrack(float[] data)
        {
            System.Array.Copy(data, this.data, data.Length);
            buffereAvailable = true;
        }
        public float[] GetAudioTrack()
        {
            if(buffereAvailable)
            {
                buffereAvailable = false;
                return data;
            }
            return null;
            
        }
    }
}

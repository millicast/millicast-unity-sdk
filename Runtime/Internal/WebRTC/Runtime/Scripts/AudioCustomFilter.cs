using System;
using UnityEngine;

namespace Unity.WebRTC
{
    /// <summary>
    /// This event is called on audio thread.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="channels"></param>
    delegate void AudioReadEventHandler(float[] data, int channels, int sampleRate);

    /// <summary>
    ///
    /// </summary>
    internal class AudioCustomFilter : MonoBehaviour
    {
        public event AudioReadEventHandler onAudioRead;
        public bool sender;
        public bool loopback = false;
        public int channelIndex = -1;
        private int m_sampleRate;
        public AudioSplitHandler audioSplitHandler = null;
        public AudioSource audioSource = null;

        void OnEnable()
        {
            OnAudioConfigurationChanged(false);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            m_sampleRate = AudioSettings.outputSampleRate;
            int bufferLength = 0;
            int numBuffers = 0;
            AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
            int channelCount = AudioHelpers.GetAudioSpeakerModeIntFromEnum(AudioSettings.driverCapabilities);
            Debug.Log($"Audio configuration changed:\n\tdevice {deviceWasChanged}\n\tsamplerate {m_sampleRate}\n\tbufferLength {bufferLength}\n\tchannelcount {channelCount}");

            // Need to change the audio clip
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = AudioHelpers.CreateDummyAudioClip("Channel" + channelIndex, m_sampleRate);
            }

            if (audioSplitHandler != null)
            {
                audioSplitHandler.SetSampleBufferSizeAndChannelCount(bufferLength, channelCount);
            }

            if (audioSource != null)
            {
                audioSource.Play();
            }

        }

        /// <summary>
        /// </summary>
        /// <note>
        /// Call on the audio thread, not main thread.
        /// </note>
        /// <param name="data"></param>
        /// <param name="channels"></param>
        void OnAudioFilterRead(float[] data, int channels)
        {
            if(audioSplitHandler != null && channelIndex != -1)
            {
                if(audioSplitHandler.isBufferEmpty())
                    onAudioRead?.Invoke(data, channels, m_sampleRate);
                float[] cache = audioSplitHandler.GetAudioTrack(channelIndex);
                if(cache != null && cache.Length > 0)
                {
                    for(int i = 0; i < data.Length; i++)
                    {
                        data[i] *= cache[i];
                    }
                }
                                      
            }
            else
            {
                onAudioRead?.Invoke(data, channels, m_sampleRate);
            }
            if (sender && !loopback)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0;
                }
            }
        }
    }
}

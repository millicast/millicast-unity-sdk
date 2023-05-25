using Unity.WebRTC;
using UnityEngine;
namespace Dolby.Millicast
{
    [RequireComponent(typeof(AudioListener))]
    class AudioSender : MonoBehaviour
    {
        AudioStreamTrack track;
        int m_sampleRate = 48000;

        // The initialization process have been omitted for brevity.

        private void Start()
        {
            m_sampleRate = AudioSettings.outputSampleRate;
        }

        // This method is called on the audio thread.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            track?.SetData(data, channels, m_sampleRate);
        }

        public void SetAudioTrack(AudioStreamTrack track)
        {
            this.track = track;
        }

        void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            m_sampleRate = AudioSettings.outputSampleRate;
        }
    }
}
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

        void OnEnable()
        {
            OnAudioConfigurationChanged(false);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
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
            Debug.Log($"[AudioSender] Audio configuration changed:\n\tdevice {deviceWasChanged}\n\tsamplerate {m_sampleRate}");
            m_sampleRate = AudioSettings.outputSampleRate;
        }
    }
}
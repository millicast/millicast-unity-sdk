using Unity.WebRTC;
using UnityEngine;
namespace Dolby.Millicast
{
    [RequireComponent(typeof(AudioListener))]
    class AudioSender : MonoBehaviour
    {
        AudioStreamTrack track;
        const int sampleRate = 48000;

        // The initialization process have been omitted for brevity.

        // This method is called on the audio thread.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            track?.SetData(data, channels, sampleRate);
        }
        public void SetAudioTrack(AudioStreamTrack track)
        {
            this.track = track;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using UnityEngine.UI;

namespace Dolby.Millicast
{
    [System.Serializable]
    public class MultiSourceMediaRenderer : MediaRenderers
    {
        public string sourceId;
        public void AddStream(string sourceId)
        {
            this.sourceId = sourceId;
            _renderer.AddStream(sourceId);
        }
        public override void SetTexture(Texture texture)
        {
            Debug.Log("Added SetTexture..."+sourceId);
            _renderer.SetTexture(texture, sourceId);
        }
        public override void SetAudioTrack(AudioStreamTrack audioTrack)
        {
             Debug.Log("Added SetAudioTrack..."+sourceId);
            _renderer.SetAudioTrack(audioTrack, sourceId);
        }
        public override void AddVideoRenderTarget(Material material)
        {
            Debug.Log("Added AddVideoRenderTarget material..."+sourceId);
            _renderer.AddVideoTarget(material, sourceId);
        }

        public override void RemoveVideoRenderTarget(Material material)
        {
            _renderer.RemoveVideoTarget(material, sourceId);
        }

        /// <summary>
        /// Stop rendering the remote video stream on the previously given RawImage.
        /// </summary>
        /// <param name="image">A Unity <see cref="RawImage"/>. </param>
        public override void RemoveVideoRenderTarget(RawImage image)
        {
            _renderer.RemoveVideoTarget(image, sourceId);
        }

        /// <summary>
        /// Add a UI RawImage to display the remote video stream on. The RawImage's texture
        /// will be replaced with the remote video stream texture when it is available. Use this
        /// when you want to use render the remote stream in a GUI.
        /// </summary>
        /// <param name="image">A Unity <see cref="RawImage"/>.</param>
        public override void AddVideoRenderTarget(RawImage image)
        {
            Debug.Log("Added AddVideoRenderTarget..image."+sourceId);
            _renderer.AddVideoTarget(image, sourceId);
        }

         public override void AddAudioRenderTarget(AudioSource audioSource)
        {
            Debug.Log("Added AddAudioRenderTarget..."+sourceId);
            _renderer.AddAudioTarget(audioSource, sourceId);
        }

        /// <summary>
        /// Stop rendering the remote video stream on the previously given material
        /// </summary>
        /// <param name="material">A Unity <see cref="Material"/>. </param>
        public override void RemoveAudioRenderTarget(AudioSource audioSource)
        {
            _renderer.RemoveAudioTarget(audioSource, sourceId);
        }
    }
}

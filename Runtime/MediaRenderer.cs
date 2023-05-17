using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;

namespace Dolby.Millicast
{
    [System.Serializable]
    public class MediaRenderers
    {
        [Tooltip("Enabling will render incoming stream onto mesh renderer's material, if it exists.")]
        public bool _updateMeshRendererMaterial = false;

        [Tooltip("Add your Materials here to rendering incoming video streams")]
        public List<Material> _renderMaterials = new List<Material>();

        [Tooltip("Add your RawImages here for rendering incoming video streams. For UI rendering")]
        public List<RawImage> _renderImages = new List<RawImage>();
        
        [Tooltip("Add your AudioSources here to render incoming audio streams")]
        public List<AudioSource> _renderAudioSources = new List<AudioSource>();

        public List<VirtualAudioSpeaker> virtualAudioSpeakers;
        internal WrapperRenderer _renderer = new WrapperRenderer();


        public virtual void SetTexture(Texture texture)
        {
             Debug.Log("Added SetTexture...");
            _renderer.SetTexture(texture);
        }


        public virtual void SetAudioTrack(AudioStreamTrack audioTrack)
        {
            Debug.Log("Added SetTexture...");
            _renderer.SetAudioTrack(audioTrack);
        }

        public void AddVirtualAudioSpeaker(VirtualAudioSpeaker virtualSpeaker)
        {
            Debug.Log("Added SetTexture...");
            _renderer.AddVirtualAudioSpeaker(virtualSpeaker);
        }

    
        public void AddRenderTargets()
        {
            Debug.Log("Add render target:"+_renderImages.Count);
            foreach (var material in _renderMaterials)
            {
                AddVideoRenderTarget(material);
            }

            foreach (var image in _renderImages)
            {
                AddVideoRenderTarget(image);
            }

        }
         public void AddAudioRenderTargets()
        {
            Debug.Log("Add Audio render target:"+_renderAudioSources.Count);
            foreach (var audio in _renderAudioSources)
            {
                AddAudioRenderTarget(audio);
            }
        }

        public void AddRenderAudioSource()
        {
            foreach (var audioSource in _renderAudioSources)
            {
                AddRenderAudioSource(audioSource);
            }
        }

        public void UpdateMeshRendererMaterial(string streamName, GameObject meshObject)
        {
            if (_updateMeshRendererMaterial && !string.IsNullOrEmpty(streamName))
            {
                // If the current object contains a mesh renderer, we will 
                // update its material with the incoming stream.
                MeshRenderer meshRenderer = meshObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    Material mat = meshRenderer.materials[0];
                    if (mat != null)
                    {
                        mat.name = streamName;
                        AddVideoRenderTarget(mat);
                    }
                }
            }
        }

        /// <summary>
        /// Add a material to display the incoming remote video stream on. The material's main texture
        /// will be replaced with the remote video stream texture when it is available. Using this
        /// to replace a GUI component's material will not work. Use the RawImage overload instead.  
        /// </summary>
        /// <param name="material">A Unity <see cref="Material"/>.</param>
        public virtual void AddVideoRenderTarget(Material material)
        {
            Debug.Log("Added AddVideoRenderTarget...mat");
            _renderer.AddVideoTarget(material);
        }

        /// <summary>
        /// Stop rendering the remote video stream on the previously given material
        /// </summary>
        /// <param name="material">A Unity <see cref="Material"/>. </param>
        public virtual void RemoveVideoRenderTarget(Material material)
        {
            _renderer.RemoveVideoTarget(material);
        }

        /// <summary>
        /// Stop rendering the remote video stream on the previously given RawImage.
        /// </summary>
        /// <param name="image">A Unity <see cref="RawImage"/>. </param>
        public virtual void RemoveVideoRenderTarget(RawImage image)
        {
            _renderer.RemoveVideoTarget(image);
        }

        /// <summary>
        /// Add a UI RawImage to display the remote video stream on. The RawImage's texture
        /// will be replaced with the remote video stream texture when it is available. Use this
        /// when you want to use render the remote stream in a GUI.
        /// </summary>
        /// <param name="image">A Unity <see cref="RawImage"/>.</param>
        public virtual  void AddVideoRenderTarget(RawImage image)
        {
            Debug.Log("Added AddVideoRenderTarget..image.");
            _renderer.AddVideoTarget(image);
        }

        /// <summary>
        /// Add an audio source that will render the received audio stream.
        /// </summary>
        /// <param name="source"> A Unity <see cref="AudioSource"/> instance. </param>
        public void AddRenderAudioSource(AudioSource source)
        {
            Debug.Log("Added AddRenderAudioSource...");
            _renderer.AddAudioTarget(source);
        }

         public virtual void AddAudioRenderTarget(AudioSource audioSource)
        {
            Debug.Log("Added AddAudioRenderTarget...");
            _renderer.AddAudioTarget(audioSource);
        }

        /// <summary>
        /// Stop rendering the remote video stream on the previously given material
        /// </summary>
        /// <param name="material">A Unity <see cref="Material"/>. </param>
        public virtual void RemoveAudioRenderTarget(AudioSource audioSource)
        {
            _renderer.RemoveAudioTarget(audioSource);
        }

        /// <summary>
        /// Remove an audio source so that it stops rendering.
        /// </summary>
        /// <param name="source"> A previously added Unity <see cref="AudioSource"/> instance.</param>
        public void RemoveRenderAudioSource(AudioSource source)
        {
            _renderer.RemoveAudioTarget(source);
        }

        public void ClearRenderMaterials()
        {
            _renderMaterials.Clear();
            _renderer.Clear();
        }
    }
}

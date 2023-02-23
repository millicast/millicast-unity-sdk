using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dolby.Millicast;
namespace Dolby.Millicast.Samples
{
    public class VirtualStreamPlayer : MonoBehaviour
    {
        [Header("Stream Settings:\n")]
        public string streamName;
        [Tooltip("This make sure that the stream is unique to this Player")]
        public bool makeUniqueVideoStreamPlayer;
        [Header("Stream Video Player Aspect Settings:\n")]
        public MeshResolutionSelector.AspectRatio desiredAspectRatio;
        public float scaleFactor = 1.0f;
        public MeshResolutionSelector.VideoMode videoMode;
        private McSubscriber subscriber;
        private MeshRenderer streamingMesh;

        void Awake()
        {
            subscriber = GetComponentInChildren<McSubscriber>();
            streamingMesh = GetComponentInChildren<MeshRenderer>();
            if (subscriber == null)
                throw new System.Exception("Subscriber gameobject not found under VirtualStreamPlayer object");
            if (subscriber == null)
                throw new System.Exception("MeshRenderer gameobject not found under VirtualStreamPlayer object");
            if(string.IsNullOrEmpty(streamName))
            {
                subscriber.enabled = false;
                throw new System.Exception("Stream Name cannot be Empty.Please add Stream Name from Inspector");
            }    
            subscriber.streamName = streamName;
            if (makeUniqueVideoStreamPlayer)
            {
                subscriber.ClearRenderMaterials();
                subscriber.AddVideoRenderTarget(streamingMesh.material);
            }
        }
        /// <summary>
        /// Set the resolution of the Streaming Mesh game object
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="scale"></param>
        public void SetResolution(float width, float height, float scale)
        {
            if (streamingMesh == null)
                streamingMesh = GetComponentInChildren<MeshRenderer>();
            streamingMesh.transform.localScale = new Vector3(width * scale, 1, height * scale);
        }
    }
}

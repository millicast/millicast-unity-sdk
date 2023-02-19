using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;



namespace Dolby.Millicast
{
  /// <summary>
  /// This class implements the common functionality of 
  /// adding and removing texture and audio source renderers.
  /// </summary>
  internal class RawImageRenderer
  {
    // These are images to render a video stream to
    private HashSet<RawImage> _renderImages = new HashSet<RawImage>();

    private Texture _renderTexture;

    /// <summary>
    /// Add a material to display the received video stream texture on. Will set
    /// the underlying texture of the material to the received texture.
    /// </summary>
    /// <param name="material"></param>
    public void AddRawImage(RawImage image)
    {

      _renderImages.Add(image);

      if (_renderTexture != null)
      {
        image.texture = _renderTexture;
      }

    }


    /// <summary>
    /// Stop rendering the texture on the material.
    /// </summary>
    /// <param name="material"></param>
    public void RemoveRawImage(RawImage image)
    {
      _renderImages.Remove(image);
      if (_renderTexture != null)
      {
        image.texture = null;
      }
    }

    public void SetRenderTexture(Texture texture)
    {
      foreach (var i in _renderImages)
      {
        i.texture = texture;
      }
      _renderTexture = texture;
    }
    public void Clear()
    {
      foreach (var image in _renderImages) {
        if (image) {
          image.texture = null;
        }
      }
      _renderImages.Clear();
    }
  }
}
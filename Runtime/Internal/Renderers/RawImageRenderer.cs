using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;



namespace Dolby.Millicast
{

  internal class MultiSourceImage
  {
      public string sourceId;
      private HashSet<RawImage> _renderImages = new HashSet<RawImage>();
      private Texture _renderTexture;

      internal MultiSourceImage(string sourceId)
      {
        this.sourceId = sourceId;
      }

      public void AddRawImage(RawImage image)
      {
        _renderImages.Add(image);

        if (_renderTexture != null)
        {
          image.texture = _renderTexture;
        }
      }
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
      _renderImages.Clear();
    }

  }
  /// <summary>
  /// This class implements the common functionality of 
  /// adding and removing texture and audio source renderers.
  /// </summary>
  internal class RawImageRenderer
  {
        List<MultiSourceImage> multiSourceImages = new List<MultiSourceImage>();

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
    public void AddRawImage(RawImage image, string sourceId)
    {
      MultiSourceImage sourceImage = multiSourceImages.Find(x => x.sourceId.Equals(sourceId));
        if(sourceImage == null)
          return;
        sourceImage.AddRawImage(image);
    }

    public void AddMultiSourceStream(string sourceId)
    {
        MultiSourceImage sourceImage = multiSourceImages.Find(x => x.sourceId.Equals(sourceId));
        if(sourceImage == null)
        {
          sourceImage = new MultiSourceImage(sourceId);
          multiSourceImages.Add(sourceImage);
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
    public void RemoveRawImage(RawImage image, string sourceId)
    {
       MultiSourceImage sourceImage = multiSourceImages.Find(x => x.sourceId.Equals(sourceId));
        if(sourceImage == null)
          return;
        sourceImage.RemoveRawImage(image);
    }

    public void SetRenderTexture(Texture texture)
    {
      foreach (var i in _renderImages)
      {
        i.texture = texture;
      }
      _renderTexture = texture;
    }
    public void SetRenderTexture(Texture texture, string sourceId)
    {
      MultiSourceImage sourceImage = multiSourceImages.Find(x => x.sourceId.Equals(sourceId));
        if(sourceImage == null)
          return;
      sourceImage.SetRenderTexture(texture);
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
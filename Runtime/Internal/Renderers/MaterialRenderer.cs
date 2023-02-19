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
  internal class MaterialRenderer
  {

    // These are materials to render a video stream to
    private HashSet<Material> _renderMaterials = new HashSet<Material>();

    private Texture _renderTexture;

    /// <summary>
    /// Add a material to display the received video stream texture on. Will set
    /// the underlying texture of the material to the received texture.
    /// </summary>
    /// <param name="material"></param>
    public void AddMaterial(Material material)
    {

      _renderMaterials.Add(material);

      if (_renderTexture != null)
      {
        material.mainTexture = _renderTexture;
      }

    }


    /// <summary>
    /// Stop rendering the texture on the material.
    /// </summary>
    /// <param name="material"></param>
    public void RemoveMaterial(Material material)
    {
      _renderMaterials.Remove(material);
      if (_renderTexture != null)
      {
        material.mainTexture = null;
      }

    }
    public void SetRenderTexture(Texture texture)
    {
      foreach (var i in _renderMaterials)
      {
        i.mainTexture = texture;
      }
      _renderTexture = texture;
    }
    public void Clear()
    {
      _renderMaterials.Clear();
    }
  }
}



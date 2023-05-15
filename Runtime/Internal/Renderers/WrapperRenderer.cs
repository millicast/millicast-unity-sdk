
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;


namespace Dolby.Millicast
{

  internal class WrapperRenderer
  {

    private RawImageRenderer _rawImageRenderer = new RawImageRenderer();
    private MaterialRenderer _materialRenderer = new MaterialRenderer();

    private AudioSourceRenderer _audioSourceRenderer = new AudioSourceRenderer();

    public void AddVideoTarget(RawImage image)
    {
      _rawImageRenderer.AddRawImage(image);
    }
    public void AddVideoTarget(Material material)
    {
      _materialRenderer.AddMaterial(material);
    }
    public void AddVideoTarget(RawImage image, string sourceId)
    {
      _rawImageRenderer.AddRawImage(image, sourceId);
    }
    public void AddVideoTarget(Material material, string sourceId)
    {
      _materialRenderer.AddMaterial(material, sourceId);
    }

    public void RemoveVideoTarget(RawImage image)
    {
      _rawImageRenderer.RemoveRawImage(image);
    }
    public void RemoveVideoTarget(Material material)
    {
      _materialRenderer.RemoveMaterial(material);
    }
    public void RemoveVideoTarget(RawImage image, string sourceId)
    {
      _rawImageRenderer.RemoveRawImage(image, sourceId);
    }
    public void RemoveVideoTarget(Material material, string sourceId)
    {
      _materialRenderer.RemoveMaterial(material, sourceId);
    }

    public void AddAudioTarget(AudioSource audioSource)
    {
      _audioSourceRenderer.AddAudioSource(audioSource);
    }
     public void AddVirtualAudioSpeaker(VirtualAudioSpeaker virtualSpeaker, int channelCount)
    {
      _audioSourceRenderer.AddVirtualAudioSpeaker(virtualSpeaker, channelCount);
    }
    public void AddAudioTarget(AudioSource audioSource, string streamId)
    {
      _audioSourceRenderer.AddAudioSource(audioSource);
    }
     public void AddVirtualAudioSpeaker(VirtualAudioSpeaker virtualSpeaker, string streamId)
    {
      _audioSourceRenderer.AddVirtualAudioSpeaker(virtualSpeaker);
    }

    public void RemoveAudioTarget(AudioSource audioSource)
    {
      _audioSourceRenderer.RemoveAudioSource(audioSource);
    }

    public void AddStream(string streamId)
    {
      _materialRenderer.AddMultiSourceStream(streamId);
      _rawImageRenderer.AddMultiSourceStream(streamId);
    }


    public void SetTexture(Texture texture)
    {
      _materialRenderer.SetRenderTexture(texture);
      _rawImageRenderer.SetRenderTexture(texture);
    }
    public void SetTexture(Texture texture, string sourceId)
    {
      _materialRenderer.SetRenderTexture(texture, sourceId);
      _rawImageRenderer.SetRenderTexture(texture, sourceId);
    }

    public void SetAudioTrack(AudioStreamTrack track)
    {
      _audioSourceRenderer.SetRenderAudioTrack(track);
    }
    
    public void Clear()
    {
      _materialRenderer.Clear();
      _rawImageRenderer.Clear();
      _audioSourceRenderer.Clear();
    }
  }
}
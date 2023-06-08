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
  /// adding and removing audio source renderers.
  /// </summary>
  internal class AudioSourceRenderer
  {

    // These are images to render a video stream to

    // These are audio sources to render an audio stream to
    private HashSet<AudioSource> _renderAudioSources = new HashSet<AudioSource>();

    private AudioStreamTrack _renderAudioTrack;


    /// <summary>
    /// Add an audio source that will render the received audio stream.
    /// </summary>
    /// <param name="source"></param>
    public void AddAudioSource(AudioSource source)
    {
      _renderAudioSources.Add(source);
      if (_renderAudioTrack != null)
      {
        source.SetTrack(_renderAudioTrack);
        source.loop = true;
        source.Play();
      }
    }

    public void AddVirtualAudioSpeaker(VirtualAudioSpeaker speaker, int channelCount)
    {
        RefreshAudioTrackWithIndex(speaker.getAudioSpeakers(channelCount), channelCount);
    }
    private void RefreshAudioTrackWithIndex(AudioSource[] audiosources, int channelCount)
    {
      if(_renderAudioTrack != null && audiosources != null && audiosources.Length > 0)
       {
          int index = 0;
          foreach (var s in audiosources)
          {
            s.SetTrack(_renderAudioTrack, index++, channelCount);
            s.loop = true;
            s.Play();
          }
       } 
    }

    /// <summary>
    /// Remove an audio source so that it stops rendering.
    /// </summary>
    /// <param name="source"></param>
    public void RemoveAudioSource(AudioSource source)
    {
      _renderAudioSources.Remove(source);
      if (_renderAudioTrack != null)
      {
        source.Stop();
      }
    }
    public void SetRenderAudioTrack(AudioStreamTrack track)
    {
      foreach (var s in _renderAudioSources)
      {
        s.SetTrack(track);
        s.loop = true;
        s.Play();
      }

      _renderAudioTrack = track;
    }

    public void Clear()
    {
      _renderAudioSources.Clear();
    }
  }
}



using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;

namespace Dolby.Millicast
{

    internal class MultiSourceAudioRenderer
  {
      public string sourceId;
      private HashSet<AudioSource> _renderAudioSources = new HashSet<AudioSource>();
      private AudioStreamTrack _renderAudioTrack;

      internal MultiSourceAudioRenderer(string streamId)
      {
        this.sourceId = streamId;
      }

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

    public void AddVirtualAudioSpeaker(VirtualAudioSpeaker speaker)
    {
        RefreshAudioTrackWithIndex(speaker.getAudioSpeakers());
    }
    public void RefreshAudioTrackWithIndex(AudioSource[] audiosources)
    {
      if(_renderAudioTrack != null && audiosources != null && audiosources.Length > 0)
       {
          int index = 0;
          foreach (var s in audiosources)
          {
            s.SetTrack(_renderAudioTrack, index++, StatsParser.inboundAudioStreamChannelCount);
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
        source.SetTrack(null);
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

  }

  /// <summary>
  /// This class implements the common functionality of 
  /// adding and removing audio source renderers.
  /// </summary>
  internal class AudioSourceRenderer
  {
    List<MultiSourceAudioRenderer> multiSourceAudioRenderer = new List<MultiSourceAudioRenderer>();

    // These are images to render a video stream to

    // These are audio sources to render an audio stream to
    private HashSet<AudioSource> _renderAudioSources = new HashSet<AudioSource>();

    private AudioStreamTrack _renderAudioTrack;

    public void AddMultiSourceStream(string sourceId)
    {
        MultiSourceAudioRenderer audioRenderer = multiSourceAudioRenderer.Find(x => x.sourceId.Equals(sourceId));
        if(audioRenderer == null)
        {
          audioRenderer = new MultiSourceAudioRenderer(sourceId);
          multiSourceAudioRenderer.Add(audioRenderer);
        }
    }

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
    public void AddAudioSource(AudioSource source, string sourceId)
    {
        MultiSourceAudioRenderer audioRenderer = multiSourceAudioRenderer.Find(x => x.sourceId.Equals(sourceId));
        if(audioRenderer == null)
          return;
        audioRenderer.AddAudioSource(source);
    }

    public void AddVirtualAudioSpeaker(VirtualAudioSpeaker speaker)
    {
        RefreshAudioTrackWithIndex(speaker.getAudioSpeakers());
    }
    public void AddVirtualAudioSpeaker(VirtualAudioSpeaker speaker, string sourceId)
    {
        MultiSourceAudioRenderer audioRenderer = multiSourceAudioRenderer.Find(x => x.sourceId.Equals(sourceId));
        if(audioRenderer == null)
          return;
        audioRenderer.AddVirtualAudioSpeaker(speaker);
    }
    private void RefreshAudioTrackWithIndex(AudioSource[] audiosources)
    {
      if(_renderAudioTrack != null && audiosources != null && audiosources.Length > 0)
       {
          int index = 0;
          foreach (var s in audiosources)
          {
            s.SetTrack(_renderAudioTrack, index++, StatsParser.inboundAudioStreamChannelCount);
            s.loop = true;
            s.Play();
          }
       } 
    }
    private void RefreshAudioTrackWithIndex(AudioSource[] audiosources, string sourceId)
    {
      MultiSourceAudioRenderer audioRenderer = multiSourceAudioRenderer.Find(x => x.sourceId.Equals(sourceId));
        if(audioRenderer == null)
          return;
        audioRenderer.RefreshAudioTrackWithIndex(audiosources);
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
        source.SetTrack(null);
        source.Stop();
      }
    }
     public void RemoveAudioSource(AudioSource source, string sourceId)
    {
      MultiSourceAudioRenderer audioRenderer = multiSourceAudioRenderer.Find(x => x.sourceId.Equals(sourceId));
        if(audioRenderer == null)
          return;
        audioRenderer.RemoveAudioSource(source);
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
    public void SetRenderAudioTrack(AudioStreamTrack track, string sourceId)
    {
     MultiSourceAudioRenderer audioRenderer = multiSourceAudioRenderer.Find(x => x.sourceId.Equals(sourceId));
        if(audioRenderer == null)
          return;
        audioRenderer.SetRenderAudioTrack(track);
    }

    public void Clear()
    {
      _renderAudioSources.Clear();
    }
  }
}



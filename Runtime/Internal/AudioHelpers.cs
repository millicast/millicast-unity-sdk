
using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using Unity.WebRTC;

namespace Dolby.Millicast
{
  internal class AudioHelpers
  {

    /// <summary>
    /// Maps Unity's AudioSpeakerMode Enum to an integer
    /// </summary>
    /// <returns></returns>
    internal static int GetAudioSpeakerModeIntFromEnum(AudioSpeakerMode mode)  
    {
      switch(mode)
      {
        case AudioSpeakerMode.Mono:
          return 1;
        case AudioSpeakerMode.Stereo:
        case AudioSpeakerMode.Prologic:
          return 2;
        case AudioSpeakerMode.Quad:
          return 4;
        case AudioSpeakerMode.Surround:
          return 5;
        case AudioSpeakerMode.Mode5point1:
          return 6;
        case AudioSpeakerMode.Mode7point1:
          return 8;
        default:
          return -1;
      }
    }
  }
}
using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using Unity.WebRTC;

namespace Dolby.Millicast
{
  internal class Millicast
  {
    private static readonly string VERSION = "1.0.0";

    /// <summary>
    /// This will return the current millicast package version. Mainly
    /// to be used in setting the appropriate User Agent when
    /// communicating with the MediaServer.
    /// </summary>
    /// <returns></returns>
    public static string GetPackageVersion()
    {
      return VERSION;
    }
  }
}


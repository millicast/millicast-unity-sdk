using System;

namespace Dolby.Millicast
{
  /// <summary>
  /// Generally thrown when there is incorrect configuration on the publisher. 
  /// </summary>
  public class PublishingException : Exception
  {
    public PublishingException() { }

    public PublishingException(string message)
        : base(message) { }

    public PublishingException(string message, Exception inner)
        : base(message, inner) { }
  }
}



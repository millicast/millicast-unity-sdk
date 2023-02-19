using System;
using UnityEngine;

namespace Dolby.Millicast
{
    /// <summary>
    /// A Scriptable Object which can be used to configure the stream Account ID, authentication tokens and url's which will be used to connect to media server for subscribing and publishing
    /// More information on where to get those details from can be found in 
    /// https://docs.dolby.io/streaming-apis/docs/getting-started
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "Credentials", menuName = "Millicast/Credentials")]
    public class McCredentials : ScriptableObject
    {
        [Tooltip("This can be found in the Dashboard")]
        public string accountId;

        [Header("Publisher Data")]
        [Tooltip("This can be found in the Dashboard")]
        public string publish_url = "https://director.millicast.com/api/director/publish";

        /// <summary>
        /// In case of publishing, the token used to publish. In case of subscribing, only required
        /// if the stream is a secure-viewer, in which case the token should be the subscriber token. 
        /// </summary>
        [Tooltip("This can be found in the Dashboard")]
        public string publish_token = null;
        [Header("Subscriber Data")]
        [Tooltip("This can be found in the Dashboard")]
        public string subscribe_url = "https://director.millicast.com/api/director/subscribe";

        /// <summary>
        /// In case of publishing, the token used to publish. In case of subscribing, only required
        /// if the stream is a secure-viewer, in which case the token should be the subscriber token. 
        /// </summary>
        [Tooltip("This can be found in the Dashboard")]
        public string subscribe_token = null;
    }

}
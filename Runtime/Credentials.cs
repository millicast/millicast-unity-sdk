using System;
using UnityEngine;

namespace Dolby.Millicast
{
    /// <summary>
    /// Internal representation for communicating with the media server
    /// More information on where to get those details from can be found in 
    /// https://docs.dolby.io/streaming-apis/docs/getting-started
    /// </summary>

    public class Credentials
    {

        public Credentials(McCredentials credentials, bool isSubscriber)
        {
            if(credentials == null)
                throw new Exception("Credentials cannot be null. Please add reference to the Credentials Scriptable Object from Inspector");
            this.accountId = credentials.accountId;
            this.url = isSubscriber ? credentials.subscribe_url : credentials.publish_url;
            this.token = isSubscriber ? credentials.subscribe_token : credentials.publish_token;
        }

        /// <summary>
        /// The account ID the stream belongs to. 
        /// </summary>
        public string accountId;

        /// <summary>
        /// The publish or subscribe url. Can be found in the dashboard. 
        /// </summary>
        public string url;

        /// <summary>
        /// In case of publishing, the token used to publish. In case of subscribing, only required
        /// if the stream is a secure-viewer, in which case the token should be the subscriber token. 
        /// </summary>
        public string token = null;
    }

}
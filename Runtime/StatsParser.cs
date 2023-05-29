using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.WebRTC;
using UnityEngine;
namespace Dolby.Millicast
{
    public class StatsParser 
    {
        public int inboundAudioStreamChannelCount = -1;
        public int[] ChannelMap;
        private RTCPeerConnection peerConnection;
        string pattern = @"channel_mapping=(\d+(,\d+)*)";
        //string codecid = "CIT01_98_channel_mapping=0,4,1,2,3,5;coupled_streams=2;minptime=10;num_streams=4;useinbandfec=1";
        public StatsParser(RTCPeerConnection peerConnection)
        {
            this.peerConnection = peerConnection;
        }

        public int getChannelCount(string tpattern, string text)
        {
            Match match = Regex.Match(text, tpattern);
            if(match.Success)
            {
                string channelsStr = match.Groups[1].Value;
                string[] channels = channelsStr.Split(',');
                ChannelMap = new int[channels.Length];
                for(int i=0; i < channels.Length; i++)
                {
                    ChannelMap[i] = int.Parse(channels[i]);
                }
                int channelsCount = channels.Length;
                return channelsCount;
            }
            return 2;   
        }

        public IEnumerator CheckStats()
        {
            yield return new WaitForSeconds(0.1f);
            if (peerConnection == null)
                yield break;

            var op = peerConnection.GetStats();
            yield return op;
            if (op.IsError)
            {
                Debug.LogErrorFormat("RTCPeerConnection.GetStats failed: {0}", op.Error);
                yield break;
            }

            RTCStatsReport report = op.Value;
             foreach (var stat in op.Value.Stats.Values)
            {
                if (!(stat is RTCInboundRTPStreamStats))
                {
                    continue;
                }
                if (stat is RTCInboundRTPStreamStats inboundStats && inboundStats.kind.Equals("audio"))
                {
                    if(string.IsNullOrEmpty(inboundStats.codecId))
                    continue;

                    var newInboundChannelCount = getChannelCount(pattern, inboundStats.codecId);
                    if (newInboundChannelCount != inboundAudioStreamChannelCount)
                        Debug.Log("Inbound channel count updated:"+newInboundChannelCount);
                    inboundAudioStreamChannelCount = newInboundChannelCount;
                }
            }
        }
    }
}

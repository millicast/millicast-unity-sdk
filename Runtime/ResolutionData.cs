
using System;
using UnityEngine;
using System.Collections.Generic;
namespace Dolby.Millicast
{
    /// <summary>
    /// A class  to get the Resolution Ids mapped to the respective screen sizes. Can be used to populate the resolution Ids as a dropdown and fetch screen size based on selected Resolution
    /// </summary>
    public class ResolutionData
    {
        public class InternalMap
        {
            public SupportedResolutions resolution;
            public StreamSize streamSize;

            public InternalMap(SupportedResolutions res, StreamSize size)
            {
                resolution = res;
                streamSize = size;
            }
        }
        public enum  SupportedResolutions 
        {
            [InspectorName("1280 x 720 (720p)")] RES_720P,
            [InspectorName("1920 x 1080 (1080p)")] RES_1080P,
            [InspectorName("2560 x 1440 (1440p)")] RES_1440P,
            [InspectorName("2048 x 1080 (2K)")] RES_2K,
            [InspectorName("3840 x 2160 (4K)")] RES_4K
        }
        private Dictionary<string, InternalMap> resolutionInfo;

        public ResolutionData()
        {
            resolutionInfo = new Dictionary<string, InternalMap>();
            AddData("1280 x 720 (720p)", SupportedResolutions.RES_720P, new StreamSize { width = 1280, height = 720 });
            AddData("1920 x 1080 (1080p)", SupportedResolutions.RES_1080P, new StreamSize { width = 1920, height = 1080 });
            AddData("2560 x 1440 (1440p)", SupportedResolutions.RES_1440P, new StreamSize { width = 2560, height = 1440 });
            AddData("2048 x 1080 (2K)", SupportedResolutions.RES_2K, new StreamSize { width = 2048, height = 1080 });
            AddData("3840 x 2160 (4K)", SupportedResolutions.RES_4K, new StreamSize { width = 3840, height = 2160 });
        }
        

        private void AddData(string label, SupportedResolutions res, StreamSize streamSize)
        {
            InternalMap data = new InternalMap(res, streamSize);
            resolutionInfo.Add(label, data);
        }

        public List<string> GetResolutionOptions()
        {
            return new List<string>(resolutionInfo.Keys);
        }

        public SupportedResolutions GetResolutionType(string label)
        {
            return resolutionInfo[label].resolution;
        }
        public StreamSize GetStreamSize(string label)
        {
            return resolutionInfo[label].streamSize;
        }
         public StreamSize GetStreamSize(SupportedResolutions res)
        {
            return resolutionInfo[GetResolutionLabel(res)].streamSize;
        }
        public string GetResolutionLabel(SupportedResolutions res)
        {
            foreach(var key in resolutionInfo.Keys)
            {
                if(resolutionInfo[key].resolution == res)
                    return key;
            }
            return "";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static net.rs64.PSD.parser.ChannelImageDataParser;
using static net.rs64.PSD.parser.LayerRecordParser;

namespace net.rs64.PSD.parser
{
    public static class LayerInformationParser
    {
        [Serializable]
        public class LayerInfo
        {
            public uint LayersInfoSectionLength;
            public int LayerCount;
            public int LayerCountAbsValue;

            public LayerRecord[] LayerRecords;
            public ChannelImageData[] ChannelImageData;
        }
        public static LayerInfo PaseLayerInfo(MemoryStream stream)
        {
            var layerInfo = new LayerInfo();
            layerInfo.LayersInfoSectionLength = stream.ReadByteToUInt32();
            layerInfo.LayerCount = stream.ReadByteToInt16();
            layerInfo.LayerCountAbsValue = Mathf.Abs(layerInfo.LayerCount);

            // var firstPos = stream.Position;

            var LayerRecordList = new List<LayerRecord>();
            for (int i = 0; layerInfo.LayerCountAbsValue > i; i += 1)
            {
                LayerRecordList.Add(PaseLayerRecord(stream));
            }
            layerInfo.LayerRecords = LayerRecordList.ToArray();

            // var movedLength = stream.Position - firstPos;
            // Debug.Log($"moved length:{movedLength} LayersInfoSectionLength:{layerInfo.LayersInfoSectionLength}");

            var channelImageDataList = new List<ChannelImageData>();
            for (int i = 0; layerInfo.LayerCountAbsValue > i; i += 1)
            {
                for (int Ci = 0; layerInfo.LayerRecords[i].ChannelInformationArray.Length > Ci; Ci += 1)
                {
                    channelImageDataList.Add(PaseChannelImageData(stream, layerInfo.LayerRecords[i], Ci));
                }
            }
            layerInfo.ChannelImageData = channelImageDataList.ToArray();

            return layerInfo;
        }

    }
}
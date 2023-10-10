using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace net.rs64.PSD.parser
{
    public static class AdditionalLayerInformationParser
    {
        public static Dictionary<string, Type> AdditionalLayerInfoParsersTypes;
        public static Dictionary<string, Type> GetAdditionalLayerInfoParsersTypes()
        {
            var dict = new Dictionary<string, Type>();

            foreach (var addLYType in AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(I => I.GetTypes())
                 .Where(I => I.GetCustomAttribute<AdditionalLayerInfoParserAttribute>() != null))
            {
                var Instants = Activator.CreateInstance(addLYType) as AdditionalLayerInfo;

                var customAttribute = addLYType.GetCustomAttribute<AdditionalLayerInfoParserAttribute>();
                if (!dict.ContainsKey(customAttribute.Code))
                {
                    dict.Add(customAttribute.Code, addLYType);
                }
            }

            return dict;
        }
        public static AdditionalLayerInfo[] PaseAdditionalLayerInfos(Stream stream)
        {
            var addLayerInfoList = new List<AdditionalLayerInfo>();
            if (AdditionalLayerInfoParsersTypes == null) AdditionalLayerInfoParsersTypes = GetAdditionalLayerInfoParsersTypes();
            var addLayerInfoParsers = AdditionalLayerInfoParsersTypes;
            while (stream.Position < stream.Length)
            {
                if (!ParserUtility.Signature(stream, PSDLowLevelParser.OctBIMSignature)) { break; }
                var keyCode = stream.ReadBytes(4).ParseUTF8();
                uint length = stream.ReadByteToUInt32();

                if (addLayerInfoParsers.ContainsKey(keyCode))
                {
                    var parser = Activator.CreateInstance(addLayerInfoParsers[keyCode]) as AdditionalLayerInfo;
                    parser.Length = length;
                    parser.ParseAddLY(stream);
                    addLayerInfoList.Add(parser);
                }
            }
            return addLayerInfoList.ToArray();
        }

        [Serializable]
        public class AdditionalLayerInfo
        {
            public uint Length;
            public virtual void ParseAddLY(Stream stream) { }
        }
        [AttributeUsage(AttributeTargets.Class)]
        public class AdditionalLayerInfoParserAttribute : Attribute
        {
            public string Code;
            public AdditionalLayerInfoParserAttribute(string codeStr)
            {
                Code = codeStr;
            }
        }

        [Serializable, AdditionalLayerInfoParser("luni")]
        public class luni : AdditionalLayerInfo
        {
            public string LayerName;

            public override void ParseAddLY(Stream stream)
            {
                LayerName = stream.ReadBytes(Length).ParseUTF16();
            }
        }
        [Serializable, AdditionalLayerInfoParser("lnsr")]
        public class lnsr : AdditionalLayerInfo
        {
            public int IDForLayerName;

            public override void ParseAddLY(Stream stream)
            {
                IDForLayerName = stream.ReadByteToInt32();
            }
        }
        [Serializable, AdditionalLayerInfoParser("lyid")]
        public class lyid : AdditionalLayerInfo
        {
            public int ChannelID;

            public override void ParseAddLY(Stream stream)
            {
                ChannelID = stream.ReadByteToInt32();
            }
        }
        [Serializable, AdditionalLayerInfoParser("lsct")]
        public class lsct : AdditionalLayerInfo
        {
            public SelectionDividerTypeEnum SelectionDividerType;
            public string BlendModeKey;
            public int SubType;

            public enum SelectionDividerTypeEnum
            {
                AnyOther = 0,
                OpenFolder = 1,
                ClosedFolder = 2,
                BoundingSectionDivider = 3,
            }

            public override void ParseAddLY(Stream stream)
            {
                SelectionDividerType = (lsct.SelectionDividerTypeEnum)stream.ReadByteToUInt32();
                if (Length >= 12)
                {
                    stream.ReadBytes(4);
                    BlendModeKey = stream.ReadBytes(4).ParseUTF8();
                }
                if (Length >= 16)
                {
                    SubType = stream.ReadByteToInt32();
                }
            }
        }

    }
}
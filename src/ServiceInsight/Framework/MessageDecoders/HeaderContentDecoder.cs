﻿namespace ServiceInsight.Framework.MessageDecoders
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using Anotar.Serilog;
    using Newtonsoft.Json;
    using ServiceInsight.Models;

    public class HeaderContentDecoder : IContentDecoder<IList<HeaderInfo>>
    {
        IContentDecoder<string> stringDecoder;

        public HeaderContentDecoder(IContentDecoder<string> stringDecoder)
        {
            this.stringDecoder = stringDecoder;
        }

        public DecoderResult<IList<HeaderInfo>> Decode(byte[] headers)
        {
            if (headers != null && headers.Length != 0)
            {
                var headerAsString = stringDecoder.Decode(headers);
                if (headerAsString.IsParsed)
                {
                    var headerAsJson = TryParseJson(headerAsString.Value);
                    if (headerAsJson.IsParsed)
                    {
                        return headerAsJson;
                    }

                    var headerAsXml = TryParseXml(headerAsString.Value);
                    if (headerAsXml.IsParsed)
                    {
                        return headerAsXml;
                    }
                }
            }

            return new DecoderResult<IList<HeaderInfo>>();
        }

        static DecoderResult<IList<HeaderInfo>> TryParseJson(string value)
        {
            try
            {
                if (value.StartsWith("{") || value.StartsWith("["))
                {
                    var json = JsonConvert.DeserializeObject<IList<HeaderInfo>>(value);
                    return new DecoderResult<IList<HeaderInfo>>(json, json != null);
                }
            }
            catch (Exception ex)
            {
                LogTo.Error(ex, "Error trying to parse Json {value}", value);
                // Swallow
            }

            return new DecoderResult<IList<HeaderInfo>>();
        }

        static DecoderResult<IList<HeaderInfo>> TryParseXml(string value)
        {
            try
            {
                if (value.StartsWith("<"))
                {
                    var serializer = new XmlSerializer(typeof(HeaderInfo[]));
                    var deserialized = (HeaderInfo[])serializer.Deserialize(new StringReader(value));
                    return new DecoderResult<IList<HeaderInfo>>(deserialized);
                }
            }
            catch (Exception ex)
            {
                LogTo.Error(ex, "Error trying to parse XML {value}", value);
                // Swallow
            }

            return new DecoderResult<IList<HeaderInfo>>();
        }

        DecoderResult IContentDecoder.Decode(byte[] content)
        {
            return Decode(content);
        }
    }
}
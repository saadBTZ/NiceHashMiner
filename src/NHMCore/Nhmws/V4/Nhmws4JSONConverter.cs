﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace NHMCore.Nhmws.V4
{
    public class Nhmws4JSONConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var sValue = value switch
            {
                (int min, int max) => new JArray(min, max),
                (int len, string charset) => charset != null ? new JArray(len, charset) : new JArray(len),
                (int len, null) => new JArray(len),
                (string name, string unit) => new JArray(name, unit),
                _ => new JArray(),
            };
            serializer.Serialize(writer, sValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof((int min, int max)) 
                || objectType == typeof((int len, string charset))
                || objectType == typeof((string name, string unit));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace NzbDrone.Common
{
    [JsonConverter(typeof(SecretStringJsonConverter))]
    public sealed class SecretString
    {
        private readonly string _secret;
        private SecretString(string secret)
        {
            _secret = secret;
        }

        public override int GetHashCode()
        {
            return _secret.GetHashCode();
        }

        public override string ToString()
        {
            return _secret;
        }

        public static explicit operator SecretString(string secret)
        {
            return new SecretString(secret);
        }

        public static implicit operator string(SecretString secret)
        {
            return secret._secret;
        }
    }

    internal class SecretStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return (SecretString)reader.ReadAsString();
        }
    }
}

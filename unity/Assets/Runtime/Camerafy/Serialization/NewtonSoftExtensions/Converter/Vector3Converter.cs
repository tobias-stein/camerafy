using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Camerafy.Serialization.NewtonSoftExtensions.Converter
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.x},{value.y},{value.z}");
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            Vector3 vec3 = Vector3.zero;

            string strValue = (string)reader.Value;
            string[] xyz = strValue.Split(',');

            if (xyz.Length != 3)
                return Vector3.zero;

            // parse x,y,z values
            float.TryParse(xyz[0], out vec3.x);
            float.TryParse(xyz[1], out vec3.y);
            float.TryParse(xyz[2], out vec3.z);

            return vec3;
        }
    }
}

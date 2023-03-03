using System;
using System.IO;
 
namespace Camerafy.Serialization
{
    /// <summary>
    /// Manages saving and loading entities to disc.
    /// </summary>
    public static class EntityManager
    {
        /// <summary>
        /// Initialize fields.
        /// </summary>
        static EntityManager()
        {
        }

        /// <summary>
        /// Converts an object to json string
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static string ToString(object entity, bool pretty = false, bool allowTypeHandling = true)
        {
            var SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                Formatting = pretty ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
                TypeNameHandling = allowTypeHandling ? Newtonsoft.Json.TypeNameHandling.Auto : Newtonsoft.Json.TypeNameHandling.None,
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(entity, SerializerSettings);
        }

        /// <summary>
        /// Deserialize string to anonymous class object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static T FromString<T>(string input, T def)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeAnonymousType<T>(input, def);
        }

        public static T FromString<T>(string input)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(input);
        }

        /// <summary>
        /// Writes an object as json text to stream
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="stream"></param>
        public static bool ToStream(object entity, Stream stream, bool pretty = false)
        {
            try
            {
                var serializer = new Newtonsoft.Json.JsonSerializer
                {
                    Formatting = pretty ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
                };

                using (var sw = new StreamWriter(stream, System.Text.Encoding.UTF8))
                using (var jtw = new Newtonsoft.Json.JsonTextWriter(sw))
                {
                    serializer.Serialize(jtw, entity);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }

            return true;
        }

        public static T FromStream<T>(Stream stream, T def, System.Text.Encoding encoding = null)
        {
            try
            {
                using (StreamReader sr = new StreamReader(stream, encoding != null ? encoding : System.Text.Encoding.UTF8))
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(sr.ReadToEnd(), def);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return default;
        }

        public static T FromStream<T>(Stream stream, System.Text.Encoding encoding = null)
        {
            try
            {
                using (StreamReader sr = new StreamReader(stream, encoding != null ? encoding : System.Text.Encoding.UTF8))
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return default;
        }

        /// <summary>
        /// Serialize and persists an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="filepath"></param>
        public static void SaveEntity(object entity, string filepath)
        {
            try
            {
                // serialize object to json string
                string json = EntityManager.ToString(entity, true);

                FileInfo fi = new FileInfo(filepath);

                // make sure path exists
                Directory.CreateDirectory(fi.DirectoryName);

                using (Stream stream = fi.Create())
                {
                    using (StreamWriter writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                    {
                        writer.WriteAsync(json).Wait();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Loads and deserializes an entity from disc.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static T LoadEntity<T>(string filepath) where T : class
        {
            T entity = null;

            FileInfo fi = new FileInfo(filepath);

            if (!fi.Exists)
            {
                Logger.Warning("No entity with name '{0}' exists.", filepath);
                return null;
            }

            try
            {
                using (Stream stream = fi.OpenRead())
                {
                    using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();

                        var DeserializerSettings = new Newtonsoft.Json.JsonSerializerSettings
                        {
                            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
                        };

                        entity = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, DeserializerSettings);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return entity;
        }
    }
}

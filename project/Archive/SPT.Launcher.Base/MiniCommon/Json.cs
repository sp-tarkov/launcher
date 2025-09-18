/* Json.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Text;
using SPT.Launcher.Controllers;

namespace SPT.Launcher.MiniCommon
{
    public static class Json
    {
        public static string Serialize<T>(T data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public static string SerializeSingleQuotes<T>(T Data)
        {
            StringBuilder sb = new StringBuilder();
            
            using (StringWriter sw = new StringWriter(sb))
            using (JsonTextWriter jw = new JsonTextWriter(sw))
            {
                jw.QuoteChar = '\'';
                
                JsonSerializer ser = new JsonSerializer();
                ser.Serialize(jw, Data);
            }
            
            return sb.ToString();
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static void Save<T>(string filepath, T data)
        {
            string json = Serialize<T>(data);
            File.WriteAllText(filepath, json);
        }

        /// <summary>
        /// Save an object as json with formatting
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filepath">Full path to file</param>
        /// <param name="data">Object to save to json file</param>
        /// <param name="format">NewtonSoft.Json Formatting</param>
        public static bool SaveWithFormatting<T>(string filepath, T data, Formatting format)
        {
            try
            {

                if (!File.Exists(filepath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                }

                File.WriteAllText(filepath, JsonConvert.SerializeObject(data, format));
                
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.Exception(ex);
            }
            
            return false;
        }

        /// <summary>
        /// Load a class from file and don't save it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filepath">Full path to the file to load</param>
        /// <param name="AllowNullValues">Allow null class property values to be returned. Default is false</param>
        /// <returns>Returns a class object or null</returns>
        public static T LoadClassWithoutSaving<T>(string filepath, bool AllowNullValues = false) where T : class
        {
            if (File.Exists(filepath))
            {
                string json = File.ReadAllText(filepath);

                T classObject = JsonConvert.DeserializeObject<T>(json);

                if (!AllowNullValues)
                {
                    if (classObject.GetType().GetProperties().Any(x => x.GetValue(classObject) == null))
                    {
                        return null;
                    }
                }

                return classObject;
            }

            return null;
        }

        public static T Load<T>(string filepath) where T : new()
        {
            if (!File.Exists(filepath))
            {
                Save(filepath, new T());
                return Load<T>(filepath);
            }

            string json = File.ReadAllText(filepath);
            return Deserialize<T>(json);
        }

        /// <summary>
        /// Get a single property back from a json file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="FilePath">Full Path to json file</param>
        /// <param name="PropertyName">Name of property to return</param>
        /// <returns></returns>
        public static T GetPropertyByName<T>(string FilePath, string PropertyName)
        {
            using (StreamReader sr = new StreamReader(FilePath))
            {
                var tempData = JObject.Parse(sr.ReadToEnd());

                if (tempData != null)
                {
                    if (tempData[PropertyName].Value<T>() is T requestedData)
                    {
                        return requestedData;
                    }
                }
            }

            return default;
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Camerafy.Application.Config
{
    public class Configuration
    {
        #region CONFIGURATION VALUES

        #region SESSION SETTINGS

        /// <summary>
        /// If this setting is true, the user must authenticate before accessing the session.
        /// </summary>
        public bool UserLogin { get; set; } = false;

        /// <summary>
        /// Unique static session id for running application instance.
        /// </summary>
        public string SessionId { get; set; } = "c914ec502d666eba9069f88b293e64b7";

        /// <summary>
        /// Maximum concurrent allowed users.
        /// </summary>
        public int MaximumUserSlots { get; set; } = 3;

        /// <summary>
        /// Timeout in seconds when a user is considered 'timed out'. This happens, if no user input is received.
        /// </summary>
        public int UserSessionTimout { get; set; } = 60;

        /// <summary>
        /// The maximum time interval the client has to send its ready state.
        /// </summary>
        public int UserConnectingTimout { get; set; } = 10;

        /// <summary>
        /// Timeout in seconds when user is considered to be afk.
        /// </summary>
        public int UserInactiveTimeout { get; set; } = 10;

        /// <summary>
        /// The time in seconds it takes to fade the loading screen.
        /// </summary>
        public float LoadingScreenFadeTime { get; set; } = 1.0f;

        #endregion // SESSION SETTINGS

        #region SERVER SETTINGS

        /// <summary>
        /// Port of the internal application http listener.
        /// </summary>
        public int CamerafyHttpListenerPort { get; set; } = 8888;

        #endregion // SERVER SETTINGS

        #region MESSAGE BROKER SETTINGS

        /// <summary>
        /// Host address of message broker for signaling.
        /// </summary>
        public string SignaligServer { get; set; } = "localhost";

        /// <summary>
        /// Port of the message broker.
        /// </summary>
        public int SignaligServerPort { get; set; } = 61613;

        /// <summary>
        /// The amount of time in seconds a pending message is valid. If a
        /// message is received with an older timestamp it will be ignored.
        /// </summary>
        public int MessageExirationTime { get; set; } = 30;

        #endregion // MESSAGE BROKER SETTINGS

        #region WEBRTC SETTINGS

        public bool EnableWebrtcLogging { get; set; } = false;

        public class IceServer
        {
            public string urls { get; set; }
            public string username { get; set; } = "";
            public string credential { get; set; } = "";
        }

        public List<IceServer> iceServers { get; set; } = new List<IceServer>();

        public int MaxFps { get; set; } = 30;

        public int MaxFrameWidth { get; set; } = 320;

        public int MaxFrameHeight { get; set; } = 240;

        #endregion // WEBRTC SETTINGS

        #endregion

        #region Load New Configuration Instance

        [Newtonsoft.Json.JsonIgnore]
        public string ConfigurationFilepath { get; set; } = "Config/AppConfig.json";

        public static Configuration Load()
        {
            // 0. Initialize default configuration
            Configuration Config = new Configuration();

            // 1. Pre-load config from environment variables to potentially allow override of 'ConfigurationFilepath'
            Config.Apply(Environment.GetEnvironmentVariables());

            // 2. Pre-load config from command-line arguments to potentially allow override of 'ConfigurationFilepath'
            Config.Apply(Configuration.CmdLineToDictionary());

            // 3. Try load configuration from file
            var configfile = new System.IO.FileInfo(Config.ConfigurationFilepath);
            if (configfile.Exists)
            {
                Logger.Debug($"Loading configuration data from file '{configfile}'");
                Config = Serialization.EntityManager.LoadEntity<Configuration>(configfile.FullName);
            }

            // 4. Apply any configuration changes from environment variables
            Config.Apply(Environment.GetEnvironmentVariables());

            // 5. Apply any configuration changes from command-line arguments
            Config.Apply(Configuration.CmdLineToDictionary());

            // print final loaded config to log
            Config.Print();

            // return final configuration
            return Config;
        }

        private void Print()
        {
            Logger.Info("=== Current Loaded Configuration ===");
            foreach(var prop in this.GetType().GetProperties())
            {
                if (Type.GetTypeCode(prop.GetType()) == TypeCode.Object)
                {
                    
                    Logger.Info($"{prop.Name}: {Serialization.EntityManager.ToString(prop.GetValue(this), true)}");
                }
                else
                {
                    Logger.Info($"{prop.Name}: {prop.GetValue(this)}");
                }
            }
        }

        private static IDictionary CmdLineToDictionary()
        {
            var Result = new Dictionary<string, string>();

            try
            {
                // parse command-line and look for 'camfy.' prefixed arguments         
                foreach (Match m in Regex.Matches(Environment.CommandLine, @"camfy.([\w\d]+)=(?(?=\"")(\""([^""]|\\"")+\"")|([\w\d-+]+))"))
                {
                    string str = m.Value;
                    string[] kvp = str.Split(new char[] { '=' }, 2);

                    if (Result.ContainsKey(kvp[0]))
                    {
                        Logger.Warning($"Command-line contains multiple definitions for value '{kvp[0]}'. Only the first definition is accepted.");
                        continue;
                    }

                    if (kvp[1].StartsWith("\""))
                    {
                        Result.Add(kvp[0], kvp[1].Substring(1, kvp[1].Length - (kvp[1].EndsWith("\"") ? 2 : 1)));
                    }
                    else
                    {
                        Result.Add(kvp[0], kvp[1]);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return Result;
        }

        private void Apply(IDictionary InVariables)
        {
            Type Class = typeof(Configuration);

            foreach (DictionaryEntry e in InVariables)
            {
                string key = e.Key as string;
                string value = e.Value as string;

                // ignore environment variables not starting with 'camfy.' prefix
                if (!key.StartsWith("camfy."))
                    continue;

                PropertyInfo PI = Class.GetProperty(key.Substring("camfy.".Length));
                if (PI != null)
                {
                    this.SetValue(PI, value);
                }
                else
                {
                    Logger.Warning($"Variable {key} could not applied to configuration. No property found with that name.");
                }
            }
        }

        private void SetValue(PropertyInfo InPI, string InValue)
        {
            try
            {
                switch (Type.GetTypeCode(InPI.PropertyType))
                {
                    case TypeCode.Boolean: InPI.SetValue(this, Boolean.Parse(InValue)); break;
                    case TypeCode.Byte: InPI.SetValue(this, Byte.Parse(InValue)); break;
                    case TypeCode.Char: InPI.SetValue(this, Char.Parse(InValue)); break;
                    case TypeCode.DateTime: InPI.SetValue(this, DateTime.Parse(InValue)); break;
                    case TypeCode.Decimal: InPI.SetValue(this, Decimal.Parse(InValue)); break;
                    case TypeCode.Double: InPI.SetValue(this, Double.Parse(InValue)); break;
                    case TypeCode.Int16: InPI.SetValue(this, Int16.Parse(InValue)); break;
                    case TypeCode.Int32: InPI.SetValue(this, Int32.Parse(InValue)); break;
                    case TypeCode.Int64: InPI.SetValue(this, Int64.Parse(InValue)); break;
                    case TypeCode.SByte: InPI.SetValue(this, SByte.Parse(InValue)); break;
                    case TypeCode.Single: InPI.SetValue(this, Single.Parse(InValue)); break;
                    case TypeCode.String: InPI.SetValue(this, InValue); break;
                    case TypeCode.UInt16: InPI.SetValue(this, UInt16.Parse(InValue)); break;
                    case TypeCode.UInt32: InPI.SetValue(this, UInt32.Parse(InValue)); break;
                    case TypeCode.UInt64: InPI.SetValue(this, UInt64.Parse(InValue)); break;

                    default:
                        Logger.Warning($"Cannot apply variable to {InPI.GetType().Name} value. Only elementar values are supported.");
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        #endregion
    }
}

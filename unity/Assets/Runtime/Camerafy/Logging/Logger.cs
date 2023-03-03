using log4net;
using System;

namespace Camerafy
{

    /// <summary>
    /// Centralized logger class. All loging should be done with this class.
    /// </summary>    
    [System.Reflection.Obfuscation(Exclude = true)]
    public sealed class Logger
    {
        #region LOGGER

        static readonly ILog LOGGER;

        static Logger()
        {
            // set log file name
            log4net.GlobalContext.Properties["LogfilePath"] = new System.IO.FileInfo("Logs").FullName;
            // read log config
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("Config/log4net.config"));
            // create logger instance
            LOGGER = LogManager.GetLogger("CamerafyLogger");
        }

        #endregion

        /// <summary>
        /// Log an debug message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Debug(string message, params object[] args)
        {
            if (!LOGGER.IsDebugEnabled)
                return;

            string msg = EvalMessage(message, args);

            // log to file
            LOGGER.Debug(msg);
            // log to unity
            UnityEngine.Debug.Log(msg);
        }

        /// <summary>
        /// Log an info message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Info(string message, params object[] args)
        {
            if (!LOGGER.IsInfoEnabled)
                return;

            string msg = EvalMessage(message, args);

            // log to file
            LOGGER.InfoFormat(msg);
            // log to unity
            UnityEngine.Debug.Log(msg);
        }

        /// <summary>
        /// Log an warning message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Warning(string message, params object[] args)
        {
            if (!LOGGER.IsWarnEnabled)
                return;

            string msg = EvalMessage(message, args);

            // log to file
            LOGGER.Warn(msg);
            // log to unity
            UnityEngine.Debug.LogWarning(msg);
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Error(string message, params object[] args)
        {
            if (!LOGGER.IsErrorEnabled)
                return;

            string msg = EvalMessage(message, args);

            // log to file
            LOGGER.Error(msg);
            // log to unity
            UnityEngine.Debug.LogErrorFormat(msg);
        }

        public static void Error(Exception ex)
        {
            if (!LOGGER.IsErrorEnabled)
                return;

            // log to file
            LOGGER.Error("", ex);
            // log to unity
            UnityEngine.Debug.LogException(ex);
        }

        /// <summary>
        /// Log an fatal message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Fatal(string message, params object[] args)
        {
            if (!LOGGER.IsFatalEnabled)
                return;

            string msg = EvalMessage(message, args);

            // log to file
            LOGGER.Fatal(msg);
            // log to unity
            UnityEngine.Debug.LogError(msg);
        }

        public static void Fatal(Exception ex)
        {
            if (!LOGGER.IsFatalEnabled)
                return;

            // log to file
            LOGGER.Fatal("", ex);
            // log to unity
            UnityEngine.Debug.LogException(ex);
        }

        /// <summary>
        /// Trys to evaluate messgage. If anything  goes wrong it just returns the fmt string.
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static string EvalMessage(string fmt, params object[] args)
        {
            try
            {
                return string.Format(fmt, args);
            }
            catch (Exception)
            {
                return fmt;
            }
        }
    }
}
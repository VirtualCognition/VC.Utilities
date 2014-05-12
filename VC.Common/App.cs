using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC
{
    public enum DebugMode
    {
        None = 0,
        Low,
        Normal,
        High,
    }
    
    [Serializable]
    public class AppOptions
    {
        public DebugMode? DebugMode { get; set; }
        public bool? SetupLogging { get; set; }
        /// <summary>
        /// Commit logging to network on App close (Default: true)
        /// </summary>
        public bool? LoggingCommit { get; set; }
        private LoggingType _loggingType = LoggingType.Simple;
        public LoggingType LoggingType
        {
            get { return _loggingType; }
            set { _loggingType = value; }
        }
        public bool? ConsoleSetup { get; set; }
        public bool? ConsoleVisible { get; set; }
    }

    public static class App
    {
        private static readonly object _syncRoot = new object();
        private static bool _isInitialized = false;

        public static AppOptions Options { get; private set; }
        public static string AppName { get; private set; }
        public static Logger Logger { get; private set; }

        private static DebugMode _debugMode = DebugMode.None;
        public static DebugMode DebugMode
        {
            get { return _debugMode; }
            set { _debugMode = value; }
        }
        public static bool IsDebug
        {
            get { return _debugMode > DebugMode.None; }
            set
            {
                if (value)
                {
                    if (_debugMode < DebugMode.Normal)
                    {
                        _debugMode = DebugMode.Normal;
                    }
                }
                else
                {
                    _debugMode = DebugMode.None;
                }
            }
        }

        public static void Init(string appName, AppOptions options)
        {
            if (options == null)
            {
                options = new AppOptions();
            }

            lock (_syncRoot)
            {
                if (_isInitialized)
                {
                    throw new InvalidOperationException("App Already Initialized!");
                }

                Options = options;

                AppName = appName;

                if (options.DebugMode.HasValue && options.DebugMode > DebugMode)
                {
                    DebugMode = options.DebugMode.Value;
                }

                if (options.SetupLogging.GetValueOrDefault(true))
                {
                    Logger = Logger.Create(options.LoggingType);

                    if (Logger != null)
                    {
                        Util.Logger = Logger;
                    }
                }

                if (options.ConsoleVisible.GetValueOrDefault(false))
                {
                    WinConsole.WinConsole.Visible = true;
                }
                else if (options.ConsoleSetup.GetValueOrDefault(true))
                {
                    // TODO: Can probably do better here, or just delay
                    WinConsole.WinConsole.Visible = true;
                    WinConsole.WinConsole.Visible = false;
                }

                _isInitialized = true;
            }
        }
        public static void Dispose()
        {
            lock (_syncRoot)
            {
                Util.Msg("App Disposing...");

                if (Logger != null)
                {
                    if (Options.LoggingCommit.GetValueOrDefault(true))
                    {
                        Logger.Commit();

                        if (Util.Logger == Logger)
                        {
                            Util.Logger = null;
                        }
                    }

                    Logger.Dispose();
                }
            }
        }

    }
}

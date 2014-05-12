using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace VC
{
    public static class Util
    {
        #region Base 
        
        public static void Dispose<T>(ref T obj)
            where T:class, IDisposable
        {
            if (obj == null) return;

            obj.Dispose();
            obj = null;
        }

        #endregion
        
        #region Logging

        public static void Msg(string msg, params object[] args)
        {
            Console.WriteLine(msg, args);

            Log(msg, args);
        }

        private static Logger _logger = null;

        public static Logger Logger
        {
            get { return _logger; }
            set
            {
                if (_logger != null && value != null)
                {
                    throw new InvalidOperationException("Util.Logger Already Exists!");
                }

                _logger = value;
            }
        }
        
        public static void Log(string msg, params object[] args)
        {
            if (_logger == null)
            {
                // Warn?
                return;
            }

            _logger.Log(msg, args);
        }
        
        public static void LoggingFlush()
        {
            if (_logger != null)
            {
                _logger.Flush();
            }
        }

        #endregion

        #region String

        public static string ToString(object obj)
        {
            if (obj == null) return null;

            return obj.ToString();
        }
        public static string Simple(string s)
        {
            if (s == null) return null;

            return s.Trim().ToLower();
        }
        static public bool IsEquivalent(string s1, string s2)
        {
            if (s1 == s2)
            {
                return true;
            }
            if (s1 == null || s2 == null)
            {
                return false;
            }
            return s1.Trim().Equals(s2.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }
        public static string Trim(string s)
        {
            if (s == null) return null;

            return s.Trim();
        }
        public static string Replace(string s, string oldValue, string newValue)
        {
            if (s == null) return null;

            return s.Replace(oldValue, newValue);
        }
        public static string PrettyParse(string s)
        {
            return Regex.Replace(s, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);
        }

        // TODO: Expand!
        public static string GetDefaultFormatString(Type type)
        {
            if (type == typeof(double))
            {
                return "#,##0.00";
            }
            else if (type == typeof(decimal))
            {
                return "$#,##0.00";
            }
            else
            {
                return null;
            }
        }

        public static string NewGuid()
        {
            return Guid.NewGuid().ToString().Replace("-", "").ToLower();
        }

        public static string ToString(IEnumerable<string> strings, string delimiter)
        {
            var str = new StringBuilder();

            foreach (var s in strings)
            {
                str.Append(s);
                str.Append(delimiter);
            }

            return str.ToString();
        }

        public static string ToString(IEnumerable<object> objs, string delimiter)
        {
            var str = new StringBuilder();

            foreach (var obj in objs)
            {
                str.Append(ToString(obj));
                str.Append(delimiter);
            }

            return str.ToString();
        }

        public static string[] ParseDelimitedString(string str, char delim = ',')
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            return str.Split(delim);
        }

        #endregion

        #region Collections

        #endregion

        #region Math

        // Options for a better seed or control?
        public static Random Random = new Random();

        public static int RandInt(int max = 100)
        {
            return Random.Next(max);
        }
        public static double Rand()
        {
            return Random.NextDouble();
        }

        public const double PriceEpsilon = 0.0001;
        public const int PriceRoundPlaces = 4;
        public static double RoundPrice(double price)
        {
            return Math.Round(price, PriceRoundPlaces);
        }

        #endregion

        #region Diagnostics
        
        public static void Break()
        {
            Debugger.Break();
        }
        [Conditional("DEBUG")]
        public static void BreakDebug()
        {
            Debugger.Break();
        }
        public static void BreakOrNotify(string message)
        {
            #if DEBUG
            if (App.DebugMode > DebugMode.None)
            {
                Debugger.Break();
                return;
            }
            #endif

            SendEmail(message);
        }
        public static void Assert(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }
        [Conditional("DEBUG")]
        public static void AssertDebug(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }

        #endregion

        #region IO

        public static DirectoryInfo GetExecutingDirectory()
        {
            return (new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location)).Directory;
        }

        public static DirectoryInfo CreateDirectory(string baseDir, params string[] directories)
        {
            var baseDirInfo = new DirectoryInfo(baseDir);

            return CreateDirectory(baseDirInfo, directories);
        }
        public static DirectoryInfo CreateDirectory(DirectoryInfo baseDirInfo, params string[] directories)
        {
            if (!baseDirInfo.Exists)
            {
                return null;
            }

            var dirInfo = baseDirInfo;

            foreach (string dir in directories)
            {
                dirInfo = new DirectoryInfo(Path.Combine(dirInfo.FullName, dir));

                if (dirInfo.Exists)
                {
                    continue;
                }

                dirInfo.Create();

                dirInfo.Refresh();

                if (!dirInfo.Exists)
                {
                    return null;
                }
            }

            return dirInfo;
        }

        #endregion

        #region Environment

        public static string GetMachineName()
        {
            return Simple(Environment.MachineName);
        }
        public static string GetUserString()
        {
            return Trim(Replace(Environment.UserName, "\\", "_"));
        }

        #endregion

        #region Type 

        public static T Create<T>(params object[] args)
            where T:class
        {
            return Activator.CreateInstance(typeof(T), args) as T;
        }

        public static AT GetCustomAttribute<AT, T>()
            where AT : Attribute
        {
            object[] attributes = typeof(T).GetCustomAttributes(typeof(AT), true);

            if (attributes.Length == 0)
            {
                return null;
            }
            else if (attributes.Length > 2)
            {
                Msg("Multiple CustomAttributes ({0}) For: {1}", typeof(AT).Name, typeof(T).Name);
            }

            return attributes[0] as AT;
        }
        public static AT GetCustomAttribute<AT>(object obj) 
            where AT : Attribute
        {
            if (obj == null)
            {
                return null;
            }

            object[] attributes = obj.GetType().GetCustomAttributes(typeof(AT), true);

            if (attributes == null || attributes.Length == 0)
            {
                return null;
            }
            else if (attributes.Length > 2)
            {
                Msg("Multiple CustomAttributes ({0}) For: {1}", typeof(AT).Name, obj.GetType().Name);
            }

            return attributes[0] as AT;
        }
        public static T GetCustomAttribute<T>(PropertyInfo info)
            where T : Attribute
        {
            if (info == null)
            {
                return null;
            }

            object[] attributes = info.GetCustomAttributes(typeof(T), true);

            if (attributes == null || attributes.Length == 0)
            {
                return null;
            }
            else if (attributes.Length > 2)
            {
                Msg("Multiple CustomAttributes ({0}) For: {1}", typeof(T).Name, info.DeclaringType.Name);
            }

            return attributes[0] as T;
        }

        public static IEnumerable<Type> FindDerivedTypes(Assembly assembly, Type baseType)
        {
            return assembly.GetTypes().Where(baseType.IsAssignableFrom);
        }
        public static IEnumerable<Type> FindDerivedTypes<T>()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes().Where(typeof(T).IsAssignableFrom).Where(type => type != typeof(T)));
        }

        // TODO: Expand! 
        public static Type GetDataSourceType(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            Type type = null;

            if (obj is IList)
            {
                var list = (obj as IList);

                Type inspectionType = list.GetType();

                if (inspectionType.IsGenericType)
                {
                    Type[] types = inspectionType.GetGenericArguments();

                    if (types != null && types.Length == 1)
                    {
                        type = types[0];
                    }
                }
                else if (inspectionType.BaseType.IsGenericType)
                {
                    Type[] types = list.GetType().BaseType.GetGenericArguments();

                    if (types != null && types.Length > 0)
                    {
                        type = types[0];
                    }
                }

                if (type == null)
                {
                    if (list.Count > 0)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i] != null)
                            {
                                type = list[i].GetType();
                                break;
                            }
                        }
                    }
                }

                return type;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Email

        public static bool SendEmail(string subject, string body = "", params string[] attachments)
        {
            return SendEmail(Defines.EmailsDefault, subject, body, attachments);
        }
        public static bool SendEmail(IEnumerable<string> recipients, string subject, string body, params string[] attachmentFilenames)
        {
            return false;
        }

        static public bool NotifyEmail(string subject, string body = "")
        {
            return NotifyEmail(null, subject, body);
        }
        static public bool NotifyEmail(IEnumerable<string> recipients, string subject, string body = "")
        {
            if (recipients == null)
            {
                recipients = Defines.EmailsDefault;
            }

            return SendEmail(recipients, subject, body, null);
        }

        #endregion
        
        #region Threading

        private static readonly List<Timer> _pendingTimers = new List<Timer>();

        public static Timer RunAtTime(Action func, DateTime targetTime)
        {
            var dueMs = (int) targetTime.Subtract(DateTime.Now).TotalMilliseconds;

            if (dueMs < 0)
            {
                throw new InvalidOperationException("targetTime is before now");
            }

            return RunAtTime(func, dueMs);
        }
        public static Timer RunAtTime(Action func, int dueMs)
        {
            if (dueMs < 0)
            {
                throw new InvalidOperationException("dueMs must be positive");
            }

            var timer = new Timer(state => func(), null, dueMs, Timeout.Infinite);

            // TODO: Do we want to track/dispose?
            //lock (_pendingTimers)
            //{
            //    _pendingTimers.Add(timer);
            //}

            return timer;
        }

        public static Thread RunThreaded(Action func)
        {
            var thread = new Thread(new ThreadStart(func));

            thread.Start();

            return thread;
        }
        public static Thread RunThreadedSta(Action func)
        {
            var thread = new Thread(new ThreadStart(func));

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return thread;
        }

        #endregion

        #region Networking

        public static bool Ping(string destination)
        {
            // TODO: 
            throw new NotImplementedException();
        }

        #endregion
    }

    public static class ExceptionHandler
    {
        public static event Action<Exception> Exception;
        private static void OnException(Exception ex)
        {
            if (Exception != null)
            {
                Exception(ex);
            }
        }

        public static string LogException(Exception ex)
        {
            string summary = GenerateExceptionSummary(ex);

            string appName = App.AppName;
            if (string.IsNullOrWhiteSpace(appName))
            {
                try
                {
                    appName = Process.GetCurrentProcess().ProcessName;
                }
                catch { }
            }

            string user = "";
            try
            {
                user = Util.GetUserString();
            }
            catch { }

            string machine = "";
            try
            {
                machine = Util.GetMachineName();
            }
            catch {}

            var timestamp = DateTime.Now;

            var filename = string.Format("Exception-{0}-{1:yyMMdd_hhmmss}-{2}-{3}", appName, timestamp, machine, user);

            var dirInfo = Util.CreateDirectory(Defines.LogsRootDirectory, appName, timestamp.ToString("yyMMdd"));

            if (dirInfo == null)
            {
                dirInfo = Util.GetExecutingDirectory();
            }

            if (dirInfo == null || !dirInfo.Exists)
            {
                Alerts.Error("Unable to log exception (failed create directory)");
                return null;
            }

            try
            {
                File.WriteAllText(filename, summary);
            }
            catch (Exception fex)
            {
                Alerts.Error("Failed to log exception");
                Util.Msg("Failed to log exception for file exception: " + fex.Message);
                return null;
            }

            return filename;
        }

        static public string GenerateExceptionSummary(Exception e)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0}\r\n\r\nSource:{1}\r\n\r\nStackTrace:\r\n{2}\r\n\r\n", e.Message, e.Source, e.StackTrace);

            Exception ie = e.InnerException;
            while (ie != null)
            {
                sb.AppendFormat("InnerException: {0}\r\nStackTrace:\r\n{1}\r\n\r\n", ie.Message, ie.StackTrace);
                ie = ie.InnerException;
            }

            return sb.ToString();
        }

        public static void HandleException(string msgPrefix, Exception ex, bool doSendEmail = false)
        {
            HandleException(ex, msgPrefix, doSendEmail);
        }
        public static void HandleException(Exception ex, string msgPrefix = "Unhandled Exception: ", bool doSendEmail = false)
        {
            if (App.DebugMode > DebugMode.Low || Debugger.IsAttached)
            {
                #if DEBUG
                Debugger.Break();
                return;
                #endif
            }

            OnException(ex);

            var filename = LogException(ex);

            if (msgPrefix != null)
            {
                Alerts.Error(msgPrefix + ex.Message);
            }

            if (doSendEmail)
            {
                try
                {
                    string summary = GenerateExceptionSummary(ex);

                    // TODO: We want the actual log written
                    string logFilename = filename;

                    string body = string.Format("{0}\r\n{1}\r\nMachine:{2}\r\nApp:{3}\r\nLog:{4}",
                        msgPrefix,
                        summary,
                        Util.GetMachineName(),
                        App.AppName,
                        logFilename);

                    if (!Util.SendEmail(msgPrefix + ex.Message, body))
                    {
                        Alerts.Error("Failed to send exception email");
                    }
                }
                catch (Exception emailEx)
                {
                    Alerts.Error("Exception sending exception email: " + emailEx.Message);
                }
            }
        }

        public static void Setup()
        {
            System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);
            System.Windows.Forms.Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
        public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }
        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception) e.ExceptionObject);
        }
    }
}

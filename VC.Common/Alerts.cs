using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VC
{
    public static class Alerts
    {
        private static readonly AlertsContainer _container = new AlertsContainer();
        
        // TODO: Abstract to context sensitive call
        public static void Status(string msg, params object[] args)
        {
            Util.Msg("STATUS: " + msg, args);
        }

        public static void Error(string msg, params object[] args)
        {
            Alert(AlertType.Error, msg, args);
        }
        public static void Warning(string msg, params object[] args)
        {
            Alert(AlertType.Warning, msg, args);
        }
        public static void Info(string msg, params object[] args)
        {
            Alert(AlertType.Info, msg, args);
        }
        public static void Alert(AlertType type, string msg, params object[] args)
        {
            Util.Msg("{0,-9}: {1}", type.ToString().ToUpper(), msg);

            string arg = null;

            if (args.Length > 0)
            {
                arg = Util.ToString(args, ",");
            }

            _container.AddAlert(type, msg, arg);
        }

        public static string GetSummary()
        {
            return _container.GetSummary();
        }

        public static int Count
        {
            get { return _container.GetCount(); }
        }
        public static int GetCount(AlertType type)
        {
            return _container.GetCount(type);
        }

        public static IReadOnlyList<Alert> GetAllAlerts()
        {
            return _container.Alerts;
        } 
    }

    public enum AlertType
    {
        None = 0,
        Info,
        Warning,
        Error,
    }

    [Serializable]
    public class Alert
    {
        public const int MaxArgumentCount = 100;

        public AlertType Type { get; set; }
        public string Message { get; set; }
        public int HitCount { get; set; }
        public int UniqueCount
        {
            get
            {
                if (Arguments == null)
                {
                    return 1;
                }
                else
                {
                    return Arguments.Count;
                }
            }
        }
        public List<AlertArgument> Arguments { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Should be called with a lock in any multi-threaded scenario</remarks>
        internal void AddArgument(string arg)
        {
            if (Arguments == null)
            {
                Arguments = new List<AlertArgument>();
            }

            for (int i = 0; i < Arguments.Count; i++)
            {
                if (Arguments[i].Argument == arg)
                {
                    Arguments[i].HitCount++;
                    return;
                }
            }

            if (Arguments.Count >= MaxArgumentCount)
            {
                return;
            }

            Arguments.Add(new AlertArgument()
                          {
                              Argument = arg,
                              HitCount = 1,
                          });
        }
    }

    [Serializable]
    public class AlertArgument
    {
        public string Argument { get; set; }
        public int HitCount { get; set; }
    }

    public class AlertsContainer
    {
        public const int AlertTypeCount = (int)AlertType.Error + 1;

        private readonly List<Alert> _alerts = new List<Alert>(); 
        private readonly Dictionary<string, Alert>[] _alertsDictionaries = new Dictionary<string, Alert>[AlertTypeCount];
        
        internal IReadOnlyList<Alert> Alerts
        {
            get { return _alerts; }
        } 

        public AlertsContainer()
        {
            foreach (AlertType type in Enum.GetValues(typeof (AlertType)))
            {
                if (type == AlertType.None) continue;

                _alertsDictionaries[(int)type] = new Dictionary<string, Alert>();
            }
        }

        public Alert AddAlert(AlertType type, string msg, string arg = null)
        {
            int idx = (int) type;

            if (idx > AlertTypeCount) throw new ArgumentOutOfRangeException("AlertType");

            var dict = _alertsDictionaries[idx];

            Alert alert;
            if (!dict.TryGetValue(msg, out alert))
            {
                lock (_alerts)
                {
                    lock (dict)
                    {
                        if (!dict.TryGetValue(msg, out alert))
                        {
                            alert = new Alert()
                                    {
                                        Type = type,
                                        Message = msg,
                                        HitCount = 1,
                                    };

                            if (arg != null)
                            {
                                alert.AddArgument(arg);
                            }

                            _alerts.Add(alert);
                            dict[msg] = alert;

                            return alert;
                        }
                    }
                }
            }

            // Ok, alert was found

            lock (alert)
            {
                alert.HitCount++;

                if (arg != null)
                {
                    alert.AddArgument(arg);
                }
            }

            return alert;
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();

            lock (_alerts)
            {
                foreach (var dict in _alertsDictionaries)
                {
                    foreach (var alert in dict.Values)
                    {
                        sb.AppendFormat("{0:-8}({1}): {2}\r\n", alert.Type, alert.HitCount, alert.Message);
                    }
                }
            }

            return sb.ToString();
        }

        public int GetCount()
        {
            return _alerts.Count;
        }

        public int GetCount(AlertType type)
        {
            return _alertsDictionaries[(int) type].Count;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC
{
    public enum LoggingType
    {
        None = 0,
        Simple,
        NLog,
        SmartInspect,
    }

    public interface ILogger : IDisposable
    {
        /// <summary>
        /// The file name if relevant
        /// </summary>
        string FileName { get; }
        /// <summary>
        /// The full local file path if relevant
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// The primary logging call
        /// </summary>
        void Log(string msg, params object[] args);
        
        /// <summary>
        /// Flush the stream if relevant
        /// </summary>
        void Flush();

        /// <summary>
        /// Commit the log to permanent storage (e.g. network share) if relevant
        /// </summary>
        string Commit();
    }

    public abstract class Logger : ILogger
    {
        protected string _fileName;
        protected string _filePath;

        public abstract LoggingType Type { get; }

        public string FileName
        {
            get { return _fileName; }
        }
        public string FilePath
        {
            get { return _filePath; }
        }

        public abstract void Dispose();

        public static Logger Create(LoggingType type, params string[] args)
        {
            switch (type)
            {
                case LoggingType.Simple:
                    return new SimpleLogger();
                //case LoggingType.SmartInspect:
                //    return new SmartInspectLogger();
                default:
                    throw new NotSupportedException("LoggingType Not Supported: " + type);
            }
        }

        public abstract void Log(string msg, params object[] args);
        
        protected virtual string GetLocalLogDirectory()
        {
            return Defines.LogsRootLocalDirectory;
        }
        protected virtual string GetRemoteLogDirectory()
        {
            return Defines.LogsRootDirectory;
        }
        protected virtual string GetFilenameBase()
        {
            return string.Format("{0} {1:yyMMdd_HHmmss} {2} {3}", App.AppName, DateTime.Now, Util.GetMachineName(), Util.GetUserString());
        }
        protected virtual string GetFilenameExtension()
        {
            return ".log";
        }
        protected void SetupFile()
        {
            var root = GetFilenameBase();

            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("Logging filename root invalid");
            }

            var directory = GetLocalLogDirectory();

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Logging directory invalid");
            }

            _fileName = root + GetFilenameExtension();

            _filePath = Path.Combine(directory, _fileName);
        }

        public virtual void Flush()
        {
            
        }

        public virtual string Commit()
        {
            try
            {
                Flush();

                if (string.IsNullOrWhiteSpace(_filePath))
                {
                    // We're done here.. 
                    return null;
                }

                var remoteDir = GetRemoteLogDirectory();

                if (string.IsNullOrWhiteSpace(remoteDir))
                {
                    throw new DirectoryNotFoundException("Root Remote Log Directory Doesn't Exist: " + App.AppName);
                }

                var rootDir = new DirectoryInfo(remoteDir);

                if (!rootDir.Exists)
                {
                    throw new DirectoryNotFoundException("Root Remote Log Directory Doesn't Exist: " + rootDir.FullName);
                }

                var fullPath = Path.Combine(rootDir.FullName, _fileName);

                File.Copy(_filePath, fullPath);

                return fullPath;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException("Log Commit Exception:", ex, true);
                
                return null;
            }
        }
    }

    public class SimpleLogger : Logger
    {
        private readonly TextWriter _textWriter;
        private bool isDisposing = false;

        public override LoggingType Type { get { return LoggingType.Simple; } }

        public SimpleLogger()
        {
            SetupFile();

            var stream = File.Open(_filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

            _textWriter = new StreamWriter(stream);
        }
        public override void Dispose()
        {
            if (isDisposing) return;

            isDisposing = true;

            _textWriter.Close();
            _textWriter.Dispose();
        }

        public override void Log(string msg, params object[] args)
        {
            if (isDisposing) return;

            _textWriter.WriteLine(msg, args);
        }

        public override void Flush()
        {
            if (isDisposing) return;

            _textWriter.Flush();
        }
    }

    //public class SmartInspectLogger : Logger
    //{
    //    public override LoggingType Type { get { return LoggingType.SmartInspect; } }

    //    public SmartInspectLogger()
    //    {
    //        SiAuto.Si.AppName = App.AppName;
    //        SiAuto.Si.Connections = string.Format("file(filename={0})", GetFilename());
    //        SiAuto.Si.Enabled = true;
    //        SiAuto.Main.LogMessage("Logging Initialized...");
    //    }
    //    public override void Dispose()
    //    {
    //        SiAuto.Si.Dispose();
    //    }

    //    public override void Log(string msg, params object[] args)
    //    {
    //        SiAuto.Main.LogMessage(msg, args);
    //    }

    //    public override string GetFilenameExtension()
    //    {
    //        return ".sil";
    //    }

    //    public override void Flush()
    //    {

    //    }
    //}
}

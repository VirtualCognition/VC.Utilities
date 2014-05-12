using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VC.DB;

namespace VC.Jobs
{
    [Serializable]
    public class JobManagerConfig
    {
        public int UpdateIntervalMs { get; set; }
        public bool DoStartUpdates { get; set; }

        public JobManagerConfig()
        {
            UpdateIntervalMs = 15 * 1000;
            DoStartUpdates = true;
        }
    }

    public class JobManager : IDisposable
    {
        #region Constants

        public const string JobQueueId = "JobQueue";

        #endregion

        #region Members

        protected JobManagerConfig _config = new JobManagerConfig();
        protected readonly DbConnection _dbConnection;
        // readonly DbQueue _jobQueue;
        protected readonly Dictionary<string, Type> _jobTypeDictionary = new Dictionary<string, Type>();
        protected IJobScheduleRepository _JobScheduleRepository;
        protected IReadOnlyList<JobSchedule> _JobSchedules = null;
        private Timer _UpdateTimer;
        protected IReadOnlyList<JobScheduleInstance> _JobScheduleInstances = null;

        #endregion

        #region Properties

        public IReadOnlyList<JobSchedule> JobSchedules
        {
            get { return _JobSchedules; }
        }
        public IReadOnlyList<JobScheduleInstance> JobScheduleInstances
        {
            get { return _JobScheduleInstances; }
        } 

        #endregion

        public JobManager(DbConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            
            _dbConnection = connection;
            //_jobQueue = new DbQueue(JobQueueId, _dbConnection);
            _JobScheduleRepository = new SqlServerJobRepository(_dbConnection);
        }

        public void Initialize()
        {
            var types = Util.FindDerivedTypes<IJob>();

            if (types == null || !types.Any())
            {
                Alerts.Error("JobManager: No Job Types Loaded!");
                return;
            }

            foreach (var type in types)
            {
                _jobTypeDictionary.Add(type.Name, type);
            }

            if (!LoadSchedules())
            {
                Alerts.Error("Schedule Load Failed");
            }

            if (_config.DoStartUpdates)
            {
                if (_config.UpdateIntervalMs > 1000)
                {
                    _UpdateTimer = new Timer(UpdateTimer_Tick, null, _config.UpdateIntervalMs, _config.UpdateIntervalMs);
                }
                else
                {
                    Alerts.Error("Invalid UpdateIntervalMs: ", _config.UpdateIntervalMs);
                }
            }
        }

        public void Dispose()
        {
            if (_UpdateTimer != null)
            {
                _UpdateTimer.Dispose();
                _UpdateTimer = null;
            }
        }

        public void OnError(string message)
        {   
            Util.BreakDebug();
            Alerts.Error("JobManager: " + message);
        }

        #region Job Schedules/Instances

        protected bool LoadSchedules()
        {
            try
            {
                _JobSchedules = _JobScheduleRepository.GetJobSchedules();

                return _JobSchedules != null && _JobSchedules.Count > 0;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "Unhandled Exception Loading Schedules: ", true);

                return false;
            }
        }

        protected bool DequeueJob(out JobScheduleInstance instance, out string jobId)
        {
            jobId = null;

            instance = _JobScheduleRepository.Dequeue();

            if (instance == null)
            {
                return true;
            }

            // TODO: Check for schedule item

            jobId = instance.JobScheduleId;

            return true;
        }

        protected bool CompleteJob(JobScheduleInstance instance)
        {
            return _JobScheduleRepository.Complete(instance);
        }

        public bool ScheduleDailyInstances()
        {
            if (!LoadSchedules() || _JobSchedules == null)
            {
                Alerts.Error("Unable to load schedules without valid schedule load");
                return false;
            }

            if (_JobSchedules.Count == 0)
            {
                Alerts.Warning("No JobSchedules to schedule?");
                return true;
            }

            Util.BreakDebug();

            foreach (var schedule in _JobSchedules)
            {
                
            }

            return false;
        }

        public bool QueueTestJob()
        {
            //return _jobQueue.EnqueueTask(typeof(TestJob).Name);

            var instance = new JobScheduleInstance()
            {
                JobScheduleId = typeof(TestJob).Name,
                TargetTime = DateTime.UtcNow.AddSeconds(30),
            };

            return _JobScheduleRepository.Enqueue(instance);
        }

        public bool QueryJobInstances()
        {
            _JobScheduleInstances = _JobScheduleRepository.GetJobScheduleInstances();

            return _JobScheduleInstances != null;
        }

        #endregion

        #region Job Processing

        protected void UpdateTimer_Tick(object _)
        {
            try
            {
                CheckForJob();
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "JobManager: Unhandled Exception Processing Job Update: ", true);
            }
        }

        public bool CheckForJob()
        {
            JobScheduleInstance instance;
            string jobId;

            if (!DequeueJob(out instance, out jobId))
            {
                if (!string.IsNullOrWhiteSpace(jobId))
                {
                    throw new InvalidOperationException("Job Dequeue Return False With A Valid Task Id");
                }
                return false;
            }

            if (instance == null)
            {
                return true;
            }
            
            if (jobId == null)
            {
                OnError("No jobId for schedule instance");
                Util.Msg("Error: No jobId for schedule instance: " + instance.InstanceId);
                return false;
            }

            var job = CreateJob(jobId);

            if (job == null)
            {
                OnError("Job Id Not Found: " + jobId);
                return false;
            }

            var result = ExecuteJob(job);

            if (result == null)
            {
                return false;
            }

            instance.Status = result.Status;

            if (!CompleteJob(instance))
            {
                OnError("Failed to update job as completed!");
                return false;
            }

            return true;
        }

        protected IJob CreateJob(string jobId)
        {
            Type type;
            if (_jobTypeDictionary.TryGetValue(jobId, out type))
            {
                return (IJob)Activator.CreateInstance(type);
            }
            
            Alerts.Error("Unknown JobId: ", jobId);

            return null;
        }

        protected IJobResult ExecuteJob(IJob job)
        {
            try
            {
                var result = job.Execute();

                if (result == null)
                {
                    OnError("Job returned without a result: " + job.Id);
                    return null;
                }

                if (!result.Success)
                {
                    OnError("Job Failed: " + job.Id);
                    return result;
                }

                Util.Msg("Job Succeeded: " + job.Id);

                return result;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "Unhandled Job Exception: ", true);
                return null;
            }
        }

        #endregion
    }
}

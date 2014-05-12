using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VC.DB;

namespace VC.Jobs
{
    public enum JobScheduleFrequencyType
    {
        None = 0,
        Once,
        Daily,
        WorkDays,
        WeeklyByDay,
        MonthlyByDate,
        Annual,
    }

    //public enum JobQueueStatus
    //{
    //    Error = -1,
    //    None = 0,
    //    Queued = 1, // Queued, but not picked up yet
    //    Processing = 2,  // Picked up, should be processing
    //    Processed = 3,
    //}

    [Serializable]
    public class JobSchedule : IHasId, IHasGuid
    {
        [Display(false)]
        public string Guid { get; set; }
        [Display(Priority = -1)]
        public string Id
        {
            get { return Guid; }
        }

        public string JobId { get; set; }

        public JobScheduleFrequencyType TargetFrequency { get; set; }
        public string TargetTime { get; set; }
        [XmlIgnore]
        public TimeSpan TargetTimeSpan
        {
            get { return TimeSpan.Parse(TargetTime); }
        }

        public static List<JobScheduleInstance> ScheduleInstances(DateTime date, JobSchedule schedule)
        {
            switch (schedule.TargetFrequency)
            {
                case JobScheduleFrequencyType.None:
                case JobScheduleFrequencyType.Once:
                    return null;
                case JobScheduleFrequencyType.Daily:
                    break;
                case JobScheduleFrequencyType.WorkDays:
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        return null;
                    }
                    else
                    {
                        break;
                    }
                default:
                    Alerts.Warning("JobScheduleFrequencyType Not Implemented: ", schedule.TargetFrequency);
                    return null;
            }

            // Ok, schedule for today.. 

            var instance = new JobScheduleInstance()
                           {
                               JobScheduleId = schedule.Id,
                               TargetTime = date.Add(schedule.TargetTimeSpan)
                           };

            return new List<JobScheduleInstance> { instance };
        } 
    }

    [Serializable]
    public class JobScheduleInstance : IHasId
    {
        public string Id
        {
            get { return InstanceId.ToString(); }
        }
        public int InstanceId { get; set; }
        public string JobScheduleId { get; set; }

        public DateTime TargetTime { get; set; } // UTC
        public Status Status { get; set; }
    }

    public interface IJobScheduleRepository
    {
        bool Enqueue(JobScheduleInstance instance);
        JobScheduleInstance Dequeue();
        bool Complete(JobScheduleInstance instance);
        DateTime? GetNextInstanceTime();

        IReadOnlyList<JobSchedule> GetJobSchedules();
        bool AddJobSchedule(JobSchedule schedule);

        /// <summary>
        /// Really just for display purposes - should not be used for firing jobs!
        /// </summary>
        IReadOnlyList<JobScheduleInstance> GetJobScheduleInstances();
    }

    public class SqlServerJobRepository : IJobScheduleRepository
    {
        public const string QueueId = "JobsQueue";

        public DbClient Client { get; protected set; }

        public SqlServerJobRepository(DbConnection conn)
        {
            Client = new DbClient(conn);
        }

        public bool Enqueue(JobScheduleInstance instance)
        {
            var cmd = Client.CreateStoredProcedureCommand("EnqueueJobInstanceQueue");

            cmd.Parameters.Add(new SqlParameter("jobId", instance.JobScheduleId));
            cmd.Parameters.Add(new SqlParameter("targetTime", instance.TargetTime));

            var cnt = Client.ExecuteCommand(cmd);

            if (cnt > 0)
            {
                return true;
            }

            Alerts.Error("JobScheduleInstance.Enqueue Failed");

            return false;
        }
        public JobScheduleInstance Dequeue()
        {
            var cmd = Client.CreateStoredProcedureCommand("DequeueJobInstanceQueue");

            try
            {
                cmd.Parameters.Add(new SqlParameter("lockedBy", Util.GetMachineName()));
                var pInstanceId = new SqlParameter("instanceId", SqlDbType.BigInt)
                                  {
                                      Direction = ParameterDirection.Output
                                  };
                cmd.Parameters.Add(pInstanceId);
                var pJobId = new SqlParameter("jobId", SqlDbType.NChar, 50)
                             {
                                 Direction = ParameterDirection.Output
                             };
                cmd.Parameters.Add(pJobId);
                var pTargetTime = new SqlParameter("targetTime", SqlDbType.DateTime)
                                  {
                                      Direction = ParameterDirection.Output
                                  };
                cmd.Parameters.Add(pTargetTime);

                var cnt = Client.ExecuteCommand(cmd);

                if (cnt == 0)
                {
                    // Okay, nothing to dequeue then

                    return null;
                }
                else if (cnt < 0)
                {
                    Alerts.Error("JobScheduleInstance.Dequeue Failed");

                    return null;
                }

                var instance = new JobScheduleInstance()
                               {
                                   InstanceId = Convert.ToInt32(pInstanceId.Value),
                                   JobScheduleId = (string) pJobId.Value,
                                   TargetTime = Convert.ToDateTime(pTargetTime.Value)
                               };

                return instance;
            }
            finally
            {
                cmd.Dispose();
            }
        }
        public bool Complete(JobScheduleInstance instance)
        {
            var cmd = Client.CreateStoredProcedureCommand("CompleteJobInstanceQueue");

            var pInstanceId = new SqlParameter("instanceId", SqlDbType.BigInt)
            {
                Value = instance.InstanceId
            };
            cmd.Parameters.Add(pInstanceId);
            var pStatus = new SqlParameter("instanceId", SqlDbType.Int)
            {
                Value = (int)instance.Status
            };
            cmd.Parameters.Add(pStatus);
            var pJobId = new SqlParameter("jobId", SqlDbType.NChar, 50)
            {
                Value = instance.JobScheduleId
            };
            cmd.Parameters.Add(pJobId);
            
            var cnt = Client.ExecuteCommand(cmd);

            if (cnt > 0)
            {
                return true;
            }

            Alerts.Error("JobScheduleInstance.Complete Failed");

            return false;
        }
        public DateTime? GetNextInstanceTime()
        {
            var cmd = Client.CreateStoredProcedureCommand("GetNextJobInstanceId");

            var pInstanceId = new SqlParameter("instanceId", SqlDbType.BigInt)
                               {
                                   Direction = ParameterDirection.Output
                               };
            cmd.Parameters.Add(pInstanceId);
            var pJobId = new SqlParameter("jobId", SqlDbType.NChar, 50)
                               {
                                   Direction = ParameterDirection.Output
                               };
            cmd.Parameters.Add(pJobId);
            var pTargetTime = new SqlParameter("targetTime", SqlDbType.DateTime)
                               {
                                   Direction = ParameterDirection.Output
                               };
            cmd.Parameters.Add(pTargetTime);

            var cnt = Client.ExecuteCommand(cmd);

            if (cnt == 0)
            {
                // Okay, nothing to dequeue then

                return null;
            }
            else if (cnt < 0)
            {
                Alerts.Error("JobScheduleInstance.GetNextInstanceTime Failed");

                return null;
            }

            return Convert.ToDateTime(pTargetTime.Value);
        }

        public IReadOnlyList<JobScheduleInstance> GetJobScheduleInstances()
        {
            try
            {
                var cmd = Client.CreateCommand("select * from JobInstanceQueue where TargetTime >= @startTime and TargetTime <= @endTime");

                cmd.Parameters.Add(new SqlParameter("startTime", DateTime.Today.ToUniversalTime()));
                cmd.Parameters.Add(new SqlParameter("endTime", DateTime.Today.AddDays(1).ToUniversalTime()));

                var reader = cmd.ExecuteReader();

                var list = new List<JobScheduleInstance>();

                while (reader.Read())
                {
                    var instance = new JobScheduleInstance()
                    {
                        InstanceId = Convert.ToInt32(reader["Id"]),
                        JobScheduleId = (string)reader["JobId"],
                        TargetTime = Convert.ToDateTime(reader["TargetTime"])
                    };

                    list.Add(instance);
                }

                reader.Close();

                return list;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "Exception Querying JobScheduleInstances: ", false);
                return null;
            }
        } 

        public IReadOnlyList<JobSchedule> GetJobSchedules()
        {
            string sql = "select s.*, sf.Frequency from JobSchedules s left join JobScheduleFrequency sf on s.FrequencyId = sf.Id";

            var reader = Client.ExecuteReader(sql);

            if (reader == null)
            {
                return null;
            }

            try
            {
                var list = new List<JobSchedule>();

                while (reader.Read())
                {
                    var schedule = new JobSchedule()
                                   {
                                       Guid = reader["Id"].ToString(),
                                       JobId = reader["JobId"].ToString(),
                                   };



                    schedule.TargetFrequency = (JobScheduleFrequencyType) Enum.Parse(typeof (JobScheduleFrequencyType), reader.GetString(reader.GetOrdinal("Frequency")));
                    var targetTime = reader["TargetTime"];
                    schedule.TargetTime = targetTime.ToString();

                    list.Add(schedule);
                }

                return list;
            }
            finally
            {
                reader.Dispose();
            }
        }
        public bool AddJobSchedule(JobSchedule schedule)
        {
            return false;
        }
    }
}

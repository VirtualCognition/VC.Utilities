using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC.DB
{

    /// <summary>
    /// A queue of string ids.  
    /// </summary>
    public class DbQueue : IDisposable
    {
        protected DbConnection Connection { get; set; }
        public string QueueId { get; set; }

        public bool IsInitialized { get; protected set; }
        public bool IsConnected
        {
            get
            {
                if (Connection == null)
                {
                    return false;
                }
                else if (Connection.State.IsConnected() != true)
                {
                    return false;
                }
                return true;
            }
        }

        public DbQueue(string id, DbConnection conn)
        {
            if (String.IsNullOrWhiteSpace(id) || id.Length > 10)
            {
                throw new ArgumentException("Invalid Queue Id.  Must be > 0 < 10 characters.");
            }

            QueueId = id;

            if (conn == null || !conn.State.IsConnected() == true)
            {
                throw new ArgumentException();
            }

            Connection = conn;
        }

        public void Dispose()
        {
            Connection = null;
        }

        public bool Initialize()
        {
            if (IsInitialized)
            {
                return true;
            }
            
            IsInitialized = true;

            return true;
        }

        public int GetQueuedTaskCount()
        {
            if (!Initialize())
            {
                Util.Msg("Failed initialization to enqueue task!");
                return -1;
            }

            using (var client = new DbClient(Connection))
            {
                return (int) client.ExecuteScalar("select count(*) from TaskQueue where QueueId = '" + QueueId + "' and Status <> 2");
            }
        }

        public List<string> GetQueuedTaskIds()
        {
            if (!Initialize())
            {
                Util.Msg("Failed initialization to retrieve queued tasks!");
                return null;
            }

            using (var client = new DbClient(Connection))
            {
                string sql = "select TaskId from TaskQueue where QueueId = '" + QueueId + "' and Status <> 2";

                var reader = client.ExecuteReader(sql);

                if (reader == null)
                {
                    return null;
                }

                List<string> ids = new List<string>();

                while (reader.Read())
                {
                    var id = reader.GetString(reader.GetOrdinal("taskid"));

                    ids.Add(id);
                }

                return ids;
            }
        }

        public bool EnqueueTask(string taskId)
        {
            if (!Initialize())
            {
                Util.Msg("Failed initialization to enqueue task!");
                return false;
            }

            if (String.IsNullOrWhiteSpace(taskId))
            {
                throw new ArgumentNullException();
            }

            if (!IsConnected)
            {
                Util.BreakDebug();
                return false;
            }

            using (var client = new DbClient(Connection))
            {
                try
                {
                    var cmd = client.CreateStoredProcedureCommand("EnqueueTaskQueue");

                    cmd.Parameters.Add(new SqlParameter("@queueId", QueueId));
                    cmd.Parameters.Add(new SqlParameter("@taskId", taskId));

                    return client.ExecuteCommand(cmd) > 0;
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException(ex, "Failed Task Queue: ", true);

                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskId">Null if nothing was dequeued, otherwise the guid of the dequeued task.</param>
        /// <returns>True if the procedure was called successfully, regardless if any tasks were dequeued.</returns>
        public bool DequeueTask(out string taskId)
        {
            taskId = null;

            if (!IsConnected)
            {
                Util.BreakDebug();
                return false;
            }

            using (var client = new DbClient(Connection))
            {
                try
                {
                    var cmd = client.CreateStoredProcedureCommand("DequeueTaskQueue");
                    
                    cmd.Parameters.Add(new SqlParameter("@LockedBy", Util.GetMachineName()));

                    var taskIdParam = new SqlParameter("@taskId", SqlDbType.NChar, 50)
                                      {
                                          Direction = ParameterDirection.Output
                                      };

                    cmd.Parameters.Add(taskIdParam);

                    int cnt = client.ExecuteCommand(cmd);

                    taskId = Util.Trim(taskIdParam.Value as string);

                    if (cnt <= 0)
                    {
                        // That's fine
                        return true;
                    }

                    if (!string.IsNullOrWhiteSpace(taskId))
                    {
                        return true;
                    }

                    // So how'd that happen?
                    Util.BreakDebug();
                    return false;
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException(ex, "Failed Task Dequeue: ", true);

                    return false;
                }
            }
        }

        public bool RemoveFailedTask(string taskId)
        {
            Alerts.Warning("TaskQueue: Removing failed task: " + taskId);

            return CompleteTask(taskId);
        }

        // TODO: Ensure a task hasn't been requeued in the meantime
        public bool CompleteTask(string taskId)
        {
            if (String.IsNullOrWhiteSpace(taskId))
            {
                throw new ArgumentNullException();
            }

            if (!IsConnected)
            {
                Util.BreakDebug();
                return false;
            }

            using (var client = new DbClient(Connection))
            {
                try
                {
                    var cmd = client.CreateStoredProcedureCommand("CompleteTaskQueue");

                    cmd.Parameters.Add(new SqlParameter("@taskId", taskId));

                    cmd.ExecuteNonQuery();

                    return true;
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException(ex, "Failed Task Queue: ", true);

                    return false;
                }
            }
        }
    }
}

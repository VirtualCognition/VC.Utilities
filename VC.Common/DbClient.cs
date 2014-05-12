using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VC.DB
{
    public static class DataExtensions
    {
        public static bool? IsConnected(this System.Data.ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Open:
                case ConnectionState.Fetching:
                case ConnectionState.Executing:
                    return true;
                case ConnectionState.Closed:
                case ConnectionState.Broken:
                    return false;
                case ConnectionState.Connecting:
                default:
                    return null;
            }
        }
    }

    public class DbClient : IDisposable
    {
        public const int DefaultWarningTimeMs = 1000;

        private readonly bool _isConnectionOwned;
        private readonly string _connectionString;
        private DbConnection _connection;

        public string ConnectionString
        {
            get { return _connectionString; }
        }
        public DbConnection Connection
        {
            get { return _connection; }
        }
        public bool IsConnected
        {
            get { return _connection != null && _connection.State.IsConnected() == true; }
        }

        protected DbClient()
        {
            
        }
        public DbClient(string connectionString)
            : this()
        {
            _connectionString = connectionString;
            _isConnectionOwned = true;
        }
        public DbClient(DbConnection conn)
            : this()
        {
            _connection = conn;
            _isConnectionOwned = false;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_isConnectionOwned)
                {
                    _connection.Dispose();
                }
                _connection = null;
            }
        }

        public void Connect()
        {
            if (!_isConnectionOwned)
            {
                throw new InvalidOperationException("Connect Invalid On Existing Connection");
            }

            _connection.Open();
        }

        protected DbCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }

        public DbCommand CreateCommand(string query)
        {
            DbCommand command = CreateCommand();

            command.CommandText = query;

            return command;
        }

        protected void HandleException(string query, Exception ex)
        {
            Util.BreakDebug();

            ExceptionHandler.HandleException(ex, "DbClient Query Exception: ", false);
        }

        protected void PostQuery(string query, double timeMs)
        {
            if (timeMs > DefaultWarningTimeMs)
            {
                Util.Msg("Slow DbClient Query: {0} / {1}", timeMs, query);
                Alerts.Warning("Slow DbClient Query: ", timeMs);
            }
        }

        public DbDataReader ExecuteReader(string query)
        {
            if (!IsConnected)
            {
                return null;
            }

            var timestamp = DateTime.UtcNow;

            var cmd = CreateCommand(query);

            DbDataReader reader = null;

            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                HandleException(query, ex);

                return null;
            }

            var elapMs = DateTime.UtcNow.Subtract(timestamp).TotalMilliseconds;

            PostQuery(query, elapMs);

            return reader;
        }
        public int ExecuteNonQuery(string query)
        {
            if (!IsConnected)
            {
                return -1;
            }

            var timestamp = DateTime.UtcNow;

            var cmd = CreateCommand(query);

            int n = -1;

            try
            {
                n = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                HandleException(query, ex);

                throw;
            }

            var elapMs = DateTime.UtcNow.Subtract(timestamp).TotalMilliseconds;

            PostQuery(query, elapMs);

            return n;
        }
        public object ExecuteScalar(string query)
        {
            if (!IsConnected)
            {
                return -1;
            }

            var timestamp = DateTime.UtcNow;

            var cmd = CreateCommand(query);

            object ret = null;

            try
            {
                ret = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                HandleException(query, ex);

                throw;
            }

            var elapMs = DateTime.UtcNow.Subtract(timestamp).TotalMilliseconds;

            PostQuery(query, elapMs);

            return ret;
        }

        public DbCommand CreateStoredProcedureCommand(string spName)
        {
            var cmd = CreateCommand(spName);

            cmd.CommandType = CommandType.StoredProcedure;

            return cmd;
        }

        public int ExecuteCommand(DbCommand cmd)
        {
            var timestamp = DateTime.UtcNow;

            int n = -1;

            try
            {
                n = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                HandleException(cmd.CommandText, ex);

                throw;
            }

            var elapMs = DateTime.UtcNow.Subtract(timestamp).TotalMilliseconds;

            PostQuery(cmd.CommandText, elapMs);

            return n;
        }

        protected class DbObjectColumn
        {
            public PropertyInfo Property { get; set; }
            public bool IsPrimary { get; set; }
            public string ColumnName { get; set; }
        }

        protected bool ProcessDbObjectType<T>(out string tableName, out List<DbObjectColumn> columns)
        {
            var aTable = Util.GetCustomAttribute<TableAttribute, T>();

            if (aTable == null)
            {
                throw new ArgumentException("InsertObjects supported only for types with an explicit TableAttribute specified");
            }

            tableName = string.IsNullOrWhiteSpace(aTable.Name) ? typeof(T).Name : aTable.Name;

            columns = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new DbObjectColumn()
                {
                    Property = p,
                    IsPrimary = Util.GetCustomAttribute<KeyAttribute>(p) != null,
                    ColumnName = p.Name,
                })
                .ToList();

            if (columns.Count == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Row by row upsert
        /// </summary>
        public bool UpsertObjects<T>(IReadOnlyList<T> list)
        {
            string tableName;
            List<DbObjectColumn> columns;

            if (!ProcessDbObjectType<T>(out tableName, out columns))
            {
                throw new ArgumentException("Invalid Type For UpsertObjects");
            }

            var keys = columns.Where(a => a.IsPrimary).ToList();

            if (keys.Count == 0)
            {
                throw new ArgumentException("InsertObjects supported only for types with an explicit key specified by KeyAttribute");
            }
            
            var sqlSourceArgs = String.Join(", ", columns.Select(c => "@" + c.ColumnName));
            var sqlSourceNames = String.Join(", ", columns.Select(c => c.ColumnName));
            var sqlKeyClause = String.Join(" and ", keys.Select(c => string.Format("target.{0} = source.{0}", c.ColumnName)));
            var sqlSetClause = String.Join(", ", columns.Where(c => !c.IsPrimary).Select(c => string.Format("{0} = source.{0}", c.ColumnName)));
            var sqlValueNames = String.Join(", ", columns.Select(c => "source." + c.ColumnName));
            
            bool success = true;

            foreach (var obj in list)
            {
                var cmd = CreateCommand();

                cmd.CommandText = string.Format(
                    "MERGE {0} as target " +
                    " USING (select {1}) as source ({2}) on ({3}) " +
                    " when matched then update set {4} " +
                    " when not matched then insert ({5}) values ({6}) ; ",
                    tableName,
                    sqlSourceArgs,
                    sqlSourceNames,
                    sqlKeyClause,
                    sqlSetClause,
                    sqlSourceNames,
                    sqlValueNames);

                foreach (var c in columns)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = "@" + c.ColumnName;
                    param.Value = c.Property.GetValue(obj);
                    cmd.Parameters.Add(param);
                }

                int cnt = cmd.ExecuteNonQuery();

                if (cnt <= 0)
                {
                    success = false;
                }
            }

            return success;
        }

        public List<T> GetObjects<T>(string whereClause = null)
            where T:new()
        {
            string tableName;
            List<DbObjectColumn> columns;

            if (!ProcessDbObjectType<T>(out tableName, out columns))
            {
                throw new ArgumentException("Invalid Type For GetObjects");
            }

            var list = new List<T>();

            var sql = string.Format(
                " select * from {0} ",
                tableName);

            var reader = ExecuteReader(sql);

            try
            {
                foreach (var c in columns)
                {
                    // Check for the column first? Cache ordinals?
                }

                while (reader.Read())
                {
                    var obj = new T();

                    foreach (var c in columns)
                    {
                        if (!c.Property.CanWrite) continue;

                        var ord = reader.GetOrdinal(c.ColumnName);

                        if (ord < 0)
                        {
                            throw new IndexOutOfRangeException("Column Does Not Exist: " + c.ColumnName);
                        }

                        if (reader.IsDBNull(ord))
                        {
                            // We'll just leave as the default
                            continue;
                        }

                        var val = reader.GetValue(ord);

                        c.Property.SetValue(obj, val);
                    }

                    list.Add(obj);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, string.Format("Exception Parsing GetObject<{0}>:", typeof(T).Name), false);
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        } 
    }

    public interface IReadOnlyRepository<T, K> 
        where T:IHasKey<K>
    {
        int Count { get; }
        bool Contains(K key);
        T Get(K key);
        IReadOnlyList<T> Get();
    }

    public interface IRepository<T, K> : IReadOnlyRepository<T, K>
        where T:IHasKey<K>
    {
        void Add(T entity);
        void Remote(K key);
        void Update(T entity);
    }

    [Table("TBL_TEST_ENTITY")]
    public class TestEntity : IHasId
    {
        [Key()]
        public string Id { get; set; }
        [Column("VALUE_1")]
        public double Value { get; set; }

        public static string[] existingGuids = new string[]
            {
                "A47BE632D6BA4947ACFE1E91F406B21A",
                "A47BE632D6BA4947ACFE1E91F406B21B",
                "A47BE632D6BA4947ACFE1E91F406B21C",
            };

        public static void Test(DbConnection conn)
        {
            var list = new List<TestEntity>();

            foreach (var guid in existingGuids)
            {
                list.Add(new TestEntity { Id = guid, Value = Util.Rand() });
            }

            list.Add(new TestEntity { Id = Guid.NewGuid().ToString("N"), Value = Util.Rand() });
            list.Add(new TestEntity { Id = Guid.NewGuid().ToString("N"), Value = Util.Rand() });

            var client = new DbClient(conn);

            if (!client.UpsertObjects(list))
            {
                Util.Msg("Insert failed!");
                return;
            }

            Util.Msg("Test Successful!");
        }

        public static List<TestEntity> TestGet(DbConnection conn)
        {
            var client = new DbClient(conn);

            return client.GetObjects<TestEntity>();
        } 
    }
}

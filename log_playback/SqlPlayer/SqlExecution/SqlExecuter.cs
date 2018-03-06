using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace SqlPlayer
{
    public abstract class SqlExecuter : IDisposable
    {
        protected DbProviderFactory _fact;
        protected DbConnection _conn;
        
        protected abstract DbProviderFactory GetFactory();
        public SqlExecuter()
        {
            this._fact = this.GetFactory();
        }
        //public SqlExecuter(string connectionString = null, bool openConnection = false)
        //{
        //    this._fact = this.GetFactory();
        //    if (!string.IsNullOrEmpty(connectionString) && openConnection)
        //    {
        //        this.OpenConnection(connectionString);
        //    }
        //}
        public void OpenConnection(string connectionString)
        {
            this._conn = this._fact.CreateConnection();
            this._conn.ConnectionString = connectionString;
            this._conn.Open();
        }
        private static Dictionary<string, Type> _byProvider = null;
        public static SqlExecuter Create(string connectionString, string provider)
        {
            if (_byProvider == null)
            {
                var types = typeof(SqlExecuter).Assembly.GetTypes().Where(_ => _.IsSubclassOf(typeof(SqlExecuter)));
                _byProvider = types.Select(_ => (SqlExecuter)_.GetConstructor(new Type[] { }).Invoke(new object[] { }))
                    .Select(_ => new { Type = _.GetType(), ProviderName = _.GetFactory().GetType().Namespace })
                    .ToDictionary(_ => (string)_.ProviderName, _ => _.Type);
            }
            if (_byProvider.TryGetValue(provider, out Type type))
            {
                var result = (SqlExecuter)type
                    .GetConstructor(new Type[] { })
                    .Invoke(new object[] {  });
                result.OpenConnection(connectionString);
                return result;
            }
            throw new Exception("No provider available for " + provider);
        }

        private Regex rxBase64 = new Regex(@"(##BASE64\:)([\w/=\+]*)");
        private Regex rxFile = new Regex(@"(##FILE\:)(.+)(?=\n)");
        private Regex rxServer = new Regex(@"(INFORMATION_SCHEMA|sysdatabases|use master)", RegexOptions.IgnoreCase);
        private Regex rxInsert = new Regex(@"^INSERT INTO", RegexOptions.IgnoreCase);
        public async Task Execute(string sql, System.Diagnostics.Stopwatch stopwatch = null)
        {
            var cmd = this._fact.CreateCommand();
            cmd.Connection = this._conn;

            DbParameter AddBinaryParameter(byte[] value)
            {
                var p = _fact.CreateParameter();
                p.DbType = DbType.Binary;
                p.ParameterName = "@p" + cmd.Parameters.Count;
                p.Value = value;
                cmd.Parameters.Add(p);
                return p;
            }

            sql = rxBase64.Replace(sql, m => {
                try
                {
                    var str = m.Groups[2].Value.Trim();
                    var val = Convert.FromBase64String(str);
                    return AddBinaryParameter(val).ParameterName;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });

            sql = rxFile.Replace(sql, m => {
                var path = m.Groups[2].Value.Trim();
                if (!System.IO.File.Exists(path))
                    throw new System.IO.FileNotFoundException(path);
                try
                {
                    var val = System.IO.File.ReadAllBytes(path);
                    return AddBinaryParameter(val).ParameterName;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            if (rxServer.IsMatch(sql))
            {
                //Needs master/server connection
            }
            else if (rxInsert.IsMatch(sql))
            {
                //Avoid inserts for now (to avoid polluting the DB")
            }
            else
            {
                cmd.CommandText = sql;
                try
                {
                    if (stopwatch != null)
                        stopwatch.Start();

                    await cmd.ExecuteNonQueryAsync();

                    if (stopwatch != null)
                        stopwatch.Stop();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            cmd.Dispose();
        }

        public void Dispose()
        {
            if (this._conn != null)
            {
                this._conn.Close();
                this._conn.Dispose();
                this._conn = null;
            }
        }
    }

}

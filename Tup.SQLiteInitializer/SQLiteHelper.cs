using System;
using System.Data;
using System.Data.Common;

namespace Tup.SQLiteInitializer
{
    /// <summary>
    /// SQLite 数据库操作
    /// </summary>
    public class SQLiteHelper : IDisposable
    {
        private static readonly object lockDBHelper = new object();
        /// <summary>
        /// 连接数据库字符串
        /// </summary>
        private static string s_DefaultConnectionString = string.Format(@"Data Source={0}\Test.db", AppDomain.CurrentDomain.BaseDirectory);

        /// <summary>
        /// 数据库连接对象
        /// </summary>
        private DbConnection objConnection;
        /// <summary>
        /// 数据库连接对象
        /// </summary>
        public DbConnection DbConnection
        {
            get { return objConnection; }
        }

        /// <summary>
        /// 执行命令对象
        /// </summary>
        private DbCommand objCommand;
        /// <summary>
        /// 创建提供程序工厂
        /// </summary>
        private DbProviderFactory objFactory = null;

        ///// <summary>
        /////
        ///// </summary>
        //public SQLiteHelper()
        //    : this(s_DefaultConnectionString)
        //{
        //}
        /// <summary>
        ///
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="connectionString"></param>
        public SQLiteHelper(string connectionString)
        {
            objFactory = System.Data.SQLite.SQLiteFactory.Instance;

            objConnection = objFactory.CreateConnection();
            objCommand = objFactory.CreateCommand();

            objConnection.ConnectionString = connectionString;
            objCommand.Connection = objConnection;
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="name">参数名称.</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public int AddParameter(string name, object value)
        {
            DbParameter p = objFactory.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            p.Size = 1000;
            return objCommand.Parameters.Add(p);
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="objCommand">持有的事务</param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int AddParameter(ref DbCommand objCommand, string name, object value)
        {
            DbParameter p = objCommand.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            return objCommand.Parameters.Add(p);
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="parameter">参数对象.</param>
        /// <returns></returns>
        public int AddParameter(DbParameter parameter)
        {
            return objCommand.Parameters.Add(parameter);
        }

        /// <summary>
        /// 执行命令对象
        /// </summary>
        /// <value>
        /// The command.
        /// </value>
        public DbCommand Command
        {
            get
            {
                return objCommand;
            }
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        public void BeginTransaction()
        {
            if (objConnection.State == System.Data.ConnectionState.Closed)
            {
                objConnection.Open();
            }
            objCommand.Transaction = objConnection.BeginTransaction();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void CommitTransaction()
        {
            objCommand.Transaction.Commit();
            objConnection.Close();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void RollbackTransaction()
        {
            objCommand.Transaction.Rollback();
            objConnection.Close();
        }

        /// <summary>
        /// 执行不返回数据行的SQL语句
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string query)
        {
            return ExecuteNonQuery(query, CommandType.Text, ConnectionState.CloseOnExit);
        }

        /// <summary>
        /// 执行不返回数据行的SQL语句
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="commandtype">连接保持打开或执行完关闭</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string query, CommandType commandtype)
        {
            return ExecuteNonQuery(query, commandtype, ConnectionState.CloseOnExit);
        }

        /// <summary>
        /// 执行不返回数据行的SQL语句
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="connectionstate">连接保持打开或执行完关闭</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string query, ConnectionState connectionstate)
        {
            return ExecuteNonQuery(query, CommandType.Text, connectionstate);
        }

        /// <summary>
        /// 执行不返回数据行的SQL语句
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="commandtype">表示使用SQL或储存过程</param>
        /// <param name="connectionstate">连接保持打开或执行完关闭</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string query, CommandType commandtype, ConnectionState connectionstate)
        {
            lock (lockDBHelper)
            {
                objCommand.CommandText = query;
                objCommand.CommandType = commandtype;
                int i = -1;
                try
                {
                    if (objConnection.State == System.Data.ConnectionState.Closed)
                    {
                        objConnection.Open();
                    }
                    i = objCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    HandleExceptions(ex);
                }
                finally
                {
                    objCommand.Parameters.Clear();
                    if (connectionstate == ConnectionState.CloseOnExit)
                    {
                        objConnection.Close();
                    }
                }

                return i;
            }
        }

        /// <summary>
        /// 返回单个结果
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <returns></returns>
        public object ExecuteScalar(string query)
        {
            return ExecuteScalar(query, CommandType.Text, ConnectionState.CloseOnExit);
        }

        /// <summary>
        /// 返回单个结果
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="commandtype">表示使用SQL或储存过程</param>
        /// <returns></returns>
        public object ExecuteScalar(string query, CommandType commandtype)
        {
            return ExecuteScalar(query, commandtype, ConnectionState.CloseOnExit);
        }

        /// <summary>
        /// 返回单个结果
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="connectionstate">连接保持打开或执行完关闭</param>
        /// <returns></returns>
        public object ExecuteScalar(string query, ConnectionState connectionstate)
        {
            return ExecuteScalar(query, CommandType.Text, connectionstate);
        }

        /// <summary>
        /// 返回单个结果（第一行第一列）
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="commandtype">表示使用SQL或储存过程</param>
        /// <param name="connectionstate">连接保持打开或执行完关闭</param>
        /// <returns></returns>
        public object ExecuteScalar(string query, CommandType commandtype, ConnectionState connectionstate)
        {
            objCommand.CommandText = query;
            objCommand.CommandType = commandtype;
            object o = null;
            try
            {
                if (objConnection.State == System.Data.ConnectionState.Closed)
                {
                    objConnection.Open();
                }
                o = objCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
            }
            finally
            {
                objCommand.Parameters.Clear();
                if (connectionstate == ConnectionState.CloseOnExit)
                {
                    objConnection.Close();
                }
            }

            return o;
        }

        /// <summary>
        /// 返回数据读取器
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(string query)
        {
            return ExecuteReader(query, CommandType.Text, ConnectionState.CloseOnExit);
        }

        /// <summary>
        /// 返回数据读取器
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="commandtype">表示使用SQL或储存过程</param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(string query, CommandType commandtype)
        {
            return ExecuteReader(query, commandtype, ConnectionState.CloseOnExit);
        }

        /// <summary>
        /// 返回数据读取器
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="connectionstate">表示使用SQL或储存过程</param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(string query, ConnectionState connectionstate)
        {
            return ExecuteReader(query, CommandType.Text, connectionstate);
        }

        /// <summary>
        /// 返回数据读取器
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="commandtype">表示使用SQL或储存过程</param>
        /// <param name="connectionstate">连接保持打开或执行完关闭</param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(string query, CommandType commandtype, ConnectionState connectionstate)
        {
            objCommand.CommandText = query;
            objCommand.CommandType = commandtype;
            DbDataReader reader = null;
            try
            {
                if (objConnection.State == System.Data.ConnectionState.Closed)
                {
                    objConnection.Open();
                }
                if (connectionstate == ConnectionState.CloseOnExit)
                {
                    reader = objCommand.ExecuteReader(CommandBehavior.CloseConnection);
                }
                else
                {
                    reader = objCommand.ExecuteReader();
                }
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
            }
            finally
            {
                objCommand.Parameters.Clear();
            }

            return reader;
        }

        /// <summary>
        /// 返回Dataset
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string query)
        {
            return ExecuteDataSet(query, CommandType.Text, ConnectionState.CloseOnExit);
        }

        /// <summary>
        /// 返回Dataset
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="commandtype">表示使用SQL或储存过程</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string query, CommandType commandtype)
        {
            return ExecuteDataSet(query, commandtype, ConnectionState.CloseOnExit);
        }

        /// <summary>
        /// 返回Dataset
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="connectionstate">表示使用SQL或储存过程</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string query, ConnectionState connectionstate)
        {
            return ExecuteDataSet(query, CommandType.Text, connectionstate);
        }

        /// <summary>
        /// 返回Dataset
        /// </summary>
        /// <param name="query">SQL语句</param>
        /// <param name="commandtype">表示使用SQL或储存过程</param>
        /// <param name="connectionstate">连接保持打开或执行完关闭</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string query, CommandType commandtype, ConnectionState connectionstate)
        {
            DbDataAdapter adapter = objFactory.CreateDataAdapter();
            objCommand.CommandText = query;
            objCommand.CommandType = commandtype;
            adapter.SelectCommand = objCommand;
            DataSet ds = new DataSet();
            try
            {
                adapter.Fill(ds);
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
            }
            finally
            {
                objCommand.Parameters.Clear();
                if (connectionstate == ConnectionState.CloseOnExit)
                {
                    if (objConnection.State == System.Data.ConnectionState.Open)
                    {
                        objConnection.Close();
                    }
                }
            }
            return ds;
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <exception cref="System.Exception"></exception>
        private void HandleExceptions(Exception ex)
        {
            //LogHelper.Error(string.Format("操作数据库错误：{0}。{1}", ex.Message, ex.ToString()));
            throw new Exception(ex.Message, ex);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            objConnection.Close();
            objConnection.Dispose();
            objCommand.Dispose();
        }
    }

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// 表示，数据库连接不会自动关闭，需要手动关闭。提交事务或回滚事务或调Dispose
        /// </summary>
        KeepOpen,

        /// <summary>
        /// 表示，一个方法执行完了，数据库连接关闭。
        /// </summary>
        CloseOnExit
    }
}

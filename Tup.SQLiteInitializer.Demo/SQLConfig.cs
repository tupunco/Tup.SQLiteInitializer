using System;

namespace Tup.SQLiteInitializer.Demo
{
    /// <summary>
    /// SQLite Config
    /// </summary>
    internal static class SQLConfig
    {
        /// <summary>
        /// 数据库连接 加密密码
        /// </summary>
        public static string DBConnectionKey = "123456";

        /// <summary>
        /// 数据库连接 字符串
        /// </summary>
        /// <remarks>
        /// FailIfMissing=false 数据不存在自动创建
        /// Pooling=true 使用连接池
        /// Data Source={0};Pooling=true;FailIfMissing=false Password=123456;
        /// </remarks>
        public static string ConnectionString = string.Format(@"Data Source={0}\DemoTest.db;Password={1};", AppDomain.CurrentDomain.BaseDirectory, DBConnectionKey);

        /// <summary>
        /// 数据库连接-非加密字符串
        /// </summary>
        public static readonly string NormalConnectionString = string.Format(@"Data Source={0}\DemoTest.db", AppDomain.CurrentDomain.BaseDirectory);
    }
}
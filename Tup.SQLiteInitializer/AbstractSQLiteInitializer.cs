using Tup.SQLiteInitializer.Impl;

namespace Tup.SQLiteInitializer
{
    /// <summary>
    /// 抽象 SQLite 数据库初始化器
    /// </summary>
    public abstract class AbstractSQLiteInitializer : ISQLiteInitializer
    {
        /// <summary>
        /// 数据库映射结构
        /// </summary>
        private static SQLiteMapping SQLiteMapping = new SQLiteMapping();

        /// <summary>
        /// 当前数据库版本号
        /// </summary>
        public abstract int NewVersion { get; }
        /// <summary>
        /// Default ConnectionString
        /// </summary>
        public abstract string DefaultConnectionString { get; }

        protected void ToMapping<TMap>()
        {
            SQLiteMapping.ToMapping<TMap>();
        }

        #region OnDoLegacy
        /// <summary>
        /// 处理遗留版本动作
        /// </summary>
        void ISQLiteInitializer.OnDoLegacy()
        {
            OnDoLegacy();
        }
        /// <summary>
        /// 处理遗留版本动作
        /// </summary>
        protected abstract void OnDoLegacy();
        #endregion

        #region OnCreate
        /// <summary>
        /// 当前数据库版本号
        /// </summary>
        /// <param name="db"></param>
        /// <param name="currentVersion"></param>
        void ISQLiteInitializer.OnCreate(SQLiteHelper db, int currentVersion)
        {
            SQLiteMapping.SQLiteDB = db;
            SQLiteMapping.CreateAllTable();

            OnCreate(db, currentVersion);
        }
        /// <summary>
        /// 当前数据库版本号
        /// </summary>
        /// <param name="db"></param>
        /// <param name="currentVersion"></param>
        protected abstract void OnCreate(SQLiteHelper db, int currentVersion);
        #endregion

        #region OnUpgrade
        /// <summary>
        /// 升级数据库时动作
        /// </summary>
        /// <param name="db"></param>
        /// <param name="oldVersion"></param>
        /// <param name="newVersion"></param>
        void ISQLiteInitializer.OnUpgrade(SQLiteHelper db, int oldVersion, int newVersion)
        {
            //INFO: 数据库映射本身会进行对缺少的 "表/列" 迁移升级. 但是不会对索引进行迁移, 需要手动操作
            SQLiteMapping.SQLiteDB = db;
            SQLiteMapping.CreateAllTable();

            //INFO: 所有数据库操作请加 ConnectionState.KeepOpen 设置
            OnUpgrade(db, oldVersion, newVersion);
        }
        /// <summary>
        /// 升级数据库时动作
        /// </summary>
        /// <param name="db"></param>
        /// <param name="oldVersion"></param>
        /// <param name="newVersion"></param>
        protected abstract void OnUpgrade(SQLiteHelper db, int oldVersion, int newVersion);
        #endregion

        #region Utils
        /// <summary>
        /// Drop Index IfExists
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        protected int DropIndexIfExists(string indexName)
        {
            return SQLiteMapping.DropIndexIfExists(indexName);
        }
        /// <summary>
        /// Execute SQL
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected int Execute(string sql)
        {
            return SQLiteMapping.Execute(sql);
        }
        #endregion
    }
}

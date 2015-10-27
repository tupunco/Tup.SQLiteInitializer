namespace Tup.SQLiteInitializer
{
    /// <summary>
    /// 此接口的实现用于在首次使用 SQLite 实例时初始化基础数据库
    /// </summary>
    public interface ISQLiteInitializer
    {
        /// <summary>
        /// DefaultConnectionString
        /// </summary>
        string DefaultConnectionString { get; }

        /// <summary>
        /// 当前数据库版本号
        /// number of the database (starting at 1); if the database is older,
        ///    {@link #onUpgrade} will be used to upgrade the database; if the database is
        ///    newer, {@link #onDowngrade} will be used to downgrade the database
        /// </summary>
        int NewVersion { get; }

        /// <summary>
        /// 处理遗留版本动作
        /// </summary>
        void OnDoLegacy();

        /// <summary>
        /// 创建数据时动作
        /// Called when the database is created for the first time. This is where the
        /// creation of tables and the initial population of the tables should happen.
        /// </summary>
        /// <param name="db">The database.</param>
        void OnCreate(SQLiteHelper db, int currentVersion);

        /// <summary>
        /// 升级数据库时动作
        /// Called when the database needs to be upgraded. The implementation
        /// should use this method to drop tables, add tables, or do anything else it
        /// needs to upgrade to the new schema version.
        ///
        /// <p>
        /// The SQLite ALTER TABLE documentation can be found
        /// <a href="http://sqlite.org/lang_altertable.html">here</a>. If you add new columns
        /// you can use ALTER TABLE to insert them into a live table. If you rename or remove columns
        /// you can use ALTER TABLE to rename the old table, then create the new table and then
        /// populate the new table with the contents of the old table.
        /// </p><p>
        /// This method executes within a transaction.  If an exception is thrown, all changes
        /// will automatically be rolled back.
        /// </p> 
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="oldVersion">The old database version.</param>
        /// <param name="newVersion">The new database version.</param>
        void OnUpgrade(SQLiteHelper db, int oldVersion, int newVersion);
    }
}

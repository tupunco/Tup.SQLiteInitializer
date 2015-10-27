using System;
using System.Data.SQLite;

namespace Tup.SQLiteInitializer.Demo
{
    /// <summary>
    /// 默认 SQLite 数据库初始化器
    /// </summary>
    /// <remarks>
    /// SQLite 库加解密升级说明:
    ///  http://www.cnblogs.com/yagzh2000/archive/2013/01/18/2865944.html
    ///    如果数据库没有密码，现在要设置：先open数据库，再用SQLite3_ReKey设置密码；
    ///    如果数据库有密码，现在要修改密码，则先open数据库，再用SQLite3_Key输入原来的密码,再用rekey设置新密码。
    ///    如果想把加密后的数据库变成没有加密的，则先open数据库，再用SQLite3_Key输入原来的密码，再用SQLite3_ReKey(pdb,nil,0)即可。
    ///    最后用SQLite3_Close(pdb)关闭即可
    /// </remarks>
    public class DefaultSQLiteInitializer : AbstractSQLiteInitializer
    {
        /// <summary>
        /// 当前数据库版本号
        /// </summary>
        public override int NewVersion
        {
            get { return 1024; }
        }

        public override string DefaultConnectionString
        {
            get { return SQLConfig.ConnectionString; }
        }

        public DefaultSQLiteInitializer()
        {
            base.ToMapping<TestBillEntity>();
            base.ToMapping<TestBillEntity2>();
        }

        #region OnDoLegacy

        /// <summary>
        /// 处理遗留版本动作
        /// </summary>
        protected override void OnDoLegacy()
        {
            //======处理数据库加密, 转库操作======
            var isKeyDB = true;
            //1.判断库是否为加密库
            using (var db = new SQLiteHelper(SQLConfig.ConnectionString))
            {
                isKeyDB = CheckLegacyDBIsKeyed(db);
            }

            //2.如果为不加密的库设置密码
            if (!isKeyDB)
            {
                using (var db = new SQLiteHelper(SQLConfig.NormalConnectionString))
                {
                    var conn = db.DbConnection as SQLiteConnection;
                    conn.Open();
                    conn.ChangePassword(SQLConfig.DBConnectionKey);
                }
            }
        }

        /// <summary>
        /// 判断库是否为加密库
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        private bool CheckLegacyDBIsKeyed(SQLiteHelper keyedDB)
        {
            try
            {
                keyedDB.ExecuteNonQuery("PRAGMA user_version;", ConnectionState.KeepOpen);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion OnDoLegacy

        /// <summary>
        /// 当前数据库版本号
        /// </summary>
        /// <param name="db"></param>
        /// <param name="currentVersion"></param>
        protected override void OnCreate(SQLiteHelper db, int currentVersion)
        {
            //TODO---
            this.Execute(string.Format("insert into t_test_bill (BillGuid,EmployeeId,DiscountPayment) values ('{0}', 123123000, 12.3)", Guid.NewGuid()));
            this.Execute(string.Format("insert into t_test_bill (BillGuid,EmployeeId,DiscountPayment) values ('{0}', 123123111, 56.3)", Guid.NewGuid()));
            this.Execute(string.Format("insert into t_test_bill (BillGuid,EmployeeId,DiscountPayment) values ('{0}', 123123222, 78.3)", Guid.NewGuid()));
        }

        /// <summary>
        /// 升级数据库时动作
        /// </summary>
        /// <param name="db"></param>
        /// <param name="oldVersion"></param>
        /// <param name="newVersion"></param>
        protected override void OnUpgrade(SQLiteHelper db, int oldVersion, int newVersion)
        {
            //INFO: 所有数据库操作请加 ConnectionState.KeepOpen 设置

            if (oldVersion < 20151023)
            {
                //DropIndexIfExists
                //Modify...
                this.Execute(string.Format("insert into t_test_bill (BillGuid,EmployeeId,DiscountPayment) values ('{0}', 123, 12.3)", Guid.NewGuid()));
                this.Execute(string.Format("insert into t_test_bill (BillGuid,EmployeeId,DiscountPayment) values ('{0}', 456, 56.3)", Guid.NewGuid()));
                this.Execute(string.Format("insert into t_test_bill (BillGuid,EmployeeId,DiscountPayment) values ('{0}', 789, 78.3)", Guid.NewGuid()));
            }

            //----新的版本修改放到这里------
            //if (oldVersion < *****)  //***** 新增修改操作
            //{
            //
            //}
            //-------
        }
    }
}
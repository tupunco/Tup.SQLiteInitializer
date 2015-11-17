using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Tup.SQLiteInitializer.Common;

namespace Tup.SQLiteInitializer
{
    /// <summary>
    /// SQLite Database Initializer
    /// </summary>
    public class SQLiteDatabaseInitializer
    {
        /// <summary>
        /// SQLiteInitializer Handler
        /// </summary>
        private ISQLiteInitializer Initializer = null;

        /// <summary>
        ///
        /// </summary>
        /// <param name="db"></param>
        private SQLiteDatabaseInitializer() { }

        ///// <summary>
        ///// try init SQLite Database
        ///// </summary>
        //public static void TryInitializer()
        //{
        //    TryInitializer(new DefaultSQLiteInitializer());
        //}
        /// <summary>
        /// try init SQLite Database
        /// </summary>
        /// <param name="initializer"></param>
        public static void TryInitializer(ISQLiteInitializer initializer)
        {
            ThrowHelper.ThrowIfNull(initializer, "Initializer");

            var init = new SQLiteDatabaseInitializer();
            init.Initializer = initializer;
            init.InternalInitializer();
        }

        /// <summary>
        /// Internal Initializer 升级器
        /// </summary>
        private void InternalInitializer()
        {
            var init = this.Initializer;

            //处理遗留系统
            init.OnDoLegacy();

            var mNewVersion = init.NewVersion;
            using (var db = new SQLiteHelper(init.DefaultConnectionString))
            {
                int version = this.GetVersion(db);
                if (mNewVersion <= version)
                    return;

                try
                {
                    db.BeginTransaction();

                    if (version <= 0)
                    {
                        //初始化数据库操作
                        init.OnCreate(db, version);
                    }
                    else
                    {
                        if (version > mNewVersion)
                        {
                            throw new SystemException("只能升级到高版本数据库结构");
                        }
                        else
                        {
                            //升级数据库操作
                            init.OnUpgrade(db, version, mNewVersion);
                        }
                    }
                    //升级结束设置新的版本号
                    SetVersion(db, mNewVersion);

                    db.CommitTransaction();
                }
                catch (Exception ex)
                {
                    db.RollbackTransaction();
                    throw new Exception(ex.Message, ex);
                }
            }
        }

        #region 数据库版本升级

        /// <summary>
        /// DB 版本
        /// </summary>
        /// <returns>
        /// -1 没找到版本号
        /// </returns>
        private int GetVersion(SQLiteHelper db)
        {
            var res = db.ExecuteScalar("PRAGMA user_version;", ConnectionState.KeepOpen);
            if (res is int)
                return (int)res;
            else if (res != null)
            {
                int version = -1;
                int.TryParse(res.ToString(), out version);
                return version;
            }
            return -1;
        }

        /// <summary>
        /// Sets the database version.
        /// </summary>
        /// <param name="version">the new database version</param>
        /// <returns></returns>
        private void SetVersion(SQLiteHelper db, int version)
        {
            db.ExecuteNonQuery(string.Format("PRAGMA user_version ={0};", version), ConnectionState.KeepOpen);
        }

        /// <summary>
        /// Returns true if the new version code is greater than the current database version.
        /// </summary>
        /// <param name="newVersion"></param>
        /// <returns></returns>
        public bool NeedUpgrade(SQLiteHelper db, int newVersion)
        {
            return newVersion > GetVersion(db);
        }

        #endregion 数据库版本升级
    }
}

namespace Tup.SQLiteInitializer.Impl
{
    //-------------------------------------------------------
    //FROM https://github.com/koush/sqlite-net/blob/master/src/SQLite.cs
    // 根据实体自动 "生成表结构/升级表结构/加表索引"
    // 核心代码来源 "sqlite-net"
    //-------------------------------------------------------

    /// <summary>
    /// SQLiteMapping CreateFlags
    /// </summary>
    [Flags]
    internal enum CreateFlags
    {
        None = 0,
        ImplicitPK = 1,    // create a primary key for field called 'Id' (Orm.ImplicitPkName)
        ImplicitIndex = 2, // create an index for fields ending in 'Id' (Orm.ImplicitIndexSuffix)
        AllImplicit = 3,   // do both above

        AutoIncPK = 4      // force PK field to be auto inc
    }

    /// <summary>
    /// Represents an open connection to a SQLite database.
    /// </summary>
    internal class SQLiteMapping
    {
        public bool Trace { get; set; }

        /// <summary>
        /// Current SQLiteDB
        /// </summary>
        public SQLiteHelper SQLiteDB { get; set; }

        private Dictionary<string, TableMapping> _mappings = null;
        private Dictionary<string, TableMapping> _tables = null;
        public bool StoreDateTimeAsTicks { get; private set; }

        private struct IndexedColumn
        {
            public int Order;
            public string ColumnName;
        }

        private struct IndexInfo
        {
            public string IndexName;
            public string TableName;
            public bool Unique;
            public List<IndexedColumn> Columns;
        }

        #region ToMapping

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name="type">
        /// The type whose mapping to the database is returned.
        /// </param>
        /// <param name="createFlags">
        /// Optional flags allowing implicit PK and indexes based on naming conventions
        /// </param>
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains
        /// methods to set and get properties of objects.
        /// </returns>
        private TableMapping GetMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            if (_mappings == null)
            {
                _mappings = new Dictionary<string, TableMapping>();
            }
            TableMapping map;
            if (!_mappings.TryGetValue(type.FullName, out map))
            {
                map = new TableMapping(type, createFlags);
                _mappings[type.FullName] = map;
            }
            return map;
        }

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains
        /// methods to set and get properties of objects.
        /// </returns>
        private TableMapping GetMapping<T>()
        {
            return GetMapping(typeof(T));
        }

        /// <summary>
        /// 创建表实体映射
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public void ToMapping<T>(CreateFlags createFlags = CreateFlags.None)
        {
            ToMapping(typeof(T), createFlags);
        }

        /// <summary>
        /// 创建表实体映射
        /// </summary>
        /// <param name="ty"></param>
        /// <returns></returns>
        public TableMapping ToMapping(Type ty, CreateFlags createFlags = CreateFlags.None)
        {
            if (_tables == null)
                _tables = new Dictionary<string, TableMapping>();

            TableMapping map;
            if (!_tables.TryGetValue(ty.FullName, out map))
            {
                map = GetMapping(ty, createFlags);
                _tables.Add(ty.FullName, map);
            }
            return map;
        }

        #endregion ToMapping

        #region CreateTable

        /// <summary>
        /// 创建所有已映射的所有表
        /// </summary>
        /// <returns></returns>
        public int CreateAllTable()
        {
            if (_tables == null || _tables.Count <= 0)
                return -1;

            var count = 0;
            foreach (var map in _tables.Values)
                count += InternalCreateTable(map);

            return count;
        }

        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        /// The number of entries added to the database schema.
        /// </returns>
        public int CreateTable<T>(CreateFlags createFlags = CreateFlags.None)
        {
            return CreateTable(typeof(T), createFlags);
        }

        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a database table.</param>
        /// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>
        /// <returns>
        /// The number of entries added to the database schema.
        /// </returns>
        public int CreateTable(Type ty, CreateFlags createFlags = CreateFlags.None)
        {
            TableMapping map = null;
            if (_tables == null || !_tables.TryGetValue(ty.FullName, out map))
                map = ToMapping(ty, createFlags);

            return InternalCreateTable(map);
        }

        /// <summary>
        /// Internal CreateTable
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        private int InternalCreateTable(TableMapping map)
        {
            if (map == null)
                throw new ArgumentNullException("map");

            var query = string.Format("CREATE TABLE IF NOT EXISTS \"{0}\"(\n", map.TableName);

            var pks = map.PKs;
            var isConstraintPKs = pks != null && pks.Length > 1; //复合主键
            var decls = map.Columns.Select(p => Orm.SqlDecl(p, StoreDateTimeAsTicks, isConstraintPKs));
            query += string.Join(",\n", decls.ToArray());
            if (isConstraintPKs)//处理复合主键
                query += Orm.SqlDeclPKs(map.PKs);
            query += ");";

            var count = Execute(query);

            if (count == 0 || count == -1) //不同版本的 System.Data.SQLite.dll 返回的结果不一样, 老版本永远返回 0 , 新版本永远返回 -1
            { //Possible bug: This always seems to return 0?
                // Table already exists, migrate it
                MigrateTable(map);
            }

            var indexes = new Dictionary<string, IndexInfo>();
            foreach (var c in map.Columns)
            {
                foreach (var i in c.Indices)
                {
                    var iname = i.Name ?? map.TableName + "_" + c.Name;
                    IndexInfo iinfo;
                    if (!indexes.TryGetValue(iname, out iinfo))
                    {
                        iinfo = new IndexInfo
                        {
                            IndexName = iname,
                            TableName = map.TableName,
                            Unique = i.Unique,
                            Columns = new List<IndexedColumn>()
                        };
                        indexes.Add(iname, iinfo);
                    }

                    if (i.Unique != iinfo.Unique)
                        throw new Exception("All the columns in an index must have the same value for their Unique property");

                    iinfo.Columns.Add(new IndexedColumn
                    {
                        Order = i.Order,
                        ColumnName = c.Name
                    });
                }
            }

            foreach (var indexName in indexes.Keys)
            {
                var index = indexes[indexName];
                var columns = String.Join("\",\"", index.Columns.OrderBy(i => i.Order).Select(i => i.ColumnName).ToArray());
                count += CreateIndex(indexName, index.TableName, columns, index.Unique);
            }

            return count;
        }

        #endregion CreateTable

        /// <summary>
        /// 删除已存在索引
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public int DropIndexIfExists(string indexName)
        {
            if (string.IsNullOrEmpty(indexName))
                return 0;

            const string sqlFormat = "DROP INDEX IF EXISTS \"{0}\"";
            return Execute(string.Format(sqlFormat, indexName));
        }

        /// <summary>
        /// Creates an index for the specified table and column.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string indexName, string tableName, string columnName, bool unique = false)
        {
            const string sqlFormat = "CREATE {2} INDEX IF NOT EXISTS \"{3}\" ON \"{0}\"(\"{1}\")";
            var sql = String.Format(sqlFormat, tableName, columnName, unique ? "unique" : "", indexName);
            return Execute(sql);
        }

        /// <summary>
        /// Creates an index for the specified table and column.
        /// </summary>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(string.Concat(tableName, "_", columnName.Replace("\",\"", "_")), tableName, columnName, unique);
        }

        /// <summary>
        /// Creates an index for the specified object property.
        /// e.g. CreateIndex<Client>(c => c.Name);
        /// </summary>
        /// <typeparam name="T">Type to reflect to a database table.</typeparam>
        /// <param name="property">Property to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public void CreateIndex<T>(Expression<Func<T, object>> property, bool unique = false)
        {
            MemberExpression mx;
            if (property.Body.NodeType == ExpressionType.Convert)
            {
                mx = ((UnaryExpression)property.Body).Operand as MemberExpression;
            }
            else
            {
                mx = (property.Body as MemberExpression);
            }
            var propertyInfo = mx.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
            }

            var propName = propertyInfo.Name;

            var map = GetMapping<T>();
            var colName = map.FindColumnWithPropertyName(propName).Name;

            CreateIndex(map.TableName, colName, unique);
        }

        public class ColumnInfo
        {
            //			public int cid { get; set; }

            [Column("name")]
            public string Name { get; set; }

            //			[Column ("type")]
            //			public string ColumnType { get; set; }

            //			public int notnull { get; set; }

            //			public string dflt_value { get; set; }

            //			public int pk { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private List<ColumnInfo> GetTableInfo(string tableName)
        {
            var query = string.Format("PRAGMA table_info(\"{0}\")", tableName);
            var outList = new List<ColumnInfo>();
            using (var reader = this.SQLiteDB.ExecuteReader(query, ConnectionState.KeepOpen))
            {
                while (reader.Read())
                {
                    outList.Add(new ColumnInfo()
                    {
                        //name/type/notnull
                        Name = reader.GetString(reader.GetOrdinal("name"))
                    });
                }
            }
            return outList;
        }

        /// <summary>
        /// 升级表补充缺失的列
        /// </summary>
        /// <param name="map"></param>
        private void MigrateTable(TableMapping map)
        {
            try
            {
                var existingCols = GetTableInfo(map.TableName);

                var toBeAdded = new List<TableMapping.Column>();

                foreach (var p in map.Columns)
                {
                    var found = false;
                    foreach (var c in existingCols)
                    {
                        found = (string.Compare(p.Name, c.Name, StringComparison.OrdinalIgnoreCase) == 0);
                        if (found)
                            break;
                    }
                    if (!found)
                    {
                        toBeAdded.Add(p);
                    }
                }

                foreach (var p in toBeAdded)
                {
                    var addCol = string.Format("ALTER TABLE \"{0}\" ADD COLUMN {1}", map.TableName, Orm.SqlDecl(p, StoreDateTimeAsTicks));
                    Execute(addCol);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("SQLiteMapping-MigrateTable-TableName:{0}-ex:{1}", map.TableName, ex);
                ex = null;
            }
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// Use this method instead of Query when you don't expect rows back. Such cases include
        /// INSERTs, UPDATEs, and DELETEs.
        /// You can set the Trace or TimeExecution properties of the connection
        /// to profile execution.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// The number of rows modified in the database as a result of this execution.
        /// </returns>
        internal int Execute(string query)
        {
            return this.SQLiteDB.ExecuteNonQuery(query, ConnectionState.KeepOpen);
        }
    }

    /// <summary>
    /// TableMapping
    /// </summary>
    internal class TableMapping
    {
        public Type MappedType { get; private set; }

        public string TableName { get; private set; }

        public Column[] Columns { get; private set; }

        /// <summary>
        /// 复合主键
        /// </summary>
        /// <remarks>
        /// Primary Key(BookId,TableId)
        /// </remarks>
        public Column[] PKs { get; private set; }

        //public string GetByPrimaryKeySql { get; private set; }

        //Column _autoPk;
        //Column[] _insertColumns;
        //Column[] _insertOrReplaceColumns;

        public TableMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            MappedType = type;

#if NETFX_CORE
        var tableAttr = (TableAttribute)System.Reflection.CustomAttributeExtensions
            .GetCustomAttribute(type.GetTypeInfo(), typeof(TableAttribute), true);
#else
            var tableAttr = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault();
#endif

            TableName = tableAttr != null ? tableAttr.Name : MappedType.Name;

#if !NETFX_CORE
            var props = MappedType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
#else
        var props = from p in MappedType.GetRuntimeProperties()
                    where ((p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic) || (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic))
                    select p;
#endif
            var cols = new List<Column>();
            foreach (var p in props)
            {
#if !NETFX_CORE
                var ignore = p.GetCustomAttributes(typeof(IgnoreAttribute), true).Length > 0;
#else
            var ignore = p.GetCustomAttributes(typeof(IgnoreAttribute), true).Count() > 0;
#endif
                if (p.CanWrite && !ignore)
                {
                    cols.Add(new Column(p, createFlags));
                }
            }
            this.Columns = cols.ToArray();

            var pks = new List<Column>();
            foreach (var c in Columns)
            {
                //if (c.IsAutoInc && c.IsPK)
                //{
                //    _autoPk = c;
                //}
                if (c.IsPK)
                {
                    pks.Add(c);
                }
            }
            this.PKs = pks.ToArray();
            //HasAutoIncPK = _autoPk != null;

            //if (PK != null)
            //{
            //    GetByPrimaryKeySql = string.Format("SELECT * FROM \"{0}\" WHERE \"{1}\" = ?", TableName, PK.Name);
            //}
            //else
            //{
            //    // People should not be calling Get/Find without a PK
            //    GetByPrimaryKeySql = string.Format("SELECT * FROM \"{0}\" LIMIT 1", TableName);
            //}
        }

        //public bool HasAutoIncPK { get; private set; }

        //public void SetAutoIncPK(object obj, long id)
        //{
        //    if (_autoPk != null)
        //    {
        //        _autoPk.SetValue(obj, Convert.ChangeType(id, _autoPk.ColumnType, null));
        //    }
        //}

        //public Column[] InsertColumns
        //{
        //    get
        //    {
        //        if (_insertColumns == null)
        //        {
        //            _insertColumns = Columns.Where(c => !c.IsAutoInc).ToArray();
        //        }
        //        return _insertColumns;
        //    }
        //}

        //public Column[] InsertOrReplaceColumns
        //{
        //    get
        //    {
        //        if (_insertOrReplaceColumns == null)
        //        {
        //            _insertOrReplaceColumns = Columns.ToArray();
        //        }
        //        return _insertOrReplaceColumns;
        //    }
        //}

        public Column FindColumnWithPropertyName(string propertyName)
        {
            var exact = Columns.FirstOrDefault(c => c.PropertyName == propertyName);
            return exact;
        }

        //public Column FindColumn(string columnName)
        //{
        //    var exact = Columns.FirstOrDefault(c => c.Name == columnName);
        //    return exact;
        //}

        public class Column
        {
            private PropertyInfo _prop;

            public string Name { get; private set; }

            public string PropertyName { get { return _prop.Name; } }

            public Type ColumnType { get; private set; }

            /// <summary>
            /// 定制类型
            /// </summary>
            public string ValueType { get; private set; }

            public string Collation { get; private set; }

            public bool IsAutoInc { get; private set; }
            public bool IsAutoGuid { get; private set; }

            public bool IsPK { get; private set; }

            /// <summary>
            /// 复合主键时键顺序
            /// </summary>
            public int PKOrder { get; private set; }

            public IEnumerable<IndexedAttribute> Indices { get; set; }

            public bool IsNullable { get; private set; }
            public string DefaultValue { get; private set; }
            public bool DefaultValueBrackets { get; internal set; }

            public int MaxStringLength { get; private set; }

            public Column(PropertyInfo prop, CreateFlags createFlags = CreateFlags.None)
            {
                var colAttr = (ColumnAttribute)prop.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();

                _prop = prop;
                Name = colAttr == null ? prop.Name : (!string.IsNullOrEmpty(colAttr.Name) ? colAttr.Name : prop.Name);
                //If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T, otherwise it returns null, so get the actual type instead
                ColumnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                ValueType = colAttr != null ? colAttr.ValueType : null;
                Collation = Orm.Collation(prop);

                var pkOrder = 0;
                IsPK = Orm.IsPK(prop, out pkOrder) ||
                    (((createFlags & CreateFlags.ImplicitPK) == CreateFlags.ImplicitPK) &&
                        string.Compare(prop.Name, Orm.ImplicitPkName, StringComparison.OrdinalIgnoreCase) == 0);
                PKOrder = pkOrder;

                var isAuto = Orm.IsAutoInc(prop) || (IsPK && ((createFlags & CreateFlags.AutoIncPK) == CreateFlags.AutoIncPK));
                IsAutoGuid = isAuto && ColumnType == typeof(Guid);
                IsAutoInc = isAuto && !IsAutoGuid;

                Indices = Orm.GetIndices(prop);
                if (!Indices.Any()
                    && !IsPK
                    && ((createFlags & CreateFlags.ImplicitIndex) == CreateFlags.ImplicitIndex)
                    && Name.EndsWith(Orm.ImplicitIndexSuffix, StringComparison.OrdinalIgnoreCase)
                    )
                {
                    Indices = new IndexedAttribute[] { new IndexedAttribute() };
                }

                IsNullable = !IsPK && !(colAttr != null && colAttr.IsNotNull);
                DefaultValue = colAttr != null ? colAttr.DefaultValue : null;
                MaxStringLength = Orm.MaxStringLength(prop);
            }

            //public void SetValue(object obj, object val)
            //{
            //    _prop.SetValue(obj, val, null);
            //}

            //public object GetValue(object obj)
            //{
            //    return _prop.GetValue(obj, null);
            //}
        }
    }

    /// <summary>
    /// Orm
    /// </summary>
    internal static class Orm
    {
        public const int DefaultMaxStringLength = 140;
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        /// <summary>
        /// 复合主键拼接
        /// </summary>
        /// <param name="pks"></param>
        /// <returns></returns>
        public static string SqlDeclPKs(TableMapping.Column[] pks)
        {
            if (pks == null || pks.Length <= 0)
                throw new ArgumentNullException("pks");

            return string.Format(",\r\nPrimary Key({0})\r\n", string.Join(",", pks.OrderBy(p => p.PKOrder).Select(pk => pk.Name)));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p"></param>
        /// <param name="storeDateTimeAsTicks"></param>
        /// <param name="ignorePk">忽略主键(复合主键 true)</param>
        /// <returns></returns>
        public static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks, bool ignorePk = false)
        {
            string decl = string.Format("\"{0}\" {1} ", p.Name, SqlType(p, storeDateTimeAsTicks));

            if (!ignorePk && p.IsPK)
            {
                decl += "PRIMARY KEY ";
            }
            if (p.IsAutoInc)
            {
                decl += "AUTOINCREMENT ";
            }
            if (!p.IsNullable)
            {
                decl += "NOT NULL ";
            }
            if (p.DefaultValue != null)
            {
                decl += p.DefaultValueBrackets
                            ? string.Format("DEFAULT '{0}' ", p.DefaultValue)
                                : string.Format("DEFAULT {0} ", p.DefaultValue);
            }
            if (!string.IsNullOrEmpty(p.Collation))
            {
                decl += string.Format("COLLATE {0} ", p.Collation);
            }

            return decl;
        }

        private static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            //定制类型
            if (!string.IsNullOrEmpty(p.ValueType))
                return p.ValueType;

            var clrType = p.ColumnType;
            if (clrType == typeof(Boolean))
            {
                return "SMALLINT";
            }
            if (clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32))
            {
                return "INTEGER";
            }
            else if (clrType == typeof(UInt32) || clrType == typeof(Int64))
            {
                return "BIGINT";
            }
            else if (clrType == typeof(Single) || clrType == typeof(Decimal))
            {
                return "FLOAT";
            }
            else if (clrType == typeof(Double))
            {
                return "DOUBLE";
            }
            else if (clrType == typeof(String))
            {
                int len = p.MaxStringLength;
                p.DefaultValueBrackets = true;
                return string.Format("VARCHAR({0})", len);
            }
            else if (clrType == typeof(DateTime))
            {
                p.DefaultValueBrackets = true;
                return storeDateTimeAsTicks ? "BIGINT" : "DATETIME";
#if !NETFX_CORE
            }
            else if (clrType.IsEnum)
            {
#else
			} else if (clrType.GetTypeInfo().IsEnum) {
#endif
                return "INTEGER";
            }
            else if (clrType == typeof(byte[]))
            {
                p.DefaultValueBrackets = true;
                return "BLOB";
            }
            else if (clrType == typeof(Guid))
            {
                p.DefaultValueBrackets = true;
                return "VARCHAR(36)";
            }
            else
            {
                throw new NotSupportedException("Don't know about " + clrType);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p"></param>
        /// <param name="order">复合主键 顺序</param>
        /// <returns></returns>
        public static bool IsPK(MemberInfo p, out int order)
        {
            order = 0;
            var attrs = p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true);
            PrimaryKeyAttribute defaultPK = null;
#if !NETFX_CORE
            var countRes = attrs.Length > 0;
            if (countRes)
                defaultPK = (PrimaryKeyAttribute)attrs[0];
#else
			var countRes = return attrs.Count() > 0;
            if (countRes)
                defaultPK = (PrimaryKeyAttribute)attrs.First();
#endif
            if (countRes && defaultPK != null)
                order = defaultPK.Order;
            return countRes;
        }

        public static string Collation(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(CollationAttribute), true);
#if !NETFX_CORE
            if (attrs.Length > 0)
            {
                return ((CollationAttribute)attrs[0]).Value;
#else
			if (attrs.Count() > 0) {
                return ((CollationAttribute)attrs.First()).Value;
#endif
            }
            else
            {
                return string.Empty;
            }
        }

        public static bool IsAutoInc(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(AutoIncrementAttribute), true);
#if !NETFX_CORE
            return attrs.Length > 0;
#else
			return attrs.Count() > 0;
#endif
        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }

        public static int MaxStringLength(PropertyInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(MaxLengthAttribute), true);
#if !NETFX_CORE
            if (attrs.Length > 0)
            {
                return ((MaxLengthAttribute)attrs[0]).Value;
#else
			if (attrs.Count() > 0) {
				return ((MaxLengthAttribute)attrs.First()).Value;
#endif
            }
            else
            {
                return DefaultMaxStringLength;
            }
        }
    }
}
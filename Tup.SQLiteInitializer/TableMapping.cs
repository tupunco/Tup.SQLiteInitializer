using System;

namespace Tup.SQLiteInitializer
{
    #region 表映射 特征

    //FREOM: https://github.com/koush/sqlite-net/blob/master/src/SQLite.cs
    /// <summary>
    /// SQLite 表 特征
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }

        public TableAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// SQLite 字段 特征
    /// </summary>
    /// <remarks>
    /// 不加直接属性名称作为数据库字段名称
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// Column Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Column Default Value
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Column Value Type
        /// </summary>
        /// <remarks>
        /// 类型定制设置, 例如: DECIMAL(19,4)
        /// </remarks>
        public string ValueType { get; set; }

        /// <summary>
        /// Column IS NOT NULL
        /// </summary>
        public bool IsNotNull { get; set; }

        /// <summary>
        ///
        /// </summary>
        public ColumnAttribute()
            : this(null, false)
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        public ColumnAttribute(string name)
            : this(name, false)
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isNotNull"></param>
        public ColumnAttribute(string name, bool isNotNull)
            : this(name, null, isNotNull)
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        public ColumnAttribute(string name, string defaultValue)
            : this(name, defaultValue, false)
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <param name="isNotNull"></param>
        public ColumnAttribute(string name, string defaultValue, bool isNotNull)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
            this.IsNotNull = isNotNull;
        }
    }

    /// <summary>
    /// SQLite 忽略字段 特征
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// SQLite 字段最大长度 特征
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MaxLengthAttribute : Attribute
    {
        public int Value { get; private set; }

        public MaxLengthAttribute(int length)
        {
            Value = length;
        }
    }

    /// <summary>
    /// SQLite 主键 特征
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute
    {
        /// <summary>
        /// 复合主键顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        ///
        /// </summary>
        public PrimaryKeyAttribute() : this(0) { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="order">复合主键顺序</param>
        public PrimaryKeyAttribute(int order)
        {
            Order = order;
        }
    }

    /// <summary>
    /// SQLite 自动增加字段 特征
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AutoIncrementAttribute : Attribute
    {
    }

    /// <summary>
    /// SQLite 索引 特征
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class IndexedAttribute : Attribute
    {
        /// <summary>
        /// 索引名称
        /// </summary>
        /// <remarks>
        /// 复合索引多个字段采用相同名称
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// 复合索引字段循序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否唯一索引
        /// </summary>
        public virtual bool Unique { get; set; }

        public IndexedAttribute()
        {
        }

        public IndexedAttribute(string name, int order)
        {
            Name = name;
            Order = order;
        }
    }

    /// <summary>
    /// SQLite 唯一索引 特征
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class UniqueAttribute : IndexedAttribute
    {
        public override bool Unique
        {
            get { return true; }
            set { /* throw?  */ }
        }
    }

    /// <summary>
    /// SQLite 索引比较规则 特征
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CollationAttribute : Attribute
    {
        /// <summary>
        /// BINARY/NOCASE/REVERSE
        /// </summary>
        public string Value { get; private set; }

        public CollationAttribute(string collation)
        {
            Value = collation;
        }
    }

    #endregion 表映射 特征
}
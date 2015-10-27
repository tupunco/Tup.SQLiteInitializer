using System;

namespace Tup.SQLiteInitializer.Demo
{
    [Table("t_test_bill")]
    public class TestBillEntity
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [MaxLength(64)]
        [Column(IsNotNull = true)]
        [Indexed(Name = "idx_Bill_NetBillId_State", Order = 1)] //复合索引 第一个列
        [Indexed(Name = "idx_Bill_NetBillId", Order = 1, Unique = true)]
        public string BillGuid { get; set; }

        [Column(DefaultValue = "0")]
        public int EmployeeId { get; set; }

        [Column(ValueType = "DECIMAL(19,4)")]
        public Decimal DiscountPayment { get; set; }

        [Indexed(Name = "idx_Bill_NetBillId_State", Order = 2)]  //复合索引 第二个列
        [Indexed(Name = "idx_Bill_State", Order = 1)]
        [Column(IsNotNull = true, DefaultValue = "0")]
        [Collation("BINARY")]
        public int State { get; set; }

        [Column(IsNotNull = true, DefaultValue = "0")]
        public int BillType { get; set; }
    }

    [Table("t_test_bill2")]
    public class TestBillEntity2
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [MaxLength(64)]
        [Column(IsNotNull = true)]
        [Indexed(Name = "idx_Bill_NetBillId_State", Order = 1)] //复合索引 第一个列
        [Indexed(Name = "idx_Bill_NetBillId", Order = 1, Unique = true)]
        public string BillGuid { get; set; }

        [Column(DefaultValue = "0")]
        public int EmployeeId { get; set; }

        [Column(ValueType = "DECIMAL(19,4)")]
        public Decimal DiscountPayment { get; set; }

        [Indexed(Name = "idx_Bill_NetBillId_State", Order = 2)]  //复合索引 第二个列
        [Indexed(Name = "idx_Bill_State", Order = 1)]
        [Column(IsNotNull = true, DefaultValue = "0")]
        [Collation("BINARY")]
        public int State { get; set; }

        [Column(IsNotNull = true, DefaultValue = "0")]
        public int BillType { get; set; }
    }

}

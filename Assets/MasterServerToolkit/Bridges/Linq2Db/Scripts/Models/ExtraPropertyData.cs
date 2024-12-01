using LinqToDB.Mapping;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    [Table("extra_properties")]
    public class ExtraPropertyData
    {
        [Column("account_id", Length = 38, DataType = LinqToDB.DataType.VarChar), PrimaryKey, NotNull]
        public string AccountId { get; set; }
        [Column("property_key", Length = 45, DataType = LinqToDB.DataType.VarChar), PrimaryKey, NotNull]
        public string PropertyKey { get; set; }
        [Column("property_value", Length = 128, DataType = LinqToDB.DataType.VarChar)]
        public string PropertyValue { get; set; }
    }
}
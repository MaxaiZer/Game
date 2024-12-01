using LinqToDB.Mapping;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    [Table("profiles")]
    public class ProfilePropertyData
    {
        [Column("account_id", Length = 38, DataType = LinqToDB.DataType.VarChar), PrimaryKey, NotNull]
        public string AccountId { get; set; } = string.Empty;
        [Column("property_key", Length = 45, DataType = LinqToDB.DataType.VarChar), PrimaryKey, NotNull]
        public string PropertyKey { get; set; } = string.Empty;
        [Column("property_value", DataType = LinqToDB.DataType.Json)]
        public string PropertyValue { get; set; } = string.Empty;
    }
}
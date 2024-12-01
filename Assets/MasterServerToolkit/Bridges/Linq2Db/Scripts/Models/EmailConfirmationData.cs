using LinqToDB.Mapping;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    [Table("email_confirmation_codes")]
    public class EmailConfirmationData
    {
        [Column("email", Length = 45, DataType = LinqToDB.DataType.VarChar), PrimaryKey, NotNull]
        public string Email { get; set; }
        [Column("code", Length = 10, DataType = LinqToDB.DataType.VarChar), NotNull]
        public string Code { get; set; }
    }
}

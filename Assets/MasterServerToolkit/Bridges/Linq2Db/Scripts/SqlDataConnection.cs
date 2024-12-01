using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;
using System.Linq;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    public class SqlDataConnection : DataConnection
    {
        public SqlDataConnection(string configuration) : base(configuration) { }

        /// <summary>
        /// 
        /// </summary>
        public static void CreateAllTables(string configuration)
        {
            var connection = new SqlDataConnection(configuration);
            ISchemaProvider schemaProvider = connection.DataProvider.GetSchemaProvider();
            var schema = schemaProvider.GetSchema(connection);

            if (!schema.Tables.Any(t => t.TableName == "accounts"))
            {
                connection.CreateTable<AccountInfoData>();
            }

            if (!schema.Tables.Any(t => t.TableName == "email_confirmation_codes"))
            {
                connection.CreateTable<EmailConfirmationData>();
            }

            if (!schema.Tables.Any(t => t.TableName == "password_reset_codes"))
            {
                connection.CreateTable<PasswordResetData>();
            }
        }

        public ITable<AccountInfoData> Accounts => this.GetTable<AccountInfoData>();
        public ITable<EmailConfirmationData> EmailConfirmationCodes => this.GetTable<EmailConfirmationData>();
        public ITable<PasswordResetData> PasswordResetCodes => this.GetTable<PasswordResetData>();
    }
}
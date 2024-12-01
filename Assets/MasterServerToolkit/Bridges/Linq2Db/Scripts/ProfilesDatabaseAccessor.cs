using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;
using MasterServerToolkit.Extensions;
using MasterServerToolkit.Logging;
using MasterServerToolkit.MasterServer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    public class ProfilesDatabaseAccessor : IProfilesDatabaseAccessor
    {
        public MstProperties CustomProperties { get; private set; } = new MstProperties();
        public Logger Logger { get; set; }

        private DataConnection connection = null;

        public ProfilesDatabaseAccessor(string configuration)
        {
            connection = new DataConnection(configuration);

            ISchemaProvider schemaProvider = connection.DataProvider.GetSchemaProvider();
            var schema = schemaProvider.GetSchema(connection);

            if (!schema.Tables.Any(t => t.TableName == "profiles"))
            {
                connection.CreateTable<ProfilePropertyData>();
            }
        }

        ~ProfilesDatabaseAccessor() { Dispose(); }

        public void Dispose()
        {
            connection?.Dispose();
        }

        public async Task RestoreProfileAsync(ObservableServerProfile profile)
        {
            var table = connection.GetTable<ProfilePropertyData>();
            List<ProfilePropertyData> entries = await table.Where(a => a.AccountId == profile.UserId).ToListAsync();

            foreach (var entry in entries)
            {
                if (profile.TryGet(entry.PropertyKey.ToUint16Hash(), out IObservableProperty property))
                {
                    string propertyValue = entry.PropertyValue;
                    property.FromJson(propertyValue);
                }
            }
        }

        public async Task UpdateProfileAsync(ObservableServerProfile profile)
        {
            var table = connection.GetTable<ProfilePropertyData>();

            foreach (var property in profile)
            {
                await table
                .InsertOrUpdateAsync(() => new ProfilePropertyData()
                {
                    AccountId = profile.UserId,
                    PropertyKey = StringExtensions.FromHash(property.Key),
                    PropertyValue = property.ToJson().ToString()
                },
                (t) => new ProfilePropertyData()
                {
                    AccountId = profile.UserId,
                    PropertyKey = StringExtensions.FromHash(property.Key),
                    PropertyValue = property.ToJson().ToString()
                });
            }
        }
    }
}
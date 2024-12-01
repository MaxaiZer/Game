using LinqToDB.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    public class Linq2DbDatabaseSettings : ILinqToDBSettings
    {
        private IEnumerable<IDataProviderSettings> dataProviders = Enumerable.Empty<IDataProviderSettings>();
        private string defaultConfiguration = "SqlServer";
        private string defaultDataprovider = "SqlServer";
        private IEnumerable<IConnectionStringSettings> connectionStringSettings;

        public IEnumerable<IDataProviderSettings> DataProviders => dataProviders;
        public string DefaultConfiguration => defaultConfiguration;
        public string DefaultDataProvider => defaultDataprovider;
        public IEnumerable<IConnectionStringSettings> ConnectionStrings => connectionStringSettings;

        public Linq2DbDatabaseSettings(IConnectionStringSettings connectionStringSettings)
            : this(new List<IConnectionStringSettings>() { connectionStringSettings }) { }

        public Linq2DbDatabaseSettings(IEnumerable<IConnectionStringSettings> connectionStringSettings)
        {
            this.connectionStringSettings = connectionStringSettings;
        }
    }
}

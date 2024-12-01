using LinqToDB.Configuration;
using LinqToDB.Data;
using MasterServerToolkit.MasterServer;
using System.Diagnostics;
using UnityEngine;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    public abstract class Linq2DbDatabaseAccessorFactory : DatabaseAccessorFactory
    {
        #region INSPECTOR

        [Header("Settings"), SerializeField, Tooltip("Use [configuration] field to set your own connection string settings for database client")]
        protected string configuration = "master_server_toolkit";
        [SerializeField]
        protected string connectionString = "Server=localhost;Database=master_server_toolkit;Uid=root;Pwd=qazwsxedc123!@#;Port=3306;";
        [SerializeField]
        protected string dataProvider = "SqlServer";

        #endregion

        protected static Linq2DbDatabaseSettings databaseSettings = null;

        protected override void Awake()
        {
            base.Awake();

            configuration = Mst.Args.AsString(Mst.Args.Names.DatabaseConfiguration, configuration);
            connectionString = Mst.Args.AsString(Mst.Args.Names.DatabaseConnectionString, connectionString);
            dataProvider = Mst.Args.AsString(Mst.Args.Names.DatabaseProvider, dataProvider);

            if (databaseSettings == null)
            {
                var connectionSettings = new ConnectionStringSettings(configuration, connectionString, dataProvider);
                databaseSettings = new Linq2DbDatabaseSettings(connectionSettings);

                DataConnection.DefaultSettings = databaseSettings;

                switch (logLevel)
                {
                    case Logging.LogLevel.All:
                    case Logging.LogLevel.Trace:
                    case Logging.LogLevel.Debug:
                        DataConnection.TurnTraceSwitchOn(TraceLevel.Verbose);
                        DataConnection.WriteTraceLine = (message, category, level) =>
                        {
                            if (level == TraceLevel.Info)
                            {
                                logger.Info($"{message}, {category}");
                            }
                            else if (level == TraceLevel.Error)
                            {
                                logger.Error($"{message}, {category}");
                            }
                            else if (level == TraceLevel.Warning)
                            {
                                logger.Warn($"{message}, {category}");
                            }
                            else
                            {
                                logger.Info($"{message}, {category}");
                            }
                        };
                        break;
                }
            }
        }

        public override void CreateAccessors() { }
    }
}

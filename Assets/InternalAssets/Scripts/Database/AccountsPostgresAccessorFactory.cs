using MasterServerToolkit.Bridges.LiteDB;
using MasterServerToolkit.MasterServer;
using MasterServerToolkit.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.InternalAssets.Scripts.Database
{
    internal class AccountsPostgresAccessorFactory : PostgresAccessorFactory
    {
        private AccountsDatabaseAccessor accessor;

        private void OnDestroy()
        {
#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
            accessor?.Dispose();
#endif
        }

        public override void CreateAccessors()
        {
#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
            try
            {
                accessor = new AccountsDatabaseAccessor();
                accessor.Logger = logger;

                Mst.Server.DbAccessors.AddAccessor(accessor);
            }
            catch (Exception e)
            {
                logger.Error("Failed to setup postgres");
                logger.Error(e);
            }
#endif
        }
    }
}

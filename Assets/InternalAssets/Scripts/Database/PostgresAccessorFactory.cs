using MasterServerToolkit.MasterServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.InternalAssets.Scripts.Database
{
    public class PostgresAccessorFactory : DatabaseAccessorFactory
    {

        protected virtual void OnValidate()
        {

        }

        public override void CreateAccessors() { }
    }
}

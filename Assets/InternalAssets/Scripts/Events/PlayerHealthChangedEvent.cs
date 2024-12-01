using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.App.Scripts.Events
{
    internal struct PlayerHealthChangedEvent
    {
        public int maxHealth;
        public int newHealth;
    }
}

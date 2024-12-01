using MasterServerToolkit.MasterServer;

namespace MasterServerToolkit.Bridges.QuestMachine
{
    //[RequireComponent(typeof(PixelCrushers.QuestMachine.Wrappers.QuestMachineConfiguration))]
    public class QuestMachineModule : BaseServerModule
    {
        #region INSPECTOR

        //[Tooltip("Quest asset databases to load into memory.")]
        //[SerializeField]
        //private List<QuestDatabase> m_questDatabases;

        #endregion

        public override void Initialize(IServer server)
        {

        }
    }
}
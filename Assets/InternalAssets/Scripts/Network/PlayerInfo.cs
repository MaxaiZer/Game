namespace Assets.App.Scripts.Network
{
    internal class PlayerInfo
    {

        public int id;

        public string name;

        public int kills;

        public int deaths;

        public PlayerInfo() { }

        public PlayerInfo(int id, string name, int kills = 0, int deaths = 0)
        {
            this.id = id;
            this.name = name;
            this.kills = kills;
            this.deaths = deaths;
        }

    }
}

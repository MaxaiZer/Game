using LinqToDB.Mapping;
using MasterServerToolkit.MasterServer;
using System;
using System.Collections.Generic;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    [Table("accounts")]
    public class AccountInfoData : IAccountInfoData
    {
        [Column("id", Length = 38, DataType = LinqToDB.DataType.VarChar), PrimaryKey, NotNull]
        public string Id { get; set; } = string.Empty;
        [Column("username", Length = 45, DataType = LinqToDB.DataType.VarChar), PrimaryKey, NotNull]
        public string Username { get; set; } = string.Empty;
        [Column("password", Length = 128, DataType = LinqToDB.DataType.VarChar)]
        public string Password { get; set; } = string.Empty;
        [Column("email", Length = 45, DataType = LinqToDB.DataType.VarChar)]
        public string Email { get; set; } = string.Empty;
        [Column("token", Length = 128, DataType = LinqToDB.DataType.VarChar)]
        public string Token { get; set; } = string.Empty;
        [Column("last_login", DataType = LinqToDB.DataType.DateTime)]
        public DateTime LastLogin { get; set; }
        [Column("created", DataType = LinqToDB.DataType.DateTime)]
        public DateTime Created { get; set; }
        [Column("updated", DataType = LinqToDB.DataType.DateTime)]
        public DateTime Updated { get; set; }
        [Column("is_admin", DataType = LinqToDB.DataType.Boolean)]
        public bool IsAdmin { get; set; }
        [Column("is_guest", DataType = LinqToDB.DataType.Boolean)]
        public bool IsGuest { get; set; }
        [Column("is_email_confirmed", DataType = LinqToDB.DataType.Boolean)]
        public bool IsEmailConfirmed { get; set; }
        [Column("is_banned", DataType = LinqToDB.DataType.Boolean)]
        public bool IsBanned { get; set; }
        [Column("device_id", Length = 45, DataType = LinqToDB.DataType.VarChar)]
        public string DeviceId { get; set; } = string.Empty;
        [Column("device_name", Length = 45, DataType = LinqToDB.DataType.VarChar)]
        public string DeviceName { get; set; } = string.Empty;
        [NotColumn]
        public Dictionary<string, string> ExtraProperties { get; set; } = new Dictionary<string, string>();

        public event Action<IAccountInfoData> OnChangedEvent;

        public AccountInfoData()
        {
            Id = Mst.Helper.CreateGuidString();
            Username = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            Token = string.Empty;
            IsAdmin = false;
            IsGuest = true;
            IsEmailConfirmed = false;
            IsBanned = false;
            LastLogin = DateTime.UtcNow;
            Created = DateTime.UtcNow;
            Updated = DateTime.UtcNow;
            ExtraProperties = new Dictionary<string, string>()
            {
                { "phone_number", string.Empty },
                { "facebook_id", string.Empty },
                { "google_play_id", string.Empty },
                { "yandex_games_id", string.Empty },
            };
        }

        public void MarkAsDirty()
        {
            OnChangedEvent?.Invoke(this);
        }
    }
}
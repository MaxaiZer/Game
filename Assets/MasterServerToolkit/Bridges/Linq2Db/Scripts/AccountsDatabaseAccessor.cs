using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;
using MasterServerToolkit.Logging;
using MasterServerToolkit.MasterServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    public class AccountsDatabaseAccessor : IAccountsDatabaseAccessor
    {
        public MstProperties CustomProperties { get; private set; } = new MstProperties();
        public Logger Logger { get; set; }

        private DataConnection connection = null;

        public AccountsDatabaseAccessor(string configuration)
        {
            //Dispose();

            connection = new DataConnection(configuration);

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

            if (!schema.Tables.Any(t => t.TableName == "extra_properties"))
            {
                connection.CreateTable<ExtraPropertyData>();
            }
        }

        ~AccountsDatabaseAccessor() { Dispose(); }

        public IAccountInfoData CreateAccountInstance()
        {
            return new AccountInfoData();
        }

        public void Dispose()
        {
            connection?.DisposeAsync();
        }

        private async Task<bool> UpdateLastLogin(IAccountInfoData account)
        {
            account.LastLogin = DateTime.UtcNow;

            var table = connection.GetTable<AccountInfoData>();
            await table
                .Where(a => a.Id == account.Id)
                .Set(a => a.LastLogin, account.LastLogin)
                .UpdateAsync();

            return true;
        }

        public async Task<IAccountInfoData> GetAccountByDeviceIdAsync(string deviceId)
        {
            var table = connection.GetTable<AccountInfoData>();
            var account = await table.Where(a => a.DeviceId == deviceId).FirstOrDefaultAsync();

            if (account != null)
            {
                account.ExtraProperties = await GetExtraPropertiesAsync(account.Id);

                _ = Task.Run(async () =>
                {
                    await UpdateLastLogin(account);
                });
            }

            return account;
        }

        public async Task<IAccountInfoData> GetAccountByEmailAsync(string email)
        {
            var table = connection.GetTable<AccountInfoData>();
            var account = await table.Where(a => a.Email == email).FirstOrDefaultAsync();

            if (account != null)
            {
                account.ExtraProperties = await GetExtraPropertiesAsync(account.Id);

                _ = Task.Run(async () =>
                {
                    await UpdateLastLogin(account);
                });
            }

            return account;
        }

        public async Task<IAccountInfoData> GetAccountByIdAsync(string id)
        {
            var table = connection.GetTable<AccountInfoData>();
            var account = await table.Where(a => a.Id == id).FirstOrDefaultAsync();

            if (account != null)
            {
                account.ExtraProperties = await GetExtraPropertiesAsync(account.Id);

                _ = Task.Run(async () =>
                {
                    await UpdateLastLogin(account);
                });
            }

            return account;
        }

        public Task<IAccountInfoData> GetAccountByExtraPropertyAsync(string propertyKey, string propertyValue)
        {
            return null;
        }

        public async Task<IAccountInfoData> GetAccountByTokenAsync(string token)
        {
            var table = connection.GetTable<AccountInfoData>();
            var account = await table.Where(a => a.Token == token).FirstOrDefaultAsync();

            if (account != null)
            {
                account.ExtraProperties = await GetExtraPropertiesAsync(account.Id);

                _ = Task.Run(async () =>
                {
                    await UpdateLastLogin(account);
                });
            }

            return account;
        }

        public async Task<IAccountInfoData> GetAccountByUsernameAsync(string username)
        {
            var table = connection.GetTable<AccountInfoData>();
            var account = await table.Where(a => a.Username == username).FirstOrDefaultAsync();

            if (account != null)
            {
                account.ExtraProperties = await GetExtraPropertiesAsync(account.Id);

                _ = Task.Run(async () =>
                {
                    await UpdateLastLogin(account);
                });
            }

            return account;
        }

        public async Task<Dictionary<string, string>> GetExtraPropertiesAsync(string accountId)
        {
            var table = connection.GetTable<ExtraPropertyData>();
            var extraProperties = new Dictionary<string, string>();

            foreach (var property in await table.Where(p => p.AccountId == accountId).ToListAsync())
            {
                extraProperties.Add(property.PropertyKey, property.PropertyValue);
            }

            return extraProperties;
        }

        public async Task SaveEmailConfirmationCodeAsync(string email, string code)
        {
            try
            {
                var table = connection.GetTable<EmailConfirmationData>();
                EmailConfirmationData entry = await table.Where(e => e.Email == email).FirstOrDefaultAsync();

                await table
                    .InsertOrUpdateAsync(() => new EmailConfirmationData()
                    {
                        Email = email,
                        Code = code
                    },
                    (t) => new EmailConfirmationData()
                    {
                        Email = email,
                        Code = code
                    });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public async Task<bool> CheckEmailConfirmationCodeAsync(string email, string code)
        {
            try
            {
                var table = connection.GetTable<EmailConfirmationData>();
                EmailConfirmationData entry = await table.Where(e => e.Email == email).FirstOrDefaultAsync();

                if (entry != null && entry.Code == code)
                {
                    await table.Where(e => e.Email == email).DeleteAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public async Task SavePasswordResetCodeAsync(IAccountInfoData account, string code)
        {
            try
            {
                var table = connection.GetTable<PasswordResetData>();
                await table
                    .Where(m => m.Email == account.Email)
                    .DeleteAsync();

                await table
                    .Value(m => m.Email, account.Email)
                    .Value(m => m.Code, code)
                    .InsertAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public async Task<bool> CheckPasswordResetCodeAsync(string email, string code)
        {
            try
            {
                var table = connection.GetTable<PasswordResetData>();
                var entry = await table.Where(e => e.Email == email).FirstOrDefaultAsync();

                if (entry != null && entry.Code == code)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public async Task<bool> InsertOrUpdateTokenAsync(IAccountInfoData account, string token)
        {
            account.Token = token;

            var table = connection.GetTable<AccountInfoData>();
            await table
                .Where(a => a.Id == account.Id)
                .Set(a => a.Token, account.Token)
                .UpdateAsync();

            return true;
        }

        public async Task<string> InsertAccountAsync(IAccountInfoData account)
        {
            var table = connection.GetTable<AccountInfoData>();
            await table
                .InsertOrUpdateAsync(() => new AccountInfoData()
                {
                    Id = account.Id,
                    Username = account.Username,
                    Password = account.Password,
                    Email = account.Email,
                    Token = account.Token,
                    LastLogin = account.LastLogin,
                    Created = account.Created,
                    Updated = account.Updated,
                    IsAdmin = account.IsAdmin,
                    IsGuest = account.IsGuest,
                    DeviceId = account.DeviceId,
                    DeviceName = account.DeviceName,
                    IsEmailConfirmed = account.IsEmailConfirmed,
                    IsBanned = account.IsBanned
                },
                (t) => new AccountInfoData()
                {
                    Id = account.Id,
                    Username = account.Username,
                    Password = account.Password,
                    Email = account.Email,
                    Token = account.Token,
                    LastLogin = account.LastLogin,
                    Created = account.Created,
                    Updated = account.Updated,
                    IsAdmin = account.IsAdmin,
                    IsGuest = account.IsGuest,
                    DeviceId = account.DeviceId,
                    DeviceName = account.DeviceName,
                    IsEmailConfirmed = account.IsEmailConfirmed,
                    IsBanned = account.IsBanned
                });

            await InsertOrUpdateExtraProperties(account.Id, account.ExtraProperties);

            return account.Id;
        }

        public async Task InsertOrUpdateExtraProperties(string accountId, Dictionary<string, string> properties)
        {
            if (properties.Count > 0)
            {
                var extraPropertiesTable = connection.GetTable<ExtraPropertyData>();

                foreach (var property in properties)
                {
                    await extraPropertiesTable
                    .InsertOrUpdateAsync(() => new ExtraPropertyData()
                    {
                        AccountId = accountId,
                        PropertyKey = property.Key,
                        PropertyValue = property.Value
                    },
                    (t) => new ExtraPropertyData()
                    {
                        AccountId = accountId,
                        PropertyKey = property.Key,
                        PropertyValue = property.Value
                    });
                }
            }
        }

        public async Task<bool> UpdateAccountAsync(IAccountInfoData account)
        {
            account.Updated = DateTime.UtcNow;
            await InsertAccountAsync(account);
            return true;
        }
    }
}
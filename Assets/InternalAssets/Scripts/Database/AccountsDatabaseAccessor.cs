using MasterServerToolkit.MasterServer;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace Assets.InternalAssets.Scripts.Database
{
    public class AccountsDatabaseAccessor : IAccountsDatabaseAccessor
    {

        public MstProperties CustomProperties { get; private set; } = new MstProperties();
        public MasterServerToolkit.Logging.Logger Logger { get; set; }

        private string baseUrl = "http://46.19.66.180:5071/api";

        public AccountsDatabaseAccessor()
        {

        }

        public IAccountInfoData CreateAccountInstance()
        {
            return new AccountInfoData();
        }

        public async Task<IAccountInfoData> GetAccountByIdAsync(string id)
        {
            return await GetAccountByUrl(baseUrl + $"/users/{id}");
        }

        public async Task<IAccountInfoData> GetAccountByUsernameAsync(string username)
        {
            return await GetAccountByUrl(CreateUrl(new List<(string, string)> { ("username", username) }));
        }

        public async Task<IAccountInfoData> GetAccountByTokenAsync(string token)
        {
            return await GetAccountByUrl(CreateUrl(new List<(string, string)> { ("token", token) }));
        }

        public async Task<IAccountInfoData> GetAccountByEmailAsync(string email)
        {
            return await GetAccountByUrl(CreateUrl(new List<(string, string)> { ("email", email) }));
        }

        public async Task<IAccountInfoData> GetAccountByDeviceIdAsync(string deviceId)
        {
            return await GetAccountByUrl(CreateUrl(new List<(string, string)> { ("deviceId", deviceId) }));
        }

        public Task<IAccountInfoData> GetAccountByExtraPropertyAsync(string propertyKey, string propertyValue)
        {
            return null;
        }

        private async Task<Dictionary<string, string>> GetExtraPropertiesAsync(string accountId)
        {
            return null;
            //return await Task.Run(() =>
            //{
            // return extraPropertiesCollection.Find(i => i.AccountId == accountId).ToDictionary(i => i.PropertyKey, i => i.PropertyValue);
            //});
        }

        public async Task<bool> CheckEmailConfirmationCodeAsync(string email, string code)
        {
            throw new NotSupportedException();
        }

        public async Task<bool> CheckPasswordResetCodeAsync(string email, string code)
        {
            throw new NotSupportedException();
        }

        public async Task SavePasswordResetCodeAsync(IAccountInfoData account, string code)
        {
            throw new NotSupportedException();
        }

        public async Task SaveEmailConfirmationCodeAsync(string email, string code)
        {
            throw new NotSupportedException();
        }

        public async Task<string> InsertAccountAsync(IAccountInfoData account)
        {
            string username = account.Username.Trim();
            string email = account.Email.Trim();
            IAccountInfoData existingAccount;

            // Check username duplicate
            if (!string.IsNullOrEmpty(username))
            {
                existingAccount = await GetAccountByUsernameAsync(username);

                if (existingAccount != null)
                {
                    throw new Exception($"User with username \"{username}\" already exists");
                }
            }
          
            // Check email duplicate
            if (!string.IsNullOrEmpty(email))
            {
                existingAccount = await GetAccountByEmailAsync(email);

                if (existingAccount != null)
                {
                    throw new Exception($"User with email \"{email}\" already exists");
                }
            }

            return await Task.Run(async () =>
            {
                using var client = new HttpClient();
                var json = JsonConvert.SerializeObject(account);
                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(baseUrl + "/users", stringContent);
                var str = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<int>(str).ToString();
            });
        }

        public async Task<bool> InsertOrUpdateTokenAsync(IAccountInfoData account, string token)
        {
            throw new NotSupportedException();
        }

        private async Task InsertOrUpdateExtraProperties(string accountId, Dictionary<string, string> properties)
        {
            throw new NotSupportedException();
        }

        public async Task<bool> UpdateAccountAsync(IAccountInfoData account)
        {
            using var client = new HttpClient();

            var json = JsonConvert.SerializeObject(account);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(baseUrl + "/users", stringContent);
            return response.IsSuccessStatusCode;
        }

        private async Task<IAccountInfoData> GetAccountByUrl(string url)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {                
                    var str = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<AccountInfoData>(str);
                }
            }

            return null;
        }

        private string CreateUrl(List<(string, string)> parameters)
        {
            string url = baseUrl + "/users?";

            for (int i = 0; i < parameters.Count; i++)
            {
                url += parameters[i].Item1 + "=" + parameters[i].Item2;
                if (i != parameters.Count - 1)
                    url += "&";
            }

            return url;
        }

        public void Dispose()
        {
            CustomProperties?.Clear();
            //  database?.Dispose();
        }
    }
}

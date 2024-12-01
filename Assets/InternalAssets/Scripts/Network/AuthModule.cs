using MasterServerToolkit.Json;
using MasterServerToolkit.MasterServer;
using MasterServerToolkit.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.App.Scripts.Network
{
    public class AuthModule : BaseServerModule
    {
        #region INSPECTOR

        [Header("Settings"), SerializeField, Tooltip("Min number of username characters. Will not be used in guest username")]
        protected int usernameMinChars = 4;

        [SerializeField, Tooltip("Max number of username characters. Will not be used in guest username")]
        protected int usernameMaxChars = 12;

        [SerializeField, Tooltip("Min number of user password characters")]
        protected int userPasswordMinChars = 8;

        [SerializeField, TextArea(3, 10)]
        protected string emailAddressValidationTemplate = @"^[a-z0-9][-a-z0-9._]+@([-a-z0-9]+\.)+[a-z]{2,5}$";

        [Tooltip("Database accessor factory that helps to create integration with accounts db"), SerializeField]
        protected IAccountsDatabaseAccessor authDatabaseAccessor;

        #endregion

        /// <summary>
        /// Collection of users who are currently logged in by user id
        /// </summary>
        protected ConcurrentDictionary<string, IUserPeerExtension> loggedInUsers { get; set; } = new ConcurrentDictionary<string, IUserPeerExtension>();

        /// <summary>
        /// Collection of users who are currently logged in
        /// </summary>
        public IEnumerable<IUserPeerExtension> LoggedInUsers => loggedInUsers.Values;

        /// <summary>
        /// Invoked, when user logedin
        /// </summary>
        public event UserLoggedInEventHandlerDelegate OnUserLoggedInEvent;

        /// <summary>
        /// Invoked, when user logs out
        /// </summary>
        public event UserLoggedOutEventHandlerDelegate OnUserLoggedOutEvent;

        /// <summary>
        /// Invoked, when user successfully registers an account
        /// </summary>
        public event UserRegisteredEventHandlerDelegate OnUserRegisteredEvent;

        /// <summary>
        /// Invoked, when user successfully confirms his e-mail
        /// </summary>
        public event UserEmailConfirmedEventHandlerDelegate OnUserEmailConfirmedEvent;

        protected override void Awake()
        {
            base.Awake();

            // Optional dependancy to CensorModule
            AddOptionalDependency<CensorModule>();
        }

        protected virtual void OnValidate()
        {
            if (usernameMaxChars <= usernameMinChars)
                usernameMaxChars = usernameMinChars + 1;
        }

        public override void Initialize(IServer server)
        {
            if (authDatabaseAccessor == null)
            {
                logger.Fatal($"Database accessor was not found in {GetType().Name}");
                return;
            }

            server.RegisterMessageHandler(MstOpCodes.SignIn, SignInMessageHandler);
            server.RegisterMessageHandler(MstOpCodes.SignUp, SignUpMessageHandler);
            server.RegisterMessageHandler(MstOpCodes.SignOut, SignOutMessageHandler);
        }

        public override MstJson JsonInfo()
        {
            var data = base.JsonInfo();

            try
            {
                data.AddField("loggedInUsers", LoggedInUsers.Count());
                data.AddField("minUsernameLength", usernameMinChars);
                data.AddField("minPasswordLength", userPasswordMinChars);
            }
            catch (Exception e)
            {
                data.AddField("error", e.ToString());
            }

            return data;
        }

        public override MstProperties Info()
        {
            MstProperties info = base.Info();

            info.Add("Logged In Users", LoggedInUsers.Count());
            info.Add("Min Username Length", usernameMinChars);
            info.Add("Min Password Length", userPasswordMinChars);

            return info;
        }

        /// <summary>
        /// Get logged in user by Username
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public IUserPeerExtension GetLoggedInUserByUsername(string username)
        {
            return loggedInUsers.Values.Where(i => i.Username == username).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool TryGetLoggedInUserByUsername(string username, out IUserPeerExtension user)
        {
            user = GetLoggedInUserByUsername(username);
            return user != null;
        }

        /// <summary>
        /// Get logged in user by Username
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public IUserPeerExtension GetLoggedInUserByEmail(string email)
        {
            return loggedInUsers.Values.Where(i => i.Account != null && i.Account.Email == email).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool TryGetLoggedInUserByEmail(string email, out IUserPeerExtension user)
        {
            user = GetLoggedInUserByEmail(email);
            return user != null;
        }

        /// <summary>
        /// Get logged in user by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IUserPeerExtension GetLoggedInUserById(string id)
        {
            loggedInUsers.TryGetValue(id, out IUserPeerExtension user);
            return user;
        }

        /// <summary>
        /// Get logged in user by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool TryGetLoggedInUserById(string id, out IUserPeerExtension user)
        {
            user = GetLoggedInUserById(id);
            return user != null;
        }

        /// <summary>
        /// Get logged in users by their ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IEnumerable<IUserPeerExtension> GetLoggedInUsersByIds(string[] ids)
        {
            List<IUserPeerExtension> list = new List<IUserPeerExtension>();

            foreach (string id in ids)
            {
                if (TryGetLoggedInUserById(id, out IUserPeerExtension user))
                {
                    list.Add(user);
                }
            }

            return list;
        }

        /// <summary>
        /// Check if given user is logged in
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool IsUserLoggedInByUsername(string username)
        {
            var user = GetLoggedInUserByUsername(username);
            return user != null;
        }

        /// <summary>
        /// Check if given user is logged in
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool IsUserLoggedInById(string id)
        {
            return !string.IsNullOrEmpty(id) && loggedInUsers.ContainsKey(id);
        }

        /// <summary>
        /// Create instance of <see cref="UserPeerExtension"/>
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public virtual IUserPeerExtension CreateUserPeerExtension(IPeer peer)
        {
            return new UserPeerExtension(peer);
        }

        /// <summary>
        /// Fired when any user disconected from server
        /// </summary>
        /// <param name="peer"></param>
        protected virtual void OnUserDisconnectedEventListener(IPeer peer)
        {
            peer.OnConnectionCloseEvent -= OnUserDisconnectedEventListener;

            var extension = peer.GetExtension<IUserPeerExtension>();

            if (extension == null)
            {
                return;
            }

            loggedInUsers.TryRemove(extension.UserId, out _);

            OnUserLoggedOutEvent?.Invoke(extension);
        }

        /// <summary>
        /// Check if Username is valid. Whether it is not empty or has no white spaces
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        protected virtual bool IsUsernameValid(string username)
        {
            string lowerUserName = username?.ToLower();

            if (string.IsNullOrEmpty(lowerUserName))
            {
                return false;
            }

            if (lowerUserName.Contains(" "))
            {
                return false;
            }

            if ((username.Length < usernameMinChars || username.Length > usernameMaxChars))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if Email is valid
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        protected virtual bool IsEmailValid(string email)
        {
            return !string.IsNullOrEmpty(email.Trim()) && Regex.IsMatch(email, emailAddressValidationTemplate);
        }

        /// <summary>
        /// Check if password valid
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual bool IsPasswordValid(string password)
        {
            return !string.IsNullOrEmpty(password.Trim()) && password.Length >= userPasswordMinChars;
        }

        #region MESSAGE HANDLERS

        /// <summary>
        /// Handles account registration request
        /// </summary>
        /// <param name="message"></param>
        protected virtual void SignUpMessageHandler(IIncomingMessage message)
        {
            try
            {
                // Get peer extension
                var userExtension = message.Peer.GetExtension<IUserPeerExtension>();

                // If user is logged in
                bool isLoggedIn = userExtension != null;

                // If user is already logged in and he is not a guest
                if (isLoggedIn && userExtension.Account.IsGuest == false)
                {
                    logger.Error($"Player {userExtension.Account.Username} is already logged in");
                    message.Respond(ResponseStatus.Failed);
                    return;
                }

                // Get security extension
                var securityExt = message.Peer.GetExtension<SecurityInfoPeerExtension>();

                // Check if Aes key not presented
                if (string.IsNullOrEmpty(securityExt.AesKey))
                {
                    // There's no aesKey that client and master agreed upon
                    logger.Error($"No AES key found for client {message.Peer.Id}");
                    message.Respond(ResponseStatus.Unauthorized);
                    return;
                }

                // Get encrypted data from message
                var encryptedData = message.AsBytes();

                // Let's decrypt it with our AES key
                var decryptedBytesData = Mst.Security.DecryptAES(encryptedData, securityExt.AesKey);

                // Parse our data to user creadentials
                var userCredentials = MstProperties.FromBytes(decryptedBytesData);

                string userName = userCredentials.AsString(MstDictKeys.USER_NAME);
                string userPassword = userCredentials.AsString(MstDictKeys.USER_PASSWORD);
                string userEmail = userCredentials.AsString(MstDictKeys.USER_EMAIL).ToLower();

                // Check if length of our password is valid
                if (IsPasswordValid(userPassword) == false)
                {
                    logger.Error($"Invalid password [{userPassword}]");
                    message.Respond("Invalid password", ResponseStatus.Invalid);
                    //message.Respond(ResponseStatus.Invalid);
                    return;
                }

                // Check if username is valid
                if (IsUsernameValid(userName) == false)
                {
                    logger.Error($"Invalid username [{userName}]");
                    message.Respond("Invalid username", ResponseStatus.Invalid);
                    //message.Respond(ResponseStatus.Invalid);
                    return;
                }


                // Check if email is valid
                if (IsEmailValid(userEmail) == false)
                {
                    logger.Error($"Invalid email [{userEmail}]");
                    message.Respond("Invalid email", ResponseStatus.Invalid);
                    //message.Respond(ResponseStatus.Invalid);
                    return;
                }


                // Create account instance
                var userAccount = isLoggedIn ? userExtension.Account : authDatabaseAccessor.CreateAccountInstance();
                userAccount.Username = userName;
                userAccount.Email = userEmail;
                userAccount.IsGuest = false;
                userAccount.Password = Mst.Security.CreateHash(userPassword);

                // Let's set user email as confirmed if confirmation is not required by default
                //userAccount.IsEmailConfirmed = !emailConfirmRequired;

                _ = Task.Run(async () =>
                {
                    if (isLoggedIn)
                    {
                        // Insert new account ot DB
                        await authDatabaseAccessor.UpdateAccountAsync(userAccount);
                    }
                    else
                    {
                        // Insert new account ot DB
                        await authDatabaseAccessor.InsertAccountAsync(userAccount);
                    }
                });

                message.Respond(ResponseStatus.Success);

                OnUserRegisteredEvent?.Invoke(message.Peer, userAccount);
            }
            catch (Exception e)
            {
                logger.Error(e);
                message.Respond(ResponseStatus.Error);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        protected virtual void SignOutMessageHandler(IIncomingMessage message)
        {
            try
            {
                var userExtension = message.Peer.GetExtension<IUserPeerExtension>();

                if (userExtension == null || userExtension.Account == null)
                    return;

                loggedInUsers.TryRemove(userExtension.UserId, out _);

                message.Peer.Disconnect("Signed out");
            }
            // If we got another exception
            catch (Exception e)
            {
                logger.Error(e);
                message.Respond(ResponseStatus.Error);
            }
        }

        /// <summary>
        /// Handles a request to log in
        /// </summary>
        /// <param name="message"></param>
        protected virtual async void SignInMessageHandler(IIncomingMessage message)
        {
            try
            {
                // Get security extension of a peer
                var securityExt = message.Peer.GetExtension<SecurityInfoPeerExtension>();

                // Check if Aes key not presented
                if (string.IsNullOrEmpty(securityExt.AesKey))
                {
                    // There's no aesKey that client and master agreed upon
                    logger.Error($"No AES key found for client {message.Peer.Id}");
                    message.Respond(ResponseStatus.Unauthorized);
                    return;
                }

                // Get excrypted data
                var encryptedData = message.AsBytes();

                // Decrypt data
                var decryptedBytesData = Mst.Security.DecryptAES(encryptedData, securityExt.AesKey);

                // Parse user credentials
                var userCredentials = MstProperties.FromBytes(decryptedBytesData);

                // Let's run auth factory
                IAccountInfoData userAccount = null;//await RunAuthFactory(message.Peer, userCredentials, message);

                if (userAccount == null)
                {
                    logger.Error($"Account for client {message.Peer.Id} not found!");
                    message.Respond(ResponseStatus.NotFound);
                    return;
                }

                // Setup auth extension
                var userExtension = message.Peer.AddExtension(CreateUserPeerExtension(message.Peer));
                userExtension.Account = userAccount;

                // Listen to disconnect event
                userExtension.Peer.OnConnectionCloseEvent += OnUserDisconnectedEventListener;

                // Add to lookup of logged in users
                loggedInUsers.TryAdd(userExtension.UserId, userExtension);

                // Send response to logged in user
                message.Respond(userExtension.CreateAccountInfoPacket(), ResponseStatus.Success);

                // Trigger the login event
                OnUserLoggedInEvent?.Invoke(userExtension);
            }
            // If we got another exception
            catch (Exception e)
            {
                logger.Error(e);
                message.Respond(ResponseStatus.Error);
            }
        }

        /// <summary>
        /// Signs in user with his login and password
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        protected virtual async Task<IAccountInfoData> SignInWithLoginAndPassword(IPeer peer, MstProperties userCredentials, IIncomingMessage message)
        {
            // Trying to get user extension from peer
            var userPeerExtension = peer.GetExtension<IUserPeerExtension>();

            // If user peer has IUserPeerExtension means this user is already logged in
            if (userPeerExtension != null)
            {
                logger.Debug($"User {peer.Id} trying to login, but he is already logged in");
                message.Respond(ResponseStatus.Failed);
                return null;
            }

            // Get username
            var userName = userCredentials.AsString(MstDictKeys.USER_NAME);

            // Get user password
            var userPassword = userCredentials.AsString(MstDictKeys.USER_PASSWORD);

            // If another session found
            if (IsUserLoggedInByUsername(userName))
            {
                logger.Error($"Another user with {userName} is already logged in");
                message.Respond(ResponseStatus.Failed);
                return null;
            }

            // Get account by its username
            IAccountInfoData userAccount = await authDatabaseAccessor.GetAccountByUsernameAsync(userName);

            if (userAccount == null)
            {
                logger.Error($"No account with username {userName} found for client {message.Peer.Id}");
                message.Respond(ResponseStatus.NotFound);
                return null;
            }

            if (!Mst.Security.ValidatePassword(userPassword, userAccount.Password))
            {
                logger.Error($"Invalid credentials for client {message.Peer.Id}");
                message.Respond(ResponseStatus.Invalid);
                return null;
            }

            return userAccount;
        }

        #endregion
    }
}

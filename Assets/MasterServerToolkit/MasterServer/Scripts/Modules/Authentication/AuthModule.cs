using MasterServerToolkit.Json;
using MasterServerToolkit.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace MasterServerToolkit.MasterServer
{
    public delegate void UserLoggedInEventHandlerDelegate(IUserPeerExtension user);
    public delegate void UserLoggedOutEventHandlerDelegate(IUserPeerExtension user);
    public delegate void UserRegisteredEventHandlerDelegate(IPeer peer, IAccountInfoData account);
    public delegate void UserEmailConfirmedEventHandlerDelegate(IAccountInfoData account);
    public delegate void UserAccountUpdatedEventHandlerDelegate(IAccountInfoData account);
    public delegate void UsernameChangedEventHandlerDelegate(string oldUsername, string newUsername);

    /// <summary>
    /// Authentication module, which handles logging in and registration of accounts
    /// </summary>
    public class AuthModule : BaseServerModule
    {
        #region INSPECTOR

        [Header("Settings"), SerializeField, Tooltip("Min number of username characters. Will not be used in guest username")]
        protected int usernameMinChars = 4;

        [SerializeField, Tooltip("Max number of username characters. Will not be used in guest username")]
        protected int usernameMaxChars = 12;

        [SerializeField, Tooltip("Min number of user password characters")]
        protected int userPasswordMinChars = 8;

        [SerializeField, Tooltip("Whether or not to use email confirmation when sign up")]
        protected bool emailConfirmRequired = true;

        [Header("Guest Settings")]
        [SerializeField, Tooltip("If true, players will be able to log in as guests")]
        protected bool allowGuestLogin = true;

        [SerializeField, Tooltip("Guest names will start with this prefix")]
        protected string guestPrefix = "user_";

        [Header("Permission Settings"), SerializeField, Tooltip("How many days token will be valid before expire. The token will also be updated each login")]
        protected byte tokenExpiresInDays = 60;

        [SerializeField, Tooltip("Minimal permission level, required to retrieve peer account information")]
        protected int getPeerDataPermissionsLevel = 0;

        [Header("E-Mail Settings"), SerializeField]
        protected Mailer mailer;

        [SerializeField, TextArea(3, 10)]
        protected string emailAddressValidationTemplate = @"^[a-z0-9][-a-z0-9._]+@([-a-z0-9]+\.)+[a-z]{2,5}$";

        [Header("Security"), SerializeField, Tooltip("Secret code to create user auth token. Change it for your own project")]
        protected string tokenSecret = "t0k9n-$ecr9t";

        [Header("Generic"), SerializeField, Tooltip("Min number of characters the service code must contain")]
        protected int serviceCodeMinChars = 6;

        /// <summary>
        /// Database accessor factory that helps to create integration with accounts db
        /// </summary>
        [Tooltip("Database accessor factory that helps to create integration with accounts db"), SerializeField]
        protected DatabaseAccessorFactory databaseAccessorFactory;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        protected IAccountsDatabaseAccessor authDatabaseAccessor;

        /// <summary>
        /// Censor module for bad words checking :)
        /// </summary>
        protected CensorModule censorModule;

        /// <summary>
        /// Collection of users who are currently logged in by user id
        /// </summary>
        protected ConcurrentDictionary<string, IUserPeerExtension> loggedInUsers { get; set; } = new ConcurrentDictionary<string, IUserPeerExtension>();

        /// <summary>
        /// Collection of users who are currently logged in
        /// </summary>
        public IEnumerable<IUserPeerExtension> LoggedInUsers => loggedInUsers.Values;

        /// <summary>
        /// 
        /// </summary>
        public IAccountsDatabaseAccessor DatabaseAccessor
        {
            get => authDatabaseAccessor;
            set => authDatabaseAccessor = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public DatabaseAccessorFactory DatabaseAccessorFactory
        {
            get => databaseAccessorFactory;
            set => databaseAccessorFactory = value;
        }

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

            if (tokenExpiresInDays < 0)
                tokenExpiresInDays = 1;
        }

        public override void Initialize(IServer server)
        {
            if (databaseAccessorFactory != null)
                databaseAccessorFactory.CreateAccessors();

            authDatabaseAccessor = Mst.Server.DbAccessors.GetAccessor<IAccountsDatabaseAccessor>();

            if (authDatabaseAccessor == null)
            {
                logger.Fatal($"Account database implementation was not found in {GetType().Name}");
            }

            censorModule = server.GetModule<CensorModule>();

            // Set handlers
            server.RegisterMessageHandler(MstOpCodes.SignIn, SignInMessageHandler);
            server.RegisterMessageHandler(MstOpCodes.SignUp, SignUpMessageHandler);
            server.RegisterMessageHandler(MstOpCodes.SignOut, SignOutMessageHandler);

            server.RegisterMessageHandler(MstOpCodes.GetPasswordResetCode, GetPasswordResetCodeMessageHandler);
            server.RegisterMessageHandler(MstOpCodes.ChangePassword, ChangePasswordMessageHandler);

            server.RegisterMessageHandler(MstOpCodes.GetEmailConfirmationCode, GetEmailConfirmationCodeMessageHandler);
            server.RegisterMessageHandler(MstOpCodes.ConfirmEmail, ConfirmEmailMessageHandler);

            server.RegisterMessageHandler(MstOpCodes.GetPeerAccountInfo, GetPeerAccountInfoMessageHandler);
        }

        public override MstJson JsonInfo()
        {
            var data = base.JsonInfo();

            try
            {
                data.AddField("loggedInUsers", LoggedInUsers.Count());
                data.AddField("allowGuests", allowGuestLogin);
                data.AddField("guestNamePrefix", guestPrefix);
                data.AddField("emailConfirmRequired", emailConfirmRequired);
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
            info.Add("Allow Guests", allowGuestLogin);
            info.Add("Guest Name Prefix", guestPrefix);
            info.Add("Email Confirm", emailConfirmRequired);
            info.Add("Min Username Length", usernameMinChars);
            info.Add("Min Password Length", userPasswordMinChars);

            return info;
        }

        /// <summary>
        /// Generates guest username
        /// </summary>
        /// <returns></returns>
        protected virtual string GenerateGuestUsername()
        {
            string prefix = string.IsNullOrEmpty(guestPrefix) ? "user#" : guestPrefix;
            return $"{prefix}{Mst.Helper.CreateFriendlyId()}";
        }

        /// <summary>
        /// Notify when user logged in
        /// </summary>
        /// <param name="user"></param>
        public virtual void NotifyOnUserLoggedInEvent(IUserPeerExtension user)
        {
            OnUserLoggedInEvent?.Invoke(user);
        }

        /// <summary>
        /// Notify when user logged out
        /// </summary>
        /// <param name="user"></param>
        public virtual void NotifyOnUserLoggedOutEvent(IUserPeerExtension user)
        {
            OnUserLoggedOutEvent?.Invoke(user);
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
        /// Check if given peer has permission to get peer info
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public bool HasGetPeerInfoPermissions(IPeer peer)
        {
            var extension = peer.GetExtension<SecurityInfoPeerExtension>();
            return extension.PermissionLevel >= getPeerDataPermissionsLevel;
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

            if (censorModule != null && censorModule.HasCensoredWord(username))
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected virtual bool IsTokenExpired(string token)
        {
            if (string.IsNullOrEmpty(token))
                return true;

            string decryptedToken = Mst.Security.DecryptStringAES(token, tokenSecret);
            char[] chars = new char[] { '/' };
            string[] values = decryptedToken.Split(chars, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length == 3 && DateTime.TryParse(values[2], out DateTime expireDate))
            {
                return DateTime.UtcNow >= expireDate;
            }

            return true;
        }

        /// <summary>
        /// Creates and saves account token
        /// </summary>
        /// <param name="userAccount"></param>
        protected virtual void CreateAccountToken(IAccountInfoData userAccount)
        {
            string dateTime = DateTime.UtcNow.AddDays(tokenExpiresInDays).ToString();
            string token = $"{userAccount.Id}/{userAccount.Username}/{dateTime}";
            token = Mst.Security.EncryptStringAES(token, tokenSecret);

            _ = Task.Run(async () =>
            {
                await authDatabaseAccessor.InsertOrUpdateTokenAsync(userAccount, token);
            });
        }

        #region MESSAGE HANDLERS

        /// <summary>
        /// Handles client's request to change password
        /// </summary>
        /// <param name="message"></param>
        protected virtual async void ChangePasswordMessageHandler(IIncomingMessage message)
        {
            var data = MstProperties.FromBytes(message.AsBytes());

            string code = data.AsString(MstDictKeys.RESET_PASSWORD_CODE);
            string email = data.AsString(MstDictKeys.RESET_PASSWORD_EMAIL);
            string password = data.AsString(MstDictKeys.RESET_PASSWORD);

            if (string.IsNullOrEmpty(code))
            {
                logger.Error("Invalid password change request. Code required");
                message.Respond(ResponseStatus.Invalid);
                return;
            }

            if (IsEmailValid(email) == false)
            {
                logger.Error("Invalid password change request. Email required");
                message.Respond(ResponseStatus.Invalid);
                return;
            }

            if (IsPasswordValid(password) == false)
            {
                logger.Error("Invalid password change request. Wrong length of the password or it is empty");
                message.Respond(ResponseStatus.Invalid);
                return;
            }

            var passwordResetCodeResult = await authDatabaseAccessor.CheckPasswordResetCodeAsync(email, code);

            if (passwordResetCodeResult == false)
            {
                logger.Error("Invalid code provided");
                message.Respond(ResponseStatus.Invalid);
                return;
            }

            IAccountInfoData account;

            if (TryGetLoggedInUserByEmail(email, out IUserPeerExtension user))
            {
                account = user.Account;
            }
            else
            {
                account = await authDatabaseAccessor.GetAccountByEmailAsync(email);
            }

            account.Password = Mst.Security.CreateHash(password);

            _ = Task.Run(async () =>
            {
                await authDatabaseAccessor.UpdateAccountAsync(account);
            });

            message.Respond(ResponseStatus.Success);
        }

        /// <summary>
        /// Handles password reset request
        /// </summary>
        /// <param name="message"></param>
        protected virtual async void GetPasswordResetCodeMessageHandler(IIncomingMessage message)
        {
            var userEmail = message.AsString();
            var userAccount = await authDatabaseAccessor.GetAccountByEmailAsync(userEmail);

            if (userAccount == null)
            {
                logger.Error($"No account found with email {userEmail} for client {message.Peer.Id}");
                message.Respond(ResponseStatus.NotFound);
                return;
            }

            var passwordResetCode = Mst.Helper.CreateRandomAlphanumericString(serviceCodeMinChars);
            await authDatabaseAccessor.SavePasswordResetCodeAsync(userAccount, passwordResetCode);

            StringBuilder emailBody = new StringBuilder();
            emailBody.Append($"<h3>You have requested reset password</h3>");
            emailBody.Append($"<p>Here is your reset code</p>");
            emailBody.Append($"<h1>{passwordResetCode}</h1>");
            emailBody.Append($"<p>Copy this code and paste it to your reset password form</p>");

            bool sentResult = await mailer.SendMailAsync(userAccount.Email, "Password Reset Code", emailBody.ToString());

            if (!sentResult)
            {
                logger.Error($"Couldn't send an activation code to email {userAccount.Email}");
                message.Respond(ResponseStatus.Failed);
                return;
            }

            message.Respond(ResponseStatus.Success);
        }

        /// <summary>
        /// Handles e-mail confirmation request
        /// </summary>
        /// <param name="message"></param>
        protected virtual async void ConfirmEmailMessageHandler(IIncomingMessage message)
        {
            var confirmationCode = message.AsString();
            var userExtension = message.Peer.GetExtension<IUserPeerExtension>();

            if (userExtension == null || userExtension.Account == null)
            {
                message.Respond(ResponseStatus.Unauthorized);
                return;
            }

            if (userExtension.Account.IsGuest)
            {
                logger.Error("Guests cannot confirm e-mails");
                message.Respond(ResponseStatus.Unauthorized);
                return;
            }

            if (userExtension.Account.IsEmailConfirmed)
            {
                message.Respond(ResponseStatus.Success);
                return;
            }

            var confirmationResult = await authDatabaseAccessor.CheckEmailConfirmationCodeAsync(userExtension.Account.Email, confirmationCode);

            if (confirmationResult == false)
            {
                logger.Error("Invalid activation code");
                message.Respond(ResponseStatus.Invalid);
                return;
            }

            // Confirm e-mail
            userExtension.Account.IsEmailConfirmed = true;

            _ = Task.Run(async () =>
            {
                // Update account
                await authDatabaseAccessor.UpdateAccountAsync(userExtension.Account);
            });

            // Respond with success
            message.Respond(ResponseStatus.Success);

            // Invoke the event
            OnUserEmailConfirmedEvent?.Invoke(userExtension.Account);
        }

        /// <summary>
        /// Handles request to get email conformation code
        /// </summary>
        /// <param name="message"></param>
        protected virtual async void GetEmailConfirmationCodeMessageHandler(IIncomingMessage message)
        {
            var userExtension = message.Peer.GetExtension<IUserPeerExtension>();

            if (userExtension == null || userExtension.Account == null)
            {
                message.Respond(ResponseStatus.Unauthorized);
                return;
            }

            if (userExtension.Account.IsGuest)
            {
                logger.Error("Guests cannot confirm e-mails");
                message.Respond(ResponseStatus.Unauthorized);
                return;
            }

            var newEmailConfirmationCode = Mst.Helper.CreateRandomAlphanumericString(serviceCodeMinChars);

            await authDatabaseAccessor.SaveEmailConfirmationCodeAsync(userExtension.Account.Email, newEmailConfirmationCode);

            if (mailer == null)
            {
                logger.Error($"Couldn't send a confirmation code to e-mail {userExtension.Account.Email}");
                message.Respond(ResponseStatus.Failed);
                return;
            }

            StringBuilder emailBody = new StringBuilder();
            emailBody.Append($"<h3>You have requested email activation</h3>");
            emailBody.Append($"<p>Here is your email activation code</p>");
            emailBody.Append($"<h1>{newEmailConfirmationCode}</h1>");
            emailBody.Append($"<p>Copy this code and paste it to your account activation form</p>");

            bool sentResult = await mailer.SendMailAsync(userExtension.Account.Email, "E-mail confirmation", emailBody.ToString());

            if (!sentResult)
            {
                logger.Error($"Couldn't send a confirmation code to e-mail {userExtension.Account.Email}");
                message.Respond(ResponseStatus.Failed);
                return;
            }

            // Respond with success
            message.Respond(ResponseStatus.Success);
        }

        /// <summary>
        /// Handles a request to retrieve account information
        /// </summary>
        /// <param name="message"></param>
        protected virtual void GetPeerAccountInfoMessageHandler(IIncomingMessage message)
        {
            if (!HasGetPeerInfoPermissions(message.Peer))
            {
                message.Respond(ResponseStatus.Unauthorized);
                return;
            }

            var userPeerId = message.AsInt();
            var userPeer = Server.GetPeer(userPeerId);

            if (userPeer == null)
            {
                logger.Error($"Peer with a given Id {userPeerId} is not in the game");
                message.Respond(ResponseStatus.NotFound);
                return;
            }

            var userExtension = userPeer.GetExtension<IUserPeerExtension>();

            if (userExtension == null || userExtension.Account == null)
            {
                logger.Error($"Peer with a given ID {userPeerId} is not authenticated");
                message.Respond(ResponseStatus.NotFound);
                return;
            }

            var userAccount = userExtension.Account;

            var userAccountPacket = new PeerAccountInfoPacket()
            {
                PeerId = userPeerId,
                ExtraProperties = userAccount.ExtraProperties,
                Username = userAccount.Username,
                UserId = userAccount.Id,
                IsGuest = userAccount.IsGuest,
            };

            message.Respond(userAccountPacket, ResponseStatus.Success);
        }

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
                userAccount.IsEmailConfirmed = !emailConfirmRequired;

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

                NotifyOnUserLoggedOutEvent(userExtension);

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
                IAccountInfoData userAccount = await RunAuthFactory(message.Peer, userCredentials, message);

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
        /// This is auth factory to help you to extend auth module without global changes
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        protected virtual Task<IAccountInfoData> RunAuthFactory(IPeer peer, MstProperties userCredentials, IIncomingMessage message)
        {
            // Guest Authentication
            if (userCredentials.Has(MstDictKeys.USER_IS_GUEST))
            {
                return SignInAsGuest(peer, userCredentials, message);
            }
            // Token Authentication
            else if (userCredentials.Has(MstDictKeys.USER_AUTH_TOKEN))
            {
                return SignInWithToken(peer, userCredentials, message);
            }
            // Username / Password authentication
            else if (userCredentials.Has(MstDictKeys.USER_NAME) && userCredentials.Has(MstDictKeys.USER_PASSWORD))
            {
                return SignInWithLoginAndPassword(peer, userCredentials, message);
            }
            // Email authentication
            else if (userCredentials.Has(MstDictKeys.USER_EMAIL))
            {
                return SignInWithEmail(peer, userCredentials, message);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Signs in user with his email as login and send created credentials to user
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        protected virtual async Task<IAccountInfoData> SignInWithEmail(IPeer peer, MstProperties userCredentials, IIncomingMessage message)
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

            // Get user email
            var userEmail = userCredentials.AsString(MstDictKeys.USER_EMAIL);

            // if email is not in valid format
            if (!IsEmailValid(userEmail))
            {
                logger.Error($"Email {userEmail} is invalid");
                message.Respond(ResponseStatus.Invalid);
                return null;
            }

            // If another session found
            if (IsUserLoggedInByUsername(userEmail))
            {
                logger.Error("Another user is already logged in with this account");
                message.Respond(ResponseStatus.Failed);
                return null;
            }

            // Get account by its email
            IAccountInfoData userAccount = await authDatabaseAccessor.GetAccountByEmailAsync(userEmail);

            // Create new password
            string newPassword = Mst.Helper.CreateRandomAlphanumericString(userPasswordMinChars);

            logger.Debug($"Created new password [{newPassword}] for user [{userEmail}]");

            // if no account found let's create it
            if (userAccount == null)
            {
                userAccount = authDatabaseAccessor.CreateAccountInstance();
                userAccount.Username = userEmail;
                userAccount.Email = userEmail;
                userAccount.IsGuest = false;
                userAccount.Password = Mst.Security.CreateHash(newPassword);
                userAccount.IsEmailConfirmed = true;

                // Save account and return its id in DB
                await authDatabaseAccessor.InsertAccountAsync(userAccount);
            }
            // if account found
            else
            {
                userAccount.Password = Mst.Security.CreateHash(newPassword);
            }

            // Let's save user auth token
            CreateAccountToken(userAccount);

            if (mailer == null)
            {
                message.Respond("Couldn't send creadentials to your e-mail. Please contact support", ResponseStatus.Error);
                return null;
            }

            StringBuilder emailBody = new StringBuilder();
            emailBody.Append($"<h3>You have requested sign in by email</h3>");
            emailBody.Append($"<p>Here are your account creadentials</p>");
            emailBody.Append($"<p><b>Username:</b> {userEmail}</p>");
            emailBody.Append($"<p><b>Password:</b> {newPassword}</p>");

            bool sentResult = await mailer.SendMailAsync(userEmail, "Login by Email", emailBody.ToString());

            if (!sentResult)
            {
                logger.Error($"Couldn't send creadentials to e-mail {userEmail}");
                message.Respond(ResponseStatus.Failed);
                return null;
            }

            return userAccount;
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

            // Let's save user auth token
            CreateAccountToken(userAccount);

            return userAccount;
        }

        /// <summary>
        /// Signs in user with auth token
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        protected virtual async Task<IAccountInfoData> SignInWithToken(IPeer peer, MstProperties userCredentials, IIncomingMessage message)
        {
            // Trying to get user extension from peer
            var userPeerExtension = peer.GetExtension<IUserPeerExtension>();

            // If user peer has IUserPeerExtension means this user is already logged in
            if (userPeerExtension != null)
            {
                logger.Warn($"User {peer.Id} trying to login, but he is already logged in");
                message.Respond(ResponseStatus.Failed);
                return null;
            }

            // Get token
            string token = userCredentials.AsString(MstDictKeys.USER_AUTH_TOKEN);

            // If token has expired
            if (IsTokenExpired(token))
            {
                logger.Warn($"Session token has expired");
                message.Respond(ResponseStatus.TokenExpired);
                return null;
            }

            // Get account by token
            IAccountInfoData userAccount = await authDatabaseAccessor.GetAccountByTokenAsync(token);

            // if no account found
            if (userAccount == null)
            {
                logger.Warn($"Session token id not valid");
                message.Respond(ResponseStatus.Invalid);
                return null;
            }

            // If another session found
            if (IsUserLoggedInByUsername(userAccount.Username))
            {
                logger.Error($"Another user with {userAccount.Username} is already logged in");
                message.Respond(ResponseStatus.Failed);
                return null;
            }

            return userAccount;
        }

        /// <summary>
        /// Signs in user as guest using his guest parametars such as device id
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        protected virtual async Task<IAccountInfoData> SignInAsGuest(IPeer peer, MstProperties userCredentials, IIncomingMessage message)
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

            // Check if guest login is allowed
            if (allowGuestLogin == false)
            {
                logger.Error("Guest login is not allowed in this game");
                message.Respond(ResponseStatus.Failed);
                return null;
            }

            // User device Name and Id
            var userDeviceName = userCredentials.AsString(MstDictKeys.USER_DEVICE_NAME);
            var userDeviceId = userCredentials.AsString(MstDictKeys.USER_DEVICE_ID);

            // Create null account
            // Trying to get user account by user device id
            IAccountInfoData userAccount = await authDatabaseAccessor.GetAccountByDeviceIdAsync(userDeviceId);

            // If current client is on the same device
            bool anotherGuestOnTheSameDevice = userAccount != null && IsUserLoggedInById(userAccount.Id);

            // If guest has no account create it
            if (userAccount == null || anotherGuestOnTheSameDevice)
            {
                userAccount = authDatabaseAccessor.CreateAccountInstance();
                userAccount.Username = GenerateGuestUsername();
                userAccount.DeviceId = userDeviceId;
                userAccount.DeviceName = userDeviceName;

                // If guest may save his data and this guest is not on the same device
                if (!anotherGuestOnTheSameDevice)
                {
                    _ = Task.Run(async () =>
                    {
                        // Save account and return its id in DB
                        await authDatabaseAccessor.InsertAccountAsync(userAccount);
                    });
                }
            }

            return userAccount;
        }

        #endregion
    }
}
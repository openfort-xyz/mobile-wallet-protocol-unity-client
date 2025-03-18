using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace MobileWalletProtocol
{
    /// <summary>
    /// Options for creating a new MWP client.
    /// </summary>
    [Serializable]
    public class MWPClientOptions
    {
        [SerializeField]
        AppMetadata m_Metadata;

        [SerializeField]
        Wallet m_Wallet;

        /// <summary>
        /// The metadata of the application.
        /// </summary>
        public AppMetadata Metadata
        {
            get => m_Metadata;
            set => m_Metadata = value;
        }

        /// <summary>
        /// The wallet to use for communication.
        /// </summary>
        public Wallet Wallet
        {
            get => m_Wallet;
            set => m_Wallet = value;
        }
    }

    /// <summary>
    /// A client for interacting using the Mobile Wallet Protocol.
    /// </summary>
    public class MWPClient
    {
        const string k_LibVersion = "1.0.0";
        const string k_AccountsKey = "accounts";
        const string k_ActiveChainStorage = "activeChain";
        const string k_AvailableChainsStorageKey = "availableChains";
        const string k_WalletCapabilitiesStorageKey = "walletCapabilities";

        readonly AppMetadata m_Metadata;
        readonly Wallet m_Wallet;
        readonly KeyManager m_KeyManager;
        readonly string m_StoragePrefix;

        string[] m_Accounts = Array.Empty<string>();
        Chain m_Chain;
        Dictionary<int, Capability> m_Capabilities;

        public MWPClient(MWPClientOptions options)
        {
            m_Metadata = new AppMetadata
            {
                Name = options.Metadata.Name ?? "Dapp",
                LogoUrl = options.Metadata.LogoUrl,
                ChainIds = options.Metadata.ChainIds,
                CustomScheme = Utils.AppendMWPResponsePath(options.Metadata.CustomScheme)
            };

            m_Wallet = options.Wallet;
            m_KeyManager = new KeyManager(m_Wallet);
            m_StoragePrefix = $"{m_Wallet.Name}_MWPClient_";

            m_Chain = new Chain
            {
                Id = options.Metadata.ChainIds?.Length > 0 ? options.Metadata.ChainIds[0] : "0x1"
            };

            var storedAccounts = LoadFromPlayerPrefs<string[]>(k_AccountsKey);
            if (storedAccounts != null)
            {
                m_Accounts = storedAccounts;
            }

            var storedChain = LoadFromPlayerPrefs<Chain>(k_ActiveChainStorage);
            if (storedChain != null)
            {
                m_Chain = storedChain;
            }

            var storedCapabilities = LoadFromPlayerPrefs<Dictionary<int, Capability>>(k_WalletCapabilitiesStorageKey);
            if (storedCapabilities != null)
            {
                m_Capabilities = storedCapabilities;
            }
        }

        /// <summary>
        /// Requests the accounts of the user from the wallet application.
        /// </summary>
        /// <returns>A result containing the accounts, if successful.</returns>
        public async Task<Result<string[]>> EthRequestAccounts()
        {
            if (m_Accounts.Length > 0)
            {
                return Result<string[]>.Success(m_Accounts);
            }

            try
            {
                return Result<string[]>.Success(await EthRequestAccountsInternal());
            }
            catch (Exception e)
            {
                return Result<string[]>.Failure(e.Message);
            }
        }

        async Task<string[]> EthRequestAccountsInternal()
        {
            var requestAccounts = new RequestAccounts()
            {
                Method = "eth_requestAccounts",
                Params = new RequestAccountsParams()
                {
                    AppName = m_Metadata.Name,
                    AppLogoUrl = m_Metadata.LogoUrl
                }
            };

            var request = CreateRequestMessage(requestAccounts);
            var response = await PostRequestToWallet.SendRequestAsync(request, m_Metadata.CustomScheme, m_Wallet);
            var encryptedData = response.Content as EncryptedData;
            var failure = response.Content as SerializedEthereumRpcError;

            if (failure != null)
            {
                throw new Exception(failure.Message);
            }

            var peerPublicKey = Cipher.ImportKeyFromHexString(KeyType.Public, response.Sender);

            m_KeyManager.SetPeerPublicKey(peerPublicKey);

            var decrypted = DecryptResponseMessage(encryptedData);
            var decryptedResponse = Utils.Deserialize<RequestAccountsResponse>(decrypted);

            if (decryptedResponse.Data?.Chains != null)
            {
                var chains = decryptedResponse.Data.Chains
                    .Select(kvp => new Chain
                    {
                        Id = kvp.Key,
                        RpcUrl = kvp.Value
                    })
                    .ToList();

                SaveToPlayerPrefs(k_AvailableChainsStorageKey, chains);
                UpdateChain(m_Chain.Id, chains);
            }

            if (decryptedResponse.Data?.Capabilities != null)
            {
                SaveToPlayerPrefs(k_WalletCapabilitiesStorageKey, decryptedResponse.Data.Capabilities);
            }

            if (decryptedResponse.Result.Error != null)
            {
                throw new Exception(decryptedResponse.Result.Error.Message);
            }

            m_Accounts = decryptedResponse.Result.Value;

            SaveToPlayerPrefs(k_AccountsKey, m_Accounts);

            return m_Accounts;
        }

        public string EthChainId()
        {
            CheckAuthorised();

            return m_Chain.Id;
        }

        public IReadOnlyDictionary<int, Capability> WalletGetCapabilities()
        {
            CheckAuthorised();

            return m_Capabilities;
        }

        /// <summary>
        /// Switches the active chain of the wallet application.
        /// </summary>
        /// <param name="chainId">The ID of the chain to switch to.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result> WalletSwitchEthereumChain(string chainId)
        {
            try
            {
                await WalletSwitchEthereumChainInternal(chainId);
                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Failure(e.Message);
            }
        }

        async Task WalletSwitchEthereumChainInternal(string chainId)
        {
            CheckAuthorised();

            if (UpdateChain(chainId))
            {
                return;
            }

            var result = await SendRequestToPopup<object>(new RPCRequestArguments
            {
                Method = "wallet_switchEthereumChain",
                Params = new object[]
                {
                    new SwitchEthereumChainParams()
                    {
                        ChainId = chainId
                    }
                }
            });

            if (result == null)
            {
                UpdateChain(chainId);
            }
        }

        /// <summary>
        /// Presents a plain text signature challenge to the user.
        /// </summary>
        /// <remarks>
        /// https://docs.metamask.io/wallet/reference/json-rpc-methods/personal_sign/
        /// </remarks>
        /// <param name="personalSignParams">The parameters of the challenge to sign.</param>
        /// <returns>The signed response.</returns>
        public async Task<Result<string>> PersonalSign(PersonalSignParams personalSignParams)
        {
            try
            {
                CheckAuthorised();

                var result = await SendRequestToPopup<string>(new RPCRequestArguments
                {
                    Method = "personal_sign",
                    Params = new object[]
                    {
                        personalSignParams.Challenge,
                        personalSignParams.Address
                    }
                });

                return Result<string>.Success(result);
            }
            catch (Exception e)
            {
                return Result<string>.Failure(e.Message);
            }
        }

        /// <summary>
        /// Signs a transaction using the private key of the account.
        /// </summary>
        /// <remarks>
        /// https://docs.metamask.io/snaps/reference/keyring-api/chain-methods/#eth_signtransaction
        /// </remarks>
        /// <param name="signTransactionParams">The parameters of the transaction to sign.</param>
        /// <returns>The signed transaction.</returns>
        public async Task<Result<EthSignTransactionResult>> EthSignTransaction(EthSignTransactionParams signTransactionParams)
        {
            try
            {
                CheckAuthorised();

                var result = await SendRequestToPopup<EthSignTransactionResult>(new RPCRequestArguments
                {
                    Method = "eth_signTransaction",
                    Params = new object[]
                    {
                        signTransactionParams
                    }
                });

                return Result<EthSignTransactionResult>.Success(result);
            }
            catch (Exception e)
            {
                return Result<EthSignTransactionResult>.Failure(e.Message);
            }
        }

        /// <summary>
        /// Sends a transaction to the network.
        /// </summary>
        /// <remarks>
        /// https://docs.metamask.io/wallet/reference/json-rpc-methods/eth_sendtransaction
        /// </remarks>
        /// <param name="sendTransactionParams">The parameters of the transaction to send.</param>
        /// <returns>The transaction hash, hex encoded.</returns>
        public async Task<Result<string>> EthSendTransaction(EthSendTransactionParams sendTransactionParams)
        {
            try
            {
                CheckAuthorised();

                var result = await SendRequestToPopup<string>(new RPCRequestArguments
                {
                    Method = "eth_sendTransaction",
                    Params = new object[]
                    {
                        sendTransactionParams
                    }
                });

                return Result<string>.Success(result);
            }
            catch (Exception e)
            {
                return Result<string>.Failure(e.Message);
            }
        }

        /// <summary>
        /// Signs a message using the private key of the account.
        /// </summary>
        /// <remarks>
        /// https://docs.metamask.io/snaps/reference/keyring-api/chain-methods/#eth_signtypeddata_v4
        /// </remarks>
        /// <param name="address">The address of the account to sign the message with.</param>
        /// <param name="typedData">The typed data to sign.</param>
        /// <returns>The signature of the message, hex encoded.</returns>
        public async Task<Result<string>> EthSignTypedDataV4(string address, EthSignTypedDataV4Params typedData)
        {
            try
            {
                CheckAuthorised();

                var result = await SendRequestToPopup<string>(new RPCRequestArguments
                {
                    Method = "eth_signTypedData_v4",
                    Params = new object[]
                    {
                        address,
                        typedData
                    }
                });

                return Result<string>.Success(result);
            }
            catch (Exception e)
            {
                return Result<string>.Failure(e.Message);
            }
        }

        /// <summary>
        /// Creates a confirmation asking the user to add the specified chain to the wallet application.
        /// </summary>
        /// <remarks>
        /// https://docs.metamask.io/wallet/reference/json-rpc-methods/wallet_addethereumchain
        /// </remarks>
        /// <param name="chainParams">The parameters of the chain to add.</param>
        /// <returns>null, if the chain is added.</returns>
        public async Task<Result<AddEthereumChainResult>> WalletAddEthereumChain(WalletAddEthereumChainParams chainParams)
        {
            try
            {
                CheckAuthorised();

                var result = await SendRequestToPopup<AddEthereumChainResult>(new RPCRequestArguments
                {
                    Method = "wallet_addEthereumChain",
                    Params = new object[]
                    {
                        chainParams
                    }
                });

                return Result<AddEthereumChainResult>.Success(result);
            }
            catch (Exception e)
            {
                return Result<AddEthereumChainResult>.Failure(e.Message);
            }
        }

        /// <summary>
        /// Requests that the user track the specified token in MetaMask.
        /// </summary>
        /// /// <remarks>
        /// https://docs.metamask.io/wallet/reference/json-rpc-methods/wallet_watchasset
        /// </remarks>
        /// <param name="watchAssetParams">The parameters of the token to track.</param>
        /// <returns>true, if the token was successfully added.</returns>
        public async Task<Result<bool>> WalletWatchAsset(WalletWatchAssetParams watchAssetParams)
        {
            try
            {
                CheckAuthorised();

                var result = await SendRequestToPopup<bool>(new RPCRequestArguments
                {
                    Method = "wallet_watchAsset",
                    Params = new object[]
                    {
                        watchAssetParams
                    }
                });

                return Result<bool>.Success(result);
            }
            catch (Exception e)
            {
                return Result<bool>.Failure(e.Message);
            }
        }

        /// <summary>
        /// Requests that a wallet submits a batch of calls.
        /// </summary>
        /// <remarks>
        /// https://www.eip5792.xyz/reference/sendCalls
        /// </remarks>
        /// <param name="walletSendCallsParams">The parameters of the calls to submit.</param>
        /// <returns>A call bundle identifier.</returns>
        public async Task<Result<string>> WalletSendCalls(WalletSendCallsParams walletSendCallsParams)
        {
            try
            {
                CheckAuthorised();

                var result = await SendRequestToPopup<string>(new RPCRequestArguments
                {
                    Method = "wallet_sendCalls",
                    Params = new object[]
                    {
                        walletSendCallsParams
                    }
                });

                return Result<string>.Success(result);
            }
            catch (Exception e)
            {
                return Result<string>.Failure(e.Message);
            }
        }

        /// <summary>
        /// Requests that a wallet shows information about a given call bundle.
        /// </summary>
        /// <param name="callBundleId">The identifier of the call bundle to show.</param>
        public async Task<Result> WalletShowCallsStatus(string callBundleId)
        {
            try
            {
                CheckAuthorised();

                await SendRequestToPopup(new RPCRequestArguments
                {
                    Method = "wallet_showCallsStatus",
                    Params = new object[]
                    {
                        callBundleId
                    }
                });

                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Failure(e.Message);
            }
        }

        /// <summary>
        /// Request a Wallet to grant permissions in order to execute transactions on the user’s behalf.
        /// </summary>
        /// <remarks>
        /// https://eip.tools/eip/7715
        /// </remarks>
        /// <param name="walletGrantPermissionsParams">The parameters of the permissions to grant.</param>
        /// 
        public async Task<Result<WalletGrantPermissionsResult>> WalletGrantPermissions(WalletGrantPermissionsParams walletGrantPermissionsParams)
        {
            try
            {
                CheckAuthorised();

                var result = await SendRequestToPopup<WalletGrantPermissionsResult>(new RPCRequestArguments
                {
                    Method = "wallet_grantPermissions",
                    Params = new object[]
                    {
                        walletGrantPermissionsParams
                    }
                });

                return Result<WalletGrantPermissionsResult>.Success(result);
            }
            catch (Exception e)
            {
                return Result<WalletGrantPermissionsResult>.Failure(e.Message);
            }
        }

        void CheckAuthorised()
        {
            if (m_Accounts.Length == 0)
            {
                throw StandardErrors.Provider.Unauthorized();
            }
        }

        public void Reset()
        {
            ClearPlayerPrefs();
            m_KeyManager.Clear();
            m_Accounts = Array.Empty<string>();
            m_Chain = new Chain
            {
                Id = m_Metadata.ChainIds?.Length > 0 ? m_Metadata.ChainIds[0] : "0x1"
            };
            m_Capabilities = null;
        }

        async Task<RPCResponseMessage> SendEncryptedRequest(RPCRequestArguments requestArgs)
        {
            var sharedSecret = m_KeyManager.GetSharedSecret();

            if (sharedSecret == null)
            {
                throw StandardErrors.Provider.Unauthorized();
            }

            var serialized = Utils.Serialize(new RPCRequest()
            {
                Action = requestArgs,
                ChainId = Convert.ToUInt64(m_Chain.Id, 16)
            });

            var encrypted = Cipher.Encrypt(sharedSecret, serialized);
            var message = CreateRequestMessage(encrypted);

            return await PostRequestToWallet.SendRequestAsync(message, m_Metadata.CustomScheme, m_Wallet);
        }

        string GetPrefKey(string key) => $"{m_StoragePrefix}{key}";

        T LoadFromPlayerPrefs<T>(string key)
        {
            var prefKey = GetPrefKey(key);
            if (!PlayerPrefs.HasKey(prefKey)) return default;

            var json = PlayerPrefs.GetString(prefKey);
            return Utils.Deserialize<T>(json);
        }

        void SaveToPlayerPrefs<T>(string key, T value)
        {
            var json = Utils.Serialize(value);
            PlayerPrefs.SetString(GetPrefKey(key), json);
            PlayerPrefs.Save();
        }

        void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteKey(GetPrefKey(k_AccountsKey));
            PlayerPrefs.DeleteKey(GetPrefKey(k_ActiveChainStorage));
            PlayerPrefs.DeleteKey(GetPrefKey(k_AvailableChainsStorageKey));
            PlayerPrefs.DeleteKey(GetPrefKey(k_WalletCapabilitiesStorageKey));
            PlayerPrefs.Save();
        }

        async Task SendRequestToPopup(RPCRequestArguments request)
        {
            var response = await SendEncryptedRequest(request);
            var encryptedData = response.Content as EncryptedData;
            var decrypted = DecryptResponseMessage(encryptedData);
            var decryptedResponse = Utils.Deserialize<RPCResponse>(decrypted);

            if (decryptedResponse.Result.Error != null)
            {
                throw new Exception(decryptedResponse.Result.Error.Message);
            }
        }

        async Task<T> SendRequestToPopup<T>(RPCRequestArguments request)
        {
            var response = await SendEncryptedRequest(request);
            var encryptedData = response.Content as EncryptedData;
            var decrypted = DecryptResponseMessage(encryptedData);
            var decryptedResponse = Utils.Deserialize<RPCResponse<T>>(decrypted);

            if (decryptedResponse.Result.Error != null)
            {
                throw new Exception(decryptedResponse.Result.Error.Message);
            }

            return decryptedResponse.Result.Value;
        }

        RPCRequestMessage CreateRequestMessage(object content)
        {
            var publicKey = Cipher.ExportKeyToHexString(
                KeyType.Public,
                m_KeyManager.GetOwnPublicKey()
            );

            return new RPCRequestMessage
            {
                Id = Guid.NewGuid(),
                Sender = publicKey,
                Content = content,
                SdkVersion = k_LibVersion,
                Timestamp = DateTime.UtcNow,
                CallbackUrl = m_Metadata.CustomScheme
            };
        }

        string DecryptResponseMessage(EncryptedData encryptedData)
        {
            var sharedSecret = m_KeyManager.GetSharedSecret();

            if (sharedSecret == null)
            {
                throw StandardErrors.Provider.Unauthorized();
            }

            return Cipher.Decrypt(sharedSecret, encryptedData);
        }

        bool UpdateChain(string chainId, List<Chain> newAvailableChains = null)
        {
            var chains = newAvailableChains ?? LoadFromPlayerPrefs<List<Chain>>(k_AvailableChainsStorageKey);
            var newChain = chains?.FirstOrDefault(c => c.Id == chainId);

            if (newChain == null)
            {
                return false;
            }

            if (!m_Chain.Equals(newChain))
            {
                m_Chain = newChain;
                SaveToPlayerPrefs(k_ActiveChainStorage, newChain);
            }

            return true;
        }
    }
}

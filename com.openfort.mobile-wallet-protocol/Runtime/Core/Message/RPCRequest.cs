using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace MobileWalletProtocol
{
    /// <summary>
    /// Represents the result of an operation.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// The error message if the operation failed.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess => Error == null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class.
        /// </summary>
        /// <param name="error">The error message if the operation failed.</param>
        protected Result(string error = null) { Error = error; }

        /// <summary>
        /// Returns a string representation of the result.
        /// </summary>
        /// <returns>A string representation of the result.</returns>
        public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <returns>A successful result.</returns>
        public static Result Success() => new Result();

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>A failed result.</returns>
        public static Result Failure(string error) => new Result(error);
    }

    /// <summary>
    /// Represents the result of an operation with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public sealed class Result<T> : Result
    {
        /// <summary>
        /// The value of the operation.
        /// </summary>
        public T Value { get; }

        private Result(T value) : base() { Value = value; }
        private Result(string error) : base(error) { Value = default; }

        /// <inheritdoc />
        public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";

        /// <summary>
        /// Creates a successful result with a value.
        /// </summary>
        /// <param name="value">The value of the operation.</param>
        /// <returns>A successful result with a value.</returns>
        public static Result<T> Success(T value) => new Result<T>(value);

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>A failed result.</returns>
        public static new Result<T> Failure(string error) => new Result<T>(error);
    }

    /// <summary>
    /// Represents a request to be sent to the RPC server.
    /// </summary>
    [Preserve]
    public class RPCRequest
    {
        /// <summary>
        /// The method and parameters of the request.
        /// </summary>
        [Preserve]
        public RPCRequestArguments Action { get; set; }

        /// <summary>
        /// The chain ID.
        /// </summary>
        [Preserve]
        public ulong ChainId { get; set; }
    }

    /// <summary>
    /// Represents the arguments of a request to be sent to the RPC server.
    /// </summary>
    [Preserve]
    public class RPCRequestArguments
    {
        /// <summary>
        /// The method to be called.
        /// </summary>
        [Preserve]
        public string Method { get; set; }

        /// <summary>
        /// The parameters of the method.
        /// </summary>
        [Preserve]
        public object[] Params { get; set; }
    }

    /// <summary>
    /// Parameters for the switchEthereumChain method.
    /// </summary>
    /// <remarks>
    /// https://docs.metamask.io/wallet/reference/json-rpc-methods/wallet_switchethereumchain/
    /// </remarks>
    [Preserve]
    class SwitchEthereumChainParams
    {
        /// <summary>
        /// The chain ID to switch to.
        /// </summary>
        [Preserve]
        public string ChainId { get; set; }
    }

    /// <summary>
    /// Parameters for the personal sign method.
    /// </summary>
    /// <remarks>
    /// https://docs.metamask.io/wallet/reference/json-rpc-methods/personal_sign/
    /// </remarks>
    [Preserve]
    public class PersonalSignParams
    {
        [Preserve]
        public string Challenge { get; set; }

        [Preserve]
        public string Address { get; set; }
    }

    /// <summary>
    /// Parameters for the signTransaction method.
    /// </summary>
    /// <remarks>
    /// https://docs.metamask.io/snaps/reference/keyring-api/chain-methods/#eth_signtransaction
    /// </remarks>
    [Preserve]
    public class EthSignTransactionParams
    {
        [Preserve]
        public string Type { get; set; }

        [Preserve]
        public string Nonce { get; set; }

        [Preserve]
        public string To { get; set; }

        [Preserve]
        public string From { get; set; }

        [Preserve]
        public string Value { get; set; }

        [Preserve]
        public string Data { get; set; }

        [Preserve]
        public string GasLimit { get; set; }

        [Preserve]
        public string GasPrice { get; set; }

        [Preserve]
        public string MaxPriorityFeePerGas { get; set; }

        [Preserve]
        public string MaxFeePerGas { get; set; }

        [Preserve]
        public object[] AccessList { get; set; } = Array.Empty<object>();

        [Preserve]
        public string ChainId { get; set; }
    }

    /// <summary>
    /// Result of the signTransaction method.
    /// </summary>
    [Preserve]
    public class EthSignTransactionResult
    {
        /// <summary>
        /// ECDSA Recovery ID.
        /// </summary>
        [Preserve]
        public string V { get; set; }

        /// <summary>
        /// ECDSA signature parameter R.
        /// </summary>
        [Preserve]
        public string R { get; set; }

        /// <summary>
        /// ECDSA signature parameter S.
        /// </summary>
        [Preserve]
        public string S { get; set; }
    }

    /// <summary>
    /// Parameters for the sendTransaction method.
    /// </summary>
    /// <remarks>
    /// https://docs.metamask.io/wallet/reference/json-rpc-methods/eth_sendtransaction
    /// </remarks>
    [Preserve]
    public class EthSendTransactionParams
    {
        [Preserve]
        public string To { get; set; }

        [Preserve]
        public string From { get; set; }

        [Preserve]
        public string Gas { get; set; }

        [Preserve]
        public string Value { get; set; }

        [Preserve]
        public string Data { get; set; }

        [Preserve]
        public string GasPrice { get; set; }
    }

    /// <summary>
    /// Parameters for the signTypedData method.
    /// </summary>
    /// <remarks>
    /// https://docs.metamask.io/snaps/reference/keyring-api/chain-methods/#eth_signtypeddata_v4
    /// </remarks>
    [Preserve]
    public class EthSignTypedDataV4Params
    {
        [Preserve]
        public Dictionary<string, TypedDataField[]> Types { get; set; } = new Dictionary<string, TypedDataField[]>();

        [Preserve]
        public string PrimaryType { get; set; }

        [Preserve]
        public EIP712Domain Domain { get; set; }

        [Preserve]
        public object Message { get; set; }
    }

    /// <summary>
    /// Data field for the signTypedData method.
    /// </summary>
    [Preserve]
    public class TypedDataField
    {
        [Preserve]
        public string Name { get; set; }

        [Preserve]
        public string Type { get; set; }
    }

    /// <summary>
    /// Domain separator values specified in the EIP712Domain type.
    /// </summary>
    [Preserve]
    public class EIP712Domain
    {
        [Preserve]
        public string Name { get; set; }

        [Preserve]
        public string Version { get; set; }

        [Preserve]
        public string ChainId { get; set; }

        [Preserve]
        public string VerifyingContract { get; set; }

        [Preserve]
        public string Salt { get; set; }
    }

    /// <summary>
    /// Parameters for the wallet_addEthereumChain method.
    /// </summary>
    /// <remarks>
    /// https://docs.metamask.io/wallet/reference/json-rpc-methods/wallet_addethereumchain
    /// </remarks>
    [Preserve]
    public class WalletAddEthereumChainParams
    {
        [Preserve]
        public string ChainId { get; set; }

        [Preserve]
        public string ChainName { get; set; }

        [Preserve]
        public string[] RpcUrls { get; set; } = Array.Empty<string>();

        [Preserve]
        public string[] IconUrls { get; set; } = Array.Empty<string>();

        [Preserve]
        public NativeCurrency NativeCurrency { get; set; }

        [Preserve]
        public string[] BlockExplorerUrls { get; set; }

        [Preserve]
        public string Currency { get; set; }
    }

    /// <summary>
    /// Describes the native currency of the chain using the name, symbol, and decimals fields.
    /// </summary>
    [Preserve]
    public class NativeCurrency
    {
        [Preserve]
        public string Name { get; set; }

        [Preserve]
        public string Symbol { get; set; }

        [Preserve]
        public int Decimals { get; set; }
    }

    /// <summary>
    /// The result of the wallet_addEthereumChain method.
    /// </summary>
    [Preserve]
    public class AddEthereumChainResult
    {

    }

    /// <summary>
    /// Parameters for the wallet_watchAsset method.
    /// </summary>
    /// <remarks>
    /// https://docs.metamask.io/wallet/reference/json-rpc-methods/wallet_watchasset
    /// </remarks>
    [Preserve]
    public class WalletWatchAssetParams
    {
        [Preserve]
        public string Type { get; set; }

        [Preserve]
        public WalletWatchAssetOptions Options { get; set; }
    }

    /// <summary>
    /// Options for the wallet_watchAsset method.
    /// </summary>
    [Preserve]
    public class WalletWatchAssetOptions
    {
        [Preserve]
        public string Address { get; set; }

        [Preserve]
        public string Symbol { get; set; }

        [Preserve]
        public int Decimals { get; set; }

        [Preserve]
        public string Image { get; set; }

        [Preserve]
        public string TokenId { get; set; }
    }

    /// <summary>
    /// Parameters for the wallet_sendCalls method.
    /// </summary>
    /// <remarks>
    /// https://www.eip5792.xyz/reference/sendCalls
    /// </remarks>
    [Preserve]
    public class WalletSendCallsParams
    {
        [Preserve]
        public string Version { get; set; }

        [Preserve]
        public string ChainId { get; set; }

        [Preserve]
        public string From { get; set; }

        [Preserve]
        public Call[] Calls { get; set; }

        [Preserve]
        public object Capabilities { get; set; }

    }

    /// <summary>
    /// Call parameters for the wallet_sendCalls method.
    /// </summary>
    [Preserve]
    public class Call
    {
        [Preserve]
        public string To { get; set; }

        [Preserve]
        public string Value { get; set; }

        [Preserve]
        public string Data { get; set; }
    }

    /// <summary>
    /// Parameters for the wallet_grantPermissions method.
    /// </summary>
    /// <remarks>
    /// https://eip.tools/eip/7715
    /// </remarks>
    [Preserve]
    public class WalletGrantPermissionsParams
    {
        [Preserve]
        public string ChainId { get; set; }

        [Preserve]
        public string Address { get; set; }

        [Preserve]
        public long Expiry { get; set; }

        [Preserve]
        public Signer Signer { get; set; }

        [Preserve]
        public Permission[] Permissions { get; set; }
    }

    /// <summary>
    /// Represents signer information
    /// </summary>
    [Preserve]
    public class Signer
    {
        [Preserve]
        public string Type { get; set; }

        [Preserve]
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// Represents a permission
    /// </summary>
    [Preserve]
    public class Permission
    {
        [Preserve]
        public string Type { get; set; }

        [Preserve]
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// Result data for the wallet_grantPermissionsResult method.
    /// </summary>
    [Preserve]
    public class WalletGrantPermissionsResult : WalletGrantPermissionsParams
    {
        [Preserve]
        public string Context { get; set; }

        [Preserve]
        public AccountMeta AccountMeta { get; set; }

        [Preserve]
        public SignerMeta SignerMeta { get; set; }
    }

    /// <summary>
    /// Represents account metadata
    /// </summary>
    [Preserve]
    public class AccountMeta
    {
        [Preserve]
        public string Factory { get; set; }

        [Preserve]
        public string FactoryData { get; set; }
    }

    /// <summary>
    /// Represents signer metadata
    /// </summary>
    [Preserve]
    public class SignerMeta
    {
        [Preserve]
        public string UserOpBuilder { get; set; }

        [Preserve]
        public string DelegationManager { get; set; }
    }
}
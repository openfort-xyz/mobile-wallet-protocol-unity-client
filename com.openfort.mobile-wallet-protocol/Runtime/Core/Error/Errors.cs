using System;
using System.Linq;

namespace MobileWalletProtocol
{
    public class EthereumRpcError : Exception
    {
        public int Code { get; }
        public object ObjectData { get; }

        public EthereumRpcError(int code, string message, object data = null) : base(message)
        {
            if (!message.Any())
                throw new ArgumentException("Message must be a nonempty string.", nameof(message));

            Code = code;
            ObjectData = data;
        }
    }

    public class EthereumProviderError : EthereumRpcError
    {
        public EthereumProviderError(int code, string message, object data = null) 
            : base(code, message, data)
        {
            if (!IsValidEthProviderCode(code))
                throw new ArgumentException("Code must be an integer between 1000 and 4999.", nameof(code));
        }

        private static bool IsValidEthProviderCode(int code)
        {
            return code >= 1000 && code <= 4999;
        }
    }

    public static class StandardErrors
    {
        public static class Rpc
        {
            public static EthereumRpcError Parse(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.Parse, arg);

            public static EthereumRpcError InvalidRequest(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.InvalidRequest, arg);

            public static EthereumRpcError InvalidParams(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.InvalidParams, arg);

            public static EthereumRpcError MethodNotFound(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.MethodNotFound, arg);

            public static EthereumRpcError Internal(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.Internal, arg);

            public static EthereumRpcError Server(ServerErrorOptions opts)
            {
                if (opts == null)
                    throw new ArgumentNullException(nameof(opts));

                if (opts.Code > -32005 || opts.Code < -32099)
                    throw new ArgumentException("Code must be between -32099 and -32005", nameof(opts.Code));

                return GetEthJsonRpcError(opts.Code, opts);
            }

            public static EthereumRpcError InvalidInput(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.InvalidInput, arg);

            public static EthereumRpcError ResourceNotFound(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.ResourceNotFound, arg);

            public static EthereumRpcError ResourceUnavailable(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.ResourceUnavailable, arg);

            public static EthereumRpcError TransactionRejected(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.TransactionRejected, arg);

            public static EthereumRpcError MethodNotSupported(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.MethodNotSupported, arg);

            public static EthereumRpcError LimitExceeded(object arg = null) => 
                GetEthJsonRpcError(StandardErrorCodes.Rpc.LimitExceeded, arg);
        }

        public static class Provider
        {
            public static EthereumProviderError UserRejectedRequest(object arg = null) =>
                GetEthProviderError(StandardErrorCodes.Provider.UserRejectedRequest, arg);

            public static EthereumProviderError Unauthorized(object arg = null) =>
                GetEthProviderError(StandardErrorCodes.Provider.Unauthorized, arg);

            public static EthereumProviderError UnsupportedMethod(object arg = null) =>
                GetEthProviderError(StandardErrorCodes.Provider.UnsupportedMethod, arg);

            public static EthereumProviderError Disconnected(object arg = null) =>
                GetEthProviderError(StandardErrorCodes.Provider.Disconnected, arg);

            public static EthereumProviderError ChainDisconnected(object arg = null) =>
                GetEthProviderError(StandardErrorCodes.Provider.ChainDisconnected, arg);

            public static EthereumProviderError UnsupportedChain(object arg = null) =>
                GetEthProviderError(StandardErrorCodes.Provider.UnsupportedChain, arg);

            public static EthereumProviderError Custom(CustomErrorArg opts)
            {
                if (opts == null)
                    throw new ArgumentNullException(nameof(opts));

                if (string.IsNullOrEmpty(opts.Message))
                    throw new ArgumentException("Message must be a nonempty string", nameof(opts.Message));

                return new EthereumProviderError(opts.Code, opts.Message, opts.Data);
            }
        }

        private static EthereumRpcError GetEthJsonRpcError(int code, object arg)
        {
            var (message, data) = ParseOpts(arg);
            return new EthereumRpcError(code, message ?? ErrorUtils.GetMessageFromCode(code), data);
        }

        private static EthereumProviderError GetEthProviderError(int code, object arg)
        {
            var (message, data) = ParseOpts(arg);
            return new EthereumProviderError(code, message ?? ErrorUtils.GetMessageFromCode(code), data);
        }

        private static (string Message, object Data) ParseOpts(object arg)
        {
            return arg switch
            {
                string message => (message, null),
                IEthereumErrorOptions opts => (opts.Message, opts.Data),
                _ => (null, null)
            };
        }
    }

    public interface IEthereumErrorOptions
    {
        string Message { get; }
        object Data { get; }
    }

    public class ServerErrorOptions : IEthereumErrorOptions
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public class CustomErrorArg : ServerErrorOptions { }
}
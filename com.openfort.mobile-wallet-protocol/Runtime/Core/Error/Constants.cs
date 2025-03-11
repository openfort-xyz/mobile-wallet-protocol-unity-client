using System.Collections.Generic;

namespace MobileWalletProtocol
{
    public static class StandardErrorCodes
    {
        public static class Rpc
        {
            public const int InvalidInput = -32000;
            public const int ResourceNotFound = -32001;
            public const int ResourceUnavailable = -32002;
            public const int TransactionRejected = -32003;
            public const int MethodNotSupported = -32004;
            public const int LimitExceeded = -32005;
            public const int Parse = -32700;
            public const int InvalidRequest = -32600;
            public const int MethodNotFound = -32601;
            public const int InvalidParams = -32602;
            public const int Internal = -32603;
        }

        public static class Provider
        {
            public const int UserRejectedRequest = 4001;
            public const int Unauthorized = 4100;
            public const int UnsupportedMethod = 4200;
            public const int Disconnected = 4900;
            public const int ChainDisconnected = 4901;
            public const int UnsupportedChain = 4902;
        }
    }

    public class ErrorValue
    {
        public string Standard { get; set; }
        public string Message { get; set; }
    }

    public static class ErrorValues
    {
        private static readonly Dictionary<int, ErrorValue> Values = new()
        {
            [-32700] = new() { Standard = "JSON RPC 2.0", Message = "Invalid JSON was received by the server. An error occurred on the server while parsing the JSON text." },
            [-32600] = new() { Standard = "JSON RPC 2.0", Message = "The JSON sent is not a valid Request object." },
            [-32601] = new() { Standard = "JSON RPC 2.0", Message = "The method does not exist / is not available." },
            [-32602] = new() { Standard = "JSON RPC 2.0", Message = "Invalid method parameter(s)." },
            [-32603] = new() { Standard = "JSON RPC 2.0", Message = "Internal JSON-RPC error." },
            [-32000] = new() { Standard = "EIP-1474", Message = "Invalid input." },
            [-32001] = new() { Standard = "EIP-1474", Message = "Resource not found." },
            [-32002] = new() { Standard = "EIP-1474", Message = "Resource unavailable." },
            [-32003] = new() { Standard = "EIP-1474", Message = "Transaction rejected." },
            [-32004] = new() { Standard = "EIP-1474", Message = "Method not supported." },
            [-32005] = new() { Standard = "EIP-1474", Message = "Request limit exceeded." },
            [4001] = new() { Standard = "EIP-1193", Message = "User rejected the request." },
            [4100] = new() { Standard = "EIP-1193", Message = "The requested account and/or method has not been authorized by the user." },
            [4200] = new() { Standard = "EIP-1193", Message = "The requested method is not supported by this Ethereum provider." },
            [4900] = new() { Standard = "EIP-1193", Message = "The provider is disconnected from all chains." },
            [4901] = new() { Standard = "EIP-1193", Message = "The provider is disconnected from the specified chain." },
            [4902] = new() { Standard = "EIP-3085", Message = "Unrecognized chain ID." }
        };

        public static ErrorValue GetValue(int code) => Values.TryGetValue(code, out var value) ? value : null;
    }
}
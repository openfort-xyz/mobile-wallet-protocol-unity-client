using UnityEngine.Scripting;

namespace MobileWalletProtocol
{
    [Preserve]
    class RPCResponse
    {
        [Preserve]
        public RPCResult Result { get; set; }
    }

    [Preserve]
    class RPCResult
    {
        [Preserve]
        public SerializedEthereumRpcError Error { get; set; }
    }

    [Preserve]
    class RPCResponse<T>
    {
        [Preserve]
        public RPCResult<T> Result { get; set; }
    }

    [Preserve]
    class RPCResult<T> : RPCResult
    {
        [Preserve]
        public T Value { get; set; }
    }
}
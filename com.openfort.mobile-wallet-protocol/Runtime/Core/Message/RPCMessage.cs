using System;
using UnityEngine.Scripting;

namespace MobileWalletProtocol
{
    [Preserve]
    class RPCMessage
    {
        [Preserve]
        public Guid Id { get; set; }
        [Preserve]
        public string Sender { get; set; }
        [Preserve]
        public object Content { get; set; }
        [Preserve]
        public DateTime Timestamp { get; set; }
    }

    [Preserve]
    class RPCRequestMessage : RPCMessage
    {
        [Preserve]
        public string SdkVersion { get; set; }
        [Preserve]
        public string CallbackUrl { get; set; }
        [Preserve]
        public string CustomScheme { get; set; }
    }

    [Preserve]
    class RPCResponseMessage : RPCMessage
    {
        [Preserve]
        public Guid RequestId { get; set; }
    }
}
using System;

namespace MobileWalletProtocol
{
    public class Chain : IEquatable<Chain>
    {
        public string Id { get; set; }
        public string RpcUrl { get; set; }

        public bool Equals(Chain other)
        {
            return other != null && Id == other.Id;
        }
    }
}

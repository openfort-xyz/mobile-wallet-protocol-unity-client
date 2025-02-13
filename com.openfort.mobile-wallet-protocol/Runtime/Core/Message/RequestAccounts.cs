using System.Collections.Generic;
using UnityEngine.Scripting;

namespace MobileWalletProtocol
{
    [Preserve]
    class RequestAccounts
    {
        [Preserve]
        public string Method { get; set; }
        [Preserve]
        public RequestAccountsParams Params { get; set; }
    }

    [Preserve]
    class RequestAccountsParams
    {
        [Preserve]
        public string AppName { get; set; }
        [Preserve]
        public string AppLogoUrl { get; set; }
    }

    [Preserve]
    class RequestAccountsResponse : RPCResponse<string[]>
    {
        [Preserve]
        public RequestAccountsData Data { get; set; }
    }

    [Preserve]
    class RequestAccountsData
    {
        [Preserve]
        public Dictionary<string, string> Chains { get; set; }
        [Preserve]
        public Dictionary<string, Capability> Capabilities { get; set; }
    }

    [Preserve]
    public class Capability
    {
        [Preserve]
        public Permissions Permissions { get; set; }
        [Preserve]
        public PaymasterService PaymasterService { get; set; }
        [Preserve]
        public AtomicBatch AtomicBatch { get; set; }
    }

    [Preserve]
    public class Permissions
    {
        [Preserve]
        public bool Supported { get; set; }

        [Preserve]
        public string[] SignerTypes { get; set; }

        [Preserve]
        public string[] KeyTypes { get; set; }

        [Preserve]
        public string[] PermissionTypes { get; set; }
    }

    [Preserve]
    public class PaymasterService
    {
        [Preserve]
        public bool Supported { get; set; }
    }

    [Preserve]
    public class AtomicBatch
    {
        [Preserve]
        public bool Supported { get; set; }
    }
}
using System;
using UnityEngine;

namespace MobileWalletProtocol
{
    public enum WalletType
    {
        Web,
        Native
    }

    [Serializable]
    public class Wallet
    {
        [SerializeField]
        WalletType m_Type;

        [SerializeField]
        string m_Name;

        [SerializeField]
        string m_Scheme;

        [SerializeField]
        string m_IconUrl;

        public WalletType Type
        {
            get => m_Type;
            set => m_Type = value;
        }
    
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public string Scheme
        {
            get => m_Scheme;
            set => m_Scheme = value;
        }

        public string IconUrl
        {
            get => m_IconUrl;
            set => m_IconUrl = value;
        }

        public static Wallet CreateWebWallet(string name, string scheme, string iconUrl = null)
        {
            return new Wallet()
            {
                Type = WalletType.Web,
                Name = name,
                Scheme = scheme,
                IconUrl = iconUrl
            };
        }
    }

    public class StoreUrls
    {
        public string AppStore { get; }
        public string GooglePlay { get; }

        public StoreUrls(string appStore, string googlePlay)
        {
            AppStore = appStore;
            GooglePlay = googlePlay;
        }
    }

    public static class Wallets
    {
        public static readonly Wallet CoinbaseSmartWallet = Wallet.CreateWebWallet(
            name: "Coinbase Smart Wallet",
            scheme: "https://keys.coinbase.com/connect",
            iconUrl: "https://wallet.coinbase.com/assets/images/favicon.ico"
        );
    }
}
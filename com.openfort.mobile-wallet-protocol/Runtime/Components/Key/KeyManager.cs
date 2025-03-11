using UnityEngine;

namespace MobileWalletProtocol
{
    public class StorageItem
    {
        public string StorageKey { get; }
        public KeyType KeyType { get; }

        public StorageItem(string storageKey, KeyType keyType)
        {
            StorageKey = storageKey;
            KeyType = keyType;
        }
    }

    public class KeyManager
    {
        private static readonly StorageItem OWN_PRIVATE_KEY = new StorageItem("ownPrivateKey", KeyType.Private);
        private static readonly StorageItem OWN_PUBLIC_KEY = new StorageItem("ownPublicKey", KeyType.Public);
        private static readonly StorageItem PEER_PUBLIC_KEY = new StorageItem("peerPublicKey", KeyType.Public);

        private readonly string storagePrefix;
        private CryptoKey ownPrivateKey = null;
        private CryptoKey ownPublicKey = null;
        private CryptoKey peerPublicKey = null;
        private CryptoKey sharedSecret = null;

        public KeyManager(Wallet wallet)
        {
            storagePrefix = $"{wallet.Name}_KeyManager_";
        }

        public CryptoKey GetOwnPublicKey()
        {
            LoadKeysIfNeeded();
            return ownPublicKey;
        }

        public CryptoKey GetSharedSecret()
        {
            LoadKeysIfNeeded();
            return sharedSecret;
        }

        public void SetPeerPublicKey(CryptoKey key)
        {
            sharedSecret = null;
            peerPublicKey = key;
            StoreKey(PEER_PUBLIC_KEY, key);
            LoadKeysIfNeeded();
        }

        public void Clear()
        {
            ownPrivateKey = null;
            ownPublicKey = null;
            peerPublicKey = null;
            sharedSecret = null;

            PlayerPrefs.DeleteKey(GetPrefKey(OWN_PUBLIC_KEY.StorageKey));
            PlayerPrefs.DeleteKey(GetPrefKey(OWN_PRIVATE_KEY.StorageKey));
            PlayerPrefs.DeleteKey(GetPrefKey(PEER_PUBLIC_KEY.StorageKey));
            PlayerPrefs.Save();
        }

        private void GenerateKeyPair()
        {
            var newKeyPair = Cipher.GenerateKeyPair();
            ownPrivateKey = newKeyPair.PrivateKey;
            ownPublicKey = newKeyPair.PublicKey;
            StoreKey(OWN_PRIVATE_KEY, newKeyPair.PrivateKey);
            StoreKey(OWN_PUBLIC_KEY, newKeyPair.PublicKey);
        }

        private void LoadKeysIfNeeded()
        {
            if (ownPrivateKey == null)
            {
                ownPrivateKey = LoadKey(OWN_PRIVATE_KEY);
            }

            if (ownPublicKey == null)
            {
                ownPublicKey = LoadKey(OWN_PUBLIC_KEY);
            }

            if (ownPrivateKey == null || ownPublicKey == null)
            {
                GenerateKeyPair();
            }

            if (peerPublicKey == null)
            {
                peerPublicKey = LoadKey(PEER_PUBLIC_KEY);
            }

            if (sharedSecret == null && ownPrivateKey != null && peerPublicKey != null)
            {
                sharedSecret = Cipher.DeriveSharedSecret(ownPrivateKey, peerPublicKey);
            }
        }

        // Storage methods using PlayerPrefs
        private string GetPrefKey(string key) => $"{storagePrefix}{key}";

        private CryptoKey LoadKey(StorageItem item)
        {
            string prefKey = GetPrefKey(item.StorageKey);
            if (!PlayerPrefs.HasKey(prefKey))
            {
                return null;
            }

            string hexString = PlayerPrefs.GetString(prefKey);
            return Cipher.ImportKeyFromHexString(item.KeyType, hexString);
        }

        private void StoreKey(StorageItem item, CryptoKey key)
        {
            string hexString = Cipher.ExportKeyToHexString(item.KeyType, key);
            PlayerPrefs.SetString(GetPrefKey(item.StorageKey), hexString);
            PlayerPrefs.Save();
        }
    }
}

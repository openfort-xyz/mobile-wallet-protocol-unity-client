namespace MobileWalletProtocol
{
    public class CryptoKeyPair
    {
        public CryptoKey PrivateKey { get; set; }
        public CryptoKey PublicKey { get; set; }
    }

    public enum KeyType
    {
        Public,
        Private,
        Secret
    }

    public class CryptoKey
    {
        public KeyType Type { get; set; }
        public Algorithm Algorithm { get; set; }
        public bool Extractable { get; set; }
        public string[] Usages { get; set; }
        public byte[] Key { get; set; }
    }

    public class Algorithm
    {
        public string Name { get; set; }
        public string NamedCurve { get; set; }
        public int? Length { get; set; }
    }

    class EncryptedData
    {
        public byte[] IV { get; set; }
        public byte[] CipherText { get; set; }
    }
}

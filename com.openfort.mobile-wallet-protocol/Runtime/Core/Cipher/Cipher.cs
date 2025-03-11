#if !UNITY_WEBGL || UNITY_EDITOR
#define ENABLE_COMPRESSION
#endif
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace MobileWalletProtocol
{
    static class Cipher
    {
        static readonly byte[] k_ZlibHeader = new byte[]{ 0x78, 0x9c };
        static readonly SecureRandom Random = new SecureRandom();

        public static CryptoKeyPair GenerateKeyPair()
        {
            var curve = NistNamedCurves.GetByName("P-256");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            
            var keyGen = new ECKeyPairGenerator();
            keyGen.Init(new ECKeyGenerationParameters(domainParams, Random));
            
            var keyPair = keyGen.GenerateKeyPair();
            var privateKey = keyPair.Private as ECPrivateKeyParameters;
            var publicKey = keyPair.Public as ECPublicKeyParameters;

            return new CryptoKeyPair
            {
                PrivateKey = new CryptoKey
                {
                    Type = KeyType.Private,
                    Algorithm = new Algorithm
                    {
                        Name = "ECDH",
                        NamedCurve = "P-256"
                    },
                    Extractable = true,
                    Usages = new[] { "deriveKey" },
                    Key = privateKey.D.ToByteArray()
                },
                PublicKey = new CryptoKey
                {
                    Type = KeyType.Public,
                    Algorithm = new Algorithm
                    {
                        Name = "ECDH",
                        NamedCurve = "P-256"
                    },
                    Extractable = true,
                    Usages = Array.Empty<string>(),
                    Key = publicKey.Q.GetEncoded()
                }
            };
        }

        public static CryptoKey DeriveSharedSecret(CryptoKey ownPrivateKey, CryptoKey peerPublicKey)
        {
            var curve = NistNamedCurves.GetByName("P-256");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

            var privateKey = new ECPrivateKeyParameters(new BigInteger(1, ownPrivateKey.Key), domainParams);
            var publicKey = new ECPublicKeyParameters(curve.Curve.DecodePoint(peerPublicKey.Key), domainParams);

            var agreement = new ECDHBasicAgreement();
            agreement.Init(privateKey);
            
            var aesKey = agreement.CalculateAgreement(publicKey).ToByteArrayUnsigned();

            return new CryptoKey
            {
                Type = KeyType.Secret,
                Algorithm = new Algorithm
                {
                    Name = "AES-GCM",
                    Length = 256
                },
                Extractable = false,
                Usages = new[] { "encrypt", "decrypt" },
                Key = aesKey
            };
        }

        public static EncryptedData Encrypt(CryptoKey sharedSecret, string plainText)
        {
            var iv = new byte[12];
            Random.NextBytes(iv);

            var engine = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(sharedSecret.Key), 128, iv);
            engine.Init(true, parameters);

            var bytes = Encoding.UTF8.GetBytes(plainText);
#if ENABLE_COMPRESSION
            bytes = Compress(bytes);
#endif
            var cipherText = new byte[engine.GetOutputSize(bytes.Length)];
            
            var len = engine.ProcessBytes(bytes, 0, bytes.Length, cipherText, 0);
            engine.DoFinal(cipherText, len);

            return new EncryptedData
            {
                IV = iv,
                CipherText = cipherText
            };
        }

        public static string Decrypt(CryptoKey sharedSecret, EncryptedData encryptedData)
        {
            var engine = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(sharedSecret.Key), 128, encryptedData.IV);
            
            engine.Init(false, parameters);

            var bytes = new byte[engine.GetOutputSize(encryptedData.CipherText.Length)];
            var len = engine.ProcessBytes(encryptedData.CipherText, 0, encryptedData.CipherText.Length, bytes, 0);
            
            engine.DoFinal(bytes, len);
#if ENABLE_COMPRESSION
            bytes = Decompress(bytes);
#endif
            return Encoding.UTF8.GetString(bytes);
        }

        public static string ExportKeyToHexString(KeyType type, CryptoKey key)
        {
            byte[] exported;
            if (type == KeyType.Public)
            {
                exported = EncodeECPublicKey(key.Key);
            }
            else
            {
                exported = key.Key;
            }

            return BitConverter.ToString(exported).Replace("-", string.Empty).ToLower();
        }

        public static CryptoKey ImportKeyFromHexString(KeyType type, string hexString)
        {
            var data = HexStringToByteArray(hexString);
            
            byte[] importedKey;
            if (type == KeyType.Public)
            {
                var sequence = (DerSequence)Asn1Object.FromByteArray(data);
                var bitString = (DerBitString)sequence[1];
                importedKey = bitString.GetBytes();
            }
            else
            {
                // Validate private key
                var curve = NistNamedCurves.GetByName("P-256");
                var d = new BigInteger(1, data);
                if (d.CompareTo(BigInteger.One) < 0 || d.CompareTo(curve.N) >= 0)
                {
                    throw new ArgumentException("Invalid private key");
                }
                importedKey = data;
            }

            return new CryptoKey
            {
                Type = type,
                Algorithm = new Algorithm
                {
                    Name = "ECDH",
                    NamedCurve = "P-256"
                },
                Extractable = true,
                Usages = type == KeyType.Private ? new[] { "deriveKey" } : Array.Empty<string>(),
                Key = importedKey
            };
        }

        static byte[] Compress(byte[] data)
        {
            var adler32 = ComputeAdler32(data);

            using (var outputStream = new MemoryStream())
            {
                outputStream.Write(k_ZlibHeader);

                using (var compressionStream = new DeflateStream(outputStream, CompressionMode.Compress, true))
                {
                    compressionStream.Write(data);
                }

                outputStream.Write(BitConverter.GetBytes(adler32), 0, 4);

                return outputStream.ToArray();
            }
        }

        static byte[] Decompress(byte[] compressedData)
        {
            using (var inputStream = new MemoryStream(compressedData, k_ZlibHeader.Length, compressedData.Length - k_ZlibHeader.Length))
            using (var decompressionStream = new DeflateStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                decompressionStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

        static uint ComputeAdler32(byte[] data)
        {
            const uint MOD_ADLER = 65521;
            uint a = 1, b = 0;

            foreach (byte byteValue in data)
            {
                a = (a + byteValue) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }

            return (b << 16) | a;
        }

        static byte[] HexStringToByteArray(string hex)
        {
            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        static byte[] EncodeECPublicKey(byte[] publicKey)
        {
            var algorithmIdentifier = new AlgorithmIdentifier(
                X9ObjectIdentifiers.IdECPublicKey,
                SecObjectIdentifiers.SecP256r1);

            var sequence = new DerSequence(
                algorithmIdentifier,
                new DerBitString(publicKey));

            return sequence.GetEncoded();
        }
    }
}
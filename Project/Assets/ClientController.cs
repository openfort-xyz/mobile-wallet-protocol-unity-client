using UnityEngine;
using MobileWalletProtocol;

public class ClientController : MonoBehaviour
{
    [SerializeField]
    MWPClientOptions m_Options = new MWPClientOptions()
    {
        Metadata = new AppMetadata()
        {
            Name = "Smart Wallet Expo",
            CustomScheme = "exp://",
            ChainIds = new string[] { "0xaa36a7" }
        },
        Wallet = Wallet.CreateWebWallet(
            name: "Rapid fire wallet",
            scheme: "https://id.sample.openfort.xyz#policy=pol_a909d815-9b6c-40b2-9f6c-e93505281137",
            iconUrl: "https://purple-magnificent-bat-958.mypinata.cloud/ipfs/QmfQrh2BiCzugFauYF9Weu9SFddsVh9qV82uw43cxH8UDV"
        )
    };

    MWPClient m_Client;
    string m_Account;

    void Awake()
    {
        m_Client = new MWPClient(m_Options);
    }

    public async void RequestAccounts()
    {
        var result = await m_Client.EthRequestAccounts();

        if (result.IsSuccess)
        {
            var accounts = result.Value;

            m_Account = accounts[0];

            foreach (var account in accounts)
            {
                Debug.Log("Account: " + account);
            }
        }
        else
        {
            Debug.LogError("Error: " + result.Error);
        }
    }

    public void Disconnect()
    {
        m_Client.Reset();
    }

    public async void PersonalSign()
    {
        var result = await m_Client.PersonalSign(new PersonalSignParams()
        {
            Challenge = "0x48656c6c6f2c20776f726c6421",
            Address = m_Account
        });

        if (result.IsSuccess)
        {
            Debug.Log("Signature: " + result.Value);
        }
        else
        {
            Debug.LogError("Error: " + result.Error);
        }
    }

    public async void SendTransaction()
    {
        var result = await m_Client.EthSendTransaction(new EthSendTransactionParams()
        {
            To = "0xdC2de190a921D846B35EB92d195c9c3D9C08d1C2",
            Data = "0xa0712d680000000000000000000000000000000000000000000000000de0b6b3a7640000"
        });

        if (result.IsSuccess)
        {
            Debug.Log("Transaction Hash: " + result.Value);
        }
        else
        {
            Debug.LogError("Error: " + result.Error);
        }
    }
}

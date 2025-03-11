using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using UnityEngine.Scripting;

namespace MobileWalletProtocol
{
    static class PostRequestToWallet
    {
        [Preserve]
        class EncodedResponseContent
        {
            [Preserve]
            public SerializedEthereumRpcError Failure { get; set; }

            [Preserve]
            public EncryptedContent Encrypted { get; set; }
        }

        [Preserve]
        class EncryptedContent
        {
            [Preserve]
            public string IV { get; set; }

            [Preserve]
            public string CipherText { get; set; }
        }

        public static async Task<RPCResponseMessage> SendRequestAsync(
            RPCRequestMessage request,
            string appCustomScheme,
            Wallet wallet)
        {
            if (wallet.Type == WalletType.Web)
            {
                var uriBuilder = new UriBuilder(wallet.Scheme);

                uriBuilder.Query = EncodeRequestURLParams(request);

                var result = await WebBrowser.Instance.OpenPopupAsync(
                    uriBuilder.ToString(),
                    appCustomScheme);

                if (result.Type == "success")
                {
                    var uri = new Uri(result.Url);
                    var queryParams = HttpUtility.ParseQueryString(uri.Query);

                    return DecodeResponseURLParams(queryParams);
                }
                else if (result.Type == "cancel")
                {
                    throw StandardErrors.Provider.UserRejectedRequest();
                }
                else if (result.Type == "error")
                {
                    throw StandardErrors.Provider.Unauthorized();
                }

                throw new Exception("Unexpected result type");
            }
            else if (wallet.Type == WalletType.Native)
            {
                throw new NotImplementedException("Native wallet not supported yet");
            }

            throw new ArgumentException("Unsupported wallet type");
        }

        static string EncodeRequestURLParams(RPCRequestMessage request)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            AppendParam(query, "id", request.Id);
            AppendParam(query, "sender", request.Sender);
            AppendParam(query, "sdkVersion", request.SdkVersion);
            AppendParam(query, "callbackUrl", request.CallbackUrl);
            AppendParam(query, "customScheme", request.CustomScheme);
            AppendParam(query, "timestamp", request.Timestamp);

            if (request.Content is RequestAccounts handshake)
            {
                query.Add("content", @$"{{""handshake"":{Utils.Serialize(handshake)}}}");
            }

            if (request.Content is EncryptedData encryptedData)
            {
                var encryptedDataRequest = new EncryptedContent()
                {
                    IV = Convert.ToBase64String(encryptedData.IV),
                    CipherText = Convert.ToBase64String(encryptedData.CipherText)
                };

                query.Add("content", @$"{{""encrypted"":{Utils.Serialize(encryptedDataRequest)}}}");
            }

            return query.ToString();
        }

        static void AppendParam<T>(NameValueCollection query, string key, T value)
        {
            if (value != null)
            {
                query.Add(key, Utils.Serialize(value));
            }
        }

        public static RPCResponseMessage DecodeResponseURLParams(NameValueCollection queryParams)
        {
            var contentParam = ParseParam<EncodedResponseContent>(queryParams, "content");

            return new RPCResponseMessage
            {
                Id = ParseParam<Guid>(queryParams, "id"),
                Sender = ParseParam<string>(queryParams, "sender"),
                RequestId = ParseParam<Guid>(queryParams, "requestId"),
                Timestamp = DateTime.Parse(ParseParam<string>(queryParams, "timestamp")),
                Content = contentParam.Encrypted != null
                    ? new EncryptedData
                    {
                        IV = Convert.FromBase64String(contentParam.Encrypted.IV),
                        CipherText = Convert.FromBase64String(contentParam.Encrypted.CipherText)
                    }
                    : contentParam.Failure
            };
        }

        static T ParseParam<T>(NameValueCollection queryParams, string paramName)
        {
            var encodedValue = queryParams[paramName];

            if (string.IsNullOrEmpty(encodedValue))
            {
                throw new Exception($"Missing parameter: {paramName}");
            }

            return Utils.Deserialize<T>(encodedValue);
        }
    }
}
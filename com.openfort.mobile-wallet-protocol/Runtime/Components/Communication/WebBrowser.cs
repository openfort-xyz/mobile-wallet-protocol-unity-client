using System;
using System.Threading;
using System.Threading.Tasks;
using UnityWebBrowser;

namespace MobileWalletProtocol
{
    public class WebBrowserResult
    {
        public string Type { get; set; }
        public string Url { get; set; }
    }

    static class WebBrowser
    {
        public static async Task<WebBrowserResult> OpenAuthSessionAsync(string url, string returnUrlScheme)
        {
            var result = new WebBrowserResult();
            var isDone = false;

            var onError = new Action<string, string>((key, message) =>
            {
                result.Type = string.IsNullOrEmpty(message) ? "cancel" : "error";
                result.Url = message;
                isDone = true;
            });

            var onSuccess = new Action<string>((message) =>
            {
                result.Type = "success";
                result.Url = message;
                isDone = true;
            });

            using (var webView = new WebViewObject(null, onError, null, onSuccess))
            {
                webView.LaunchAuthURL(url, returnUrlScheme);

                await Task.Run(() =>
                {
                    while (!isDone)
                    {
                        Thread.Sleep(100);
                    }
                });

                return result;
            }
        }
    }
}
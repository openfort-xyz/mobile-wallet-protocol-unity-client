using System;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.IO;
using UnityEngine;
using Hazelnut.EdgeWebView;
#else
using UnityWebBrowser;
#endif

namespace MobileWalletProtocol
{
    public class WebBrowserResult
    {
        public string Type { get; set; }
        public string Url { get; set; }
    }

    static class WebBrowser
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        public static async Task<WebBrowserResult> OpenAuthSessionAsync(string url, string returnUrlScheme)
        {
            var result = new WebBrowserResult();
            var isDone = false;

            using (var webView = new WebView(new WebViewOptions()
            {
                ParentWindowHandle = UnityUtil.WindowHandle,
                UseCustomTitle = true,
                CustomTitle = "Popup",
                IsSizable = true,
                UserDataPath = Path.Combine(Application.persistentDataPath, "WebView"),
                Width = 400,
                Height = 600
            }))
            {
                webView.WindowClosing += (sender, e) =>
                {
                    isDone = true;
                    result.Type = "cancel";
                };
                webView.Navigating += (sender, e) =>
                {
                    if (e.Uri.StartsWith(returnUrlScheme))
                    {
                        isDone = true;
                        result.Url = e.Uri;
                        result.Type = "success";
                    }
                };

                webView.Show(url);

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
#else
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
#endif
    }
}
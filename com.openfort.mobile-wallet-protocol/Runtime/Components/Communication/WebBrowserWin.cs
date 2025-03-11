#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using EdgeWebView;

namespace MobileWalletProtocol
{
    class WebBrowserWin : IWebBrowser
    {
        public async Task<WebBrowserResult> OpenPopupAsync(string url, string returnUrlScheme)
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

                while (!isDone)
                {
                    await Task.Yield();
                }

                return result;
            }
        }
    }
}
#endif
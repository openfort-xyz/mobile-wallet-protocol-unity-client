using System.Threading.Tasks;

namespace MobileWalletProtocol
{
    public class WebBrowserResult
    {
        public string Type { get; set; }
        public string Url { get; set; }
    }

    interface IWebBrowser
    {
        Task<WebBrowserResult> OpenPopupAsync(string url, string returnUrlScheme);
    }

    static class WebBrowser
    {
        public static IWebBrowser Instance { get; } =
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            new WebBrowserWin();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            new WebBrowserOSX();
#elif UNITY_IPHONE && !UNITY_EDITOR_OSX
            new WebBrowserIOS();
#elif UNITY_WEBGL && !UNITY_EDITOR
            new WebBrowserWebGL();
#else
            null;
#endif
    }
}
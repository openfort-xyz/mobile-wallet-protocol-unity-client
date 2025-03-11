#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;

namespace EdgeWebView
{
    public partial class WebView
    {
        [DllImport("EdgeWebViewLib")]
        private extern static IntPtr CreateWebView2Window(in WebView2Options options);

        [DllImport("EdgeWebViewLib")]
        private extern static WebView2ShowError ShowWebView2Window(IntPtr webView, [MarshalAs(UnmanagedType.LPWStr)] string uri);

        [DllImport("EdgeWebViewLib")]
        private extern static void CloseWebView2Window(IntPtr webView);

        [DllImport("EdgeWebViewLib")]
        private extern static void DestroyWebView2Window(IntPtr webView);

        [DllImport("EdgeWebViewLib")]
        private extern static void NavigateWebView2Window(IntPtr webView, [MarshalAs(UnmanagedType.LPWStr)] string uri);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct WebView2Options
        {
            public IntPtr ParentWindowHandle;

            public uint Width;
            public uint Height;
            public int Sizable;

            public int CustomTitle;
            public fixed char Title[128];

            public fixed char UserAgent[256];
            public fixed char UserDataDirectoryPath[1024];

            public WindowCreatedCallbackDelegate WindowCreatedCallback;
            public WindowClosingCallbackDelegate WindowClosingCallback;
            public WindowClosedCallbackDelegate WindowClosedCallback;
            public NavigatingCallbackDelegate NavigatingCallback;
            public NavigatedCallbackDelegate NavigatedCallback;
        }

        private enum WebView2ShowError
        {
            NoError,

            InvalidArguments,

            NoRegisteredWindowClass,
            NoCoInitializeCalled,
            WindowSizeAdjustFailed,
            NoWindowCreated,
            NoWebView2ObjectCreated,
            SoManyWebView2RuntimeDetected,
            WebView2RuntimeVersionMismatch,
            WebView2RuntimeNoInstalled,
            UserDataFolderAccessDenied,
            UnableToStartEdgeRuntime,
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void WindowCreatedCallbackDelegate(IntPtr sender);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void WindowClosingCallbackDelegate(IntPtr sender, out int cancel);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void WindowClosedCallbackDelegate(IntPtr sender);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void NavigatingCallbackDelegate(IntPtr sender, IntPtr uri, out int cancel);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void NavigatedCallbackDelegate(IntPtr sender, bool succeeded, IntPtr uri, ushort statusCode);
    }
}
#endif
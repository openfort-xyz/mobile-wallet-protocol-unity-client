#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;

namespace EdgeWebView
{
    [Serializable]
    public struct WebViewOptions
    {
        public IntPtr ParentWindowHandle;
        
        public bool UseCustomTitle;
        public string CustomTitle;

        public int Width;
        public int Height;
        public bool IsSizable;

        public string UserAgent;
        public string UserDataPath;
    }
}
#endif
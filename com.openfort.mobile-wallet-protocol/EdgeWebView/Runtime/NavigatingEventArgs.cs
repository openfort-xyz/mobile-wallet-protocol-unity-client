#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;

namespace EdgeWebView
{
    public class NavigatingEventArgs : EventArgs
    {
        public string Uri { get; }
        public bool Cancel { get; set; }

        public NavigatingEventArgs(string uri)
        {
            Uri = uri;
            Cancel = false;
        }
    }
}
#endif
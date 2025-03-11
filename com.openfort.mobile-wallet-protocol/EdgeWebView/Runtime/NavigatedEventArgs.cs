#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Net;

namespace EdgeWebView
{
    public class NavigatedEventArgs : EventArgs
    {
        public bool IsSucceeded { get; }
        public string Uri { get; }
        public HttpStatusCode StatusCode { get; }

        public NavigatedEventArgs(bool isSucceeded, string uri, HttpStatusCode statusCode)
        {
            IsSucceeded = isSucceeded;
            Uri = uri;
            StatusCode = statusCode;
        }
    }
}
#endif
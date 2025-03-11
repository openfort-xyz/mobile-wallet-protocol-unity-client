#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace MobileWalletProtocol
{
    public class WebBrowserWebGLCallbackReceiver : MonoBehaviour
    {
        public event Action<string> onMessage;

        [Preserve]
        public void OnMessage(string message)
        {
            onMessage?.Invoke(message);
        }
    }
}
#endif
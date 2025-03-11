using System;
using UnityEngine;

namespace MobileWalletProtocol
{
    /// <summary>
    /// Represents the metadata of the application.
    /// </summary>
    [Serializable]
    public class AppMetadata
    {
        [SerializeField]
        string m_Name;

        [SerializeField]
        string m_LogoUrl;

        [SerializeField]
        string m_CustomScheme;

        [SerializeField]
        string[] m_ChainIds;

        /// <summary>
        /// Application name
        /// </summary>
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        /// <summary>
        /// Application logo image URL
        /// </summary>
        public string LogoUrl
        {
            get => m_LogoUrl;
            set => m_LogoUrl = value;
        }

        /// <summary>
        /// Array of chainIds in number your dapp supports
        /// </summary>
        public string[] ChainIds
        {
            get => m_ChainIds;
            set => m_ChainIds = value;
        }

        /// <summary>
        /// Custom URL scheme for returning to this app after wallet interaction
        /// Example: 'myapp://'
        /// </summary>
        public string CustomScheme{
            get => m_CustomScheme;
            set => m_CustomScheme = value;
        }
    }
}
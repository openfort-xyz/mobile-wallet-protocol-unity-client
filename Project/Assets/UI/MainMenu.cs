using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    UIDocument m_Document;
    Button m_ConnectButton;
    Button m_DisconnectButton;
    Button m_PersonalSignButton;
    Button m_EthSendTransactionButton;

    [SerializeField]
    UnityEvent m_OnConnect = new UnityEvent();

    [SerializeField]
    UnityEvent m_OnDisconnect = new UnityEvent();

    [SerializeField]
    UnityEvent m_OnPersonalSign = new UnityEvent();

    [SerializeField]
    UnityEvent m_OnEthSendTransaction = new UnityEvent();

    void Awake()
    {
        m_Document = GetComponent<UIDocument>();

        m_ConnectButton = m_Document.rootVisualElement.Q<Button>("button-connect");
        m_DisconnectButton = m_Document.rootVisualElement.Q<Button>("button-disconnect");
        m_PersonalSignButton = m_Document.rootVisualElement.Q<Button>("button-personal-sign");
        m_EthSendTransactionButton = m_Document.rootVisualElement.Q<Button>("button-eth-send-transaction");

        m_ConnectButton.clickable.clicked += () => m_OnConnect.Invoke();
        m_DisconnectButton.clickable.clicked += () => m_OnDisconnect.Invoke();
        m_PersonalSignButton.clickable.clicked += () => m_OnPersonalSign.Invoke();
        m_EthSendTransactionButton.clickable.clicked += () => m_OnEthSendTransaction.Invoke();
    }


}

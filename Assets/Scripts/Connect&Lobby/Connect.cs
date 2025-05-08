using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Net.Http.Headers;

public class Connect : MonoBehaviourPunCallbacks
{
    public GameObject disconnectUI;
    public Button reconnectButton;
    public GameObject CCULimitUI;
    public TMP_Text Message;

    private bool isConnecting = false;

    private void Start()
    {
        disconnectUI.SetActive(false);
        reconnectButton.onClick.AddListener(Reconnect);
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        SceneManager.LoadScene("Lobby");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isConnecting = false;

        if (cause == DisconnectCause.MaxCcuReached)
        {
            CCULimitUI.SetActive(true);
            Message.text = "The server is full. Please try again later. 20 players are currently in the game.";
        }else
        {
            disconnectUI.SetActive(true);
        }
    }


    public void Reconnect()
    {
        if (!isConnecting) // Prevent multiple clicks while connecting
        {
            isConnecting = true;
            reconnectButton.interactable = false;  // Disable the button while connecting
            PhotonNetwork.Reconnect();
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnected()
    {
        disconnectUI.SetActive(false);
        CCULimitUI.SetActive(false);
    }
}

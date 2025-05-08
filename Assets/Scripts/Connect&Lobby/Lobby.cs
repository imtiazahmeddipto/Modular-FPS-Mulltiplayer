//Kinda advanced Photon Lobby System

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;

public class Lobby : MonoBehaviourPunCallbacks, IConnectionCallbacks
{
    [Header("Main Panels")]
    [SerializeField] private GameObject usernamePanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject waitingPanel;

    [Header("Username Panel Stuff")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_Text usernameDisplay;

    [Header("Lobby Panel Stuff")]
    [SerializeField] private CreateRoom creatingRooms;
    [SerializeField] private TMP_InputField customRoomInput;
    [SerializeField] private JoinRoom joiningRooms;
    [SerializeField] private WaitingInRooms waitingInRooms;
    private List<RoomInfo> availableRooms = new List<RoomInfo>();
    [Header("Error Messages")]
    [SerializeField] private TMP_Text ErrorText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button RandRoomorCreateButton;
    [SerializeField] private Button PrivateRoomCreateButton;
    [SerializeField] private GameObject DesconnectUI;
    [SerializeField] private TMP_Text roomCountText;
    [SerializeField] private TMP_Text playerCountText;

    #region Unity Methods

    private void Start()
    {
        //Check if has the previous saved data of the Username and if so, then load it.
        if (PlayerPrefs.HasKey("usernameKey"))
        {
            usernameInput.text = PlayerPrefs.GetString("usernameKey");
            usernameDisplay.text = PlayerPrefs.GetString("usernameKey");
            usernamePanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }
        StartCoroutine(UpdateCCUPeriodically());
        RefreshRoomList();
    }
    private IEnumerator UpdateCCUPeriodically()
    {
        while (true)
        {
            if (PhotonNetwork.IsConnected)
            {
                // Get total players in ALL rooms (including private ones)
                int totalPlayers = PhotonNetwork.CountOfPlayers;
                playerCountText.text = $"Online Players: {totalPlayers}";

                int roomCount = availableRooms.Count(room => !room.RemovedFromList && room.PlayerCount > 0);
                roomCountText.text = $"Rooms Available: {roomCount}";
            }
            else
            {
                playerCountText.text = "Connecting...";
                roomCountText.text = "Connecting...";
            }
            yield return new WaitForSeconds(1f);
        }
    }

    void IConnectionCallbacks.OnDisconnected(DisconnectCause cause)
    {
        DesconnectUI.SetActive(true);
    }

    public void Reconnect()
    {
        // Check if already connected
        if (PhotonNetwork.IsConnected)
        {
            RefreshRoomList();
            PhotonNetwork.JoinLobby();
            return;
        }

        // If not connected, try to connect again
        PhotonNetwork.ConnectUsingSettings();

        // Optionally, show some loading UI or feedback for the user
        DesconnectUI.SetActive(false); // Hide the disconnect UI
        waitingPanel.SetActive(false); // Hide the waiting panel if it's visible

        // Optionally, you can show some reconnecting message or UI
        Debug.Log("Attempting to reconnect to Photon...");
    }

    void Update()
    {
        if (PhotonNetwork.IsConnected)
        {
            DesconnectUI.SetActive(false);
        }
    }

    public void JoinRoomByID()
    {
        string roomName = customRoomInput.text;

        if (string.IsNullOrEmpty(roomName))
        {
            ErrorText.text = "Please enter a Room ID.";
            return;
        }

        // Allow joining both public (e.g., "1234") and private (e.g., "1234{P}") rooms
        PhotonNetwork.JoinRoom(roomName);
        ErrorText.text = "";
    }

    public void JoinRandomRoom()
    {
        ErrorText.text = "";
        PhotonNetwork.JoinRandomRoom();
    }
    private IEnumerator EnableButtonsAfterDelay(params Button[] buttons)
    {
        // Disable all specified buttons
        foreach (Button button in buttons)
        {
            button.interactable = false;
        }

        yield return new WaitForSeconds(3f); // 3-second cooldown

        // Re-enable all buttons
        foreach (Button button in buttons)
        {
            button.interactable = true;
        }
    }

    public void PreventSpamClick()
    {
        StartCoroutine(EnableButtonsAfterDelay(RandRoomorCreateButton, PrivateRoomCreateButton));
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Random Join Failed. Creating a new room.");
        CreateRoomOnRandomJoinFail();
    }

    public void CreateRoomOnRandomJoinFail()
    {
        string roomName = "" + Random.Range(1000, 10000); // e.g., "4827"
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = (byte)creatingRooms.maxPlayers,
            IsVisible = true, // Public rooms are visible
            IsOpen = true
        };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void CreatePrivateRoom()
    {
        string roomName = "" + Random.Range(1000, 10000) + "P";
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = (byte)creatingRooms.maxPlayers,
            IsVisible = false, // Private rooms are hidden
            IsOpen = true
        };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log("Created PRIVATE room: " + roomName);
    }


    #endregion

    #region Public Methods

    //Executed on Username Input Field Changes its text value in the Username panel.
    public void UpdateUsername()
    {
        string username = usernameInput.text;

        // Validate username length
        if (username.Length < 1 || username.Length > 8)
        {
            ErrorText.text = "Username must be between 1 and 8 characters.";
            playButton.interactable = false; // Disable play button if invalid
            return;
        }

        PhotonNetwork.NickName = username;
        PlayerPrefs.SetString("usernameKey", username);
        usernameDisplay.text = PlayerPrefs.GetString("usernameKey");
        ErrorText.text = ""; // Clear error text if valid
        playButton.interactable = true; // Enable play button if valid
    }

    //Executed on clicking the "Play!" button in the Username panel!
    public void Play()
    {
        //Load the Lobby Panel since we got the Username input.
        usernamePanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    //Executed on clicking the "Create Room!" button in the Lobby panel!
    public void CreateRoom()
    {
        /*if (string.IsNullOrWhiteSpace(creatingRooms.createInput.text) || creatingRooms.createInput.text.Length > 8)
        {
            lobbyErrorText.text = "Room name must be 1 to 8 characters.";
            StartCoroutine(ClearErrorAfterDelay(lobbyErrorText, 3f));
            return; // Ensure input is valid
        }

        string roomName = creatingRooms.createInput.text; // Get the desired room name

        // Check if the room already exists
        foreach (RoomInfo room in availableRooms)
        {
            if (room.Name == roomName)
            {
                JoinRoom(roomName); // Join the existing room instead
                Debug.Log("Joining existing room: " + roomName);
                return;
            }
        }

        // If no such room exists, create a new one
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)creatingRooms.maxPlayers;

        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
        Debug.Log("Creating new room: " + roomName);*/
    }


    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        string errorMessage = "Failed to join room: " + message;
        switch (returnCode)
        {
            case 32765: // ErrorCode.GameClosed (Photon's error code for closed rooms)
                errorMessage = "Game has already started.";
                break;
            case 32758: // ErrorCode.GameFull
                errorMessage = "Room does not exist.";
                break;
            case 32757: // ErrorCode.GameDoesNotExist
                errorMessage = "Room does not exist.";
                break;
        }
        ErrorText.text = errorMessage;
        StartCoroutine(ClearErrorAfterDelay(ErrorText, 3f));
    }

    private IEnumerator ClearErrorAfterDelay(TMP_Text errorText, float delay)
    {
        yield return new WaitForSeconds(delay);
        errorText.text = "";
    }

    //Executed in the "JoinRoom" method in the RoomItem script
    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName); //Join the Room
    }


    //Executed when "Start Game!" Button is Clicked in the Player waiting List!
    public void StartGame()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            waitingInRooms.startGameButton.SetActive(false);
            PhotonNetwork.LoadLevel("Game");
        }
        else
        {
            ErrorText.text = "Not enough players to start the game.";
            StartCoroutine(ClearErrorAfterDelay(ErrorText, 3f));
        }
    }

    //Executed when "Leave Room" Button is Clicked in the Player waiting List!
    public void LeaveRoom()
    {
        //Leave the Room!
        PhotonNetwork.LeaveRoom();
        
        //Disable all the Panels except the Username Panel
        waitingPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        usernamePanel.SetActive(true);


    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    #endregion

    #region Private Methods

    private void UpdateRoomList(List<RoomInfo> roomList)
    {
        availableRooms = roomList;
        //Delete all the Rooms Displayed so we can load them back!
        foreach (RoomItem item in joiningRooms.roomItemsList)
        {
            Destroy(item.gameObject); //Destroy
        }
        joiningRooms.roomItemsList.Clear(); //Clear the Room List

        //Add all the Rooms Deleted with the newly available Rooms!
        foreach (RoomInfo room in roomList)
        {
            //Move on if the Room was Removed from the List
            if (room.RemovedFromList)
                continue;

            RoomItem newRoom = Instantiate(joiningRooms.roomItemPrefab, joiningRooms.roomContentObject); //Spawn the New Room
            newRoom.Setup(room); //Set the Room Name of the Room!
            joiningRooms.roomItemsList.Add(newRoom); //Add the New Room into the Room List!

            if (room.PlayerCount == 0) //If Room count is 0
            {
                PhotonNetwork.Destroy(newRoom.gameObject); //Destroy the Room
                joiningRooms.roomItemsList.Remove(newRoom); //Remove the Room from the Room List
            }
        }

        Debug.Log("Updated Room List!"); //Debugging
    }

    #endregion

    #region Photon Methods

    //Executed on Successfully Joined Room
    public override void OnJoinedRoom()
    {
        //Debug.Log("Successfully Created/Joined the Room " + creatingRooms.createInput.text); //Debugging

        //Load the Waiting Panel and Disable the Lobby Panel
        lobbyPanel.SetActive(false);
        waitingPanel.SetActive(true);

        waitingInRooms.playerRoomNameText.text = PhotonNetwork.CurrentRoom.Name; //Set the Room Name

        Player[] players = PhotonNetwork.PlayerList; //Get the Player List using an Array of Players
        
        //Delete all the Player Objects Displayed so we can load them back!
        foreach (Transform item in waitingInRooms.playerContentObject)
        {
            Destroy(item.gameObject); //Destroy
        }

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(waitingInRooms.playerItemPrefab, waitingInRooms.playerContentObject).GetComponent<PlayerListItem>().Setup(players[i]); //Spawn the player in the Player List!
        }
        
        waitingInRooms.startGameButton.SetActive(PhotonNetwork.IsMasterClient); //Make sure only the Host can start the Game xD
    }

    //Executed when the Host switches
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        waitingInRooms.startGameButton.SetActive(PhotonNetwork.IsMasterClient); //Make sure only the Host can start the Game xD
    }

    //Executed on Creating Room Failed
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        string errorMessage = "Failed to create room: " + message;
        ErrorText.text = errorMessage;
        StartCoroutine(ClearErrorAfterDelay(ErrorText, 3f));
    }

    //Executed on the Room List Update
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {

        if (Time.time >= joiningRooms.nextUpdateTime)
        {
            UpdateRoomList(roomList); //Update the Room List in a separate method!
            joiningRooms.nextUpdateTime = Time.time + joiningRooms.timeBetweenUpdates;
        }
    }
    public void RefreshRoomList()
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.JoinLobby();
        }
    }

    //Executed on Player Enters the Room
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(waitingInRooms.playerItemPrefab, waitingInRooms.playerContentObject).GetComponent<PlayerListItem>().Setup(newPlayer); //Spawn the player in the Player List!
    }

    #endregion
}

#region Serialized Classes

[System.Serializable]
public class CreateRoom
{
    [Header("Room Settings")]
    public int maxPlayers = 5;
    
    //[Header("References")]
    //public TMP_InputField createInput;
    //public TMP_Text sliderValueText;
    //public Slider valueSlider;
}

[System.Serializable]
public class JoinRoom
{
    [Header("Script References")]
    public RoomItem roomItemPrefab;
    
    [Header("References")]
    public List<RoomItem> roomItemsList;
    public Transform roomContentObject;

    [Header("Settings")]
    public float timeBetweenUpdates = 1.5f;
    [HideInInspector] public float nextUpdateTime;
}

[System.Serializable]
public class WaitingInRooms
{
    [Header("References")]
    public GameObject playerItemPrefab;
    public Transform playerContentObject;
    public TMP_Text playerRoomNameText;
    [Space]
    public GameObject startGameButton;
}

#endregion
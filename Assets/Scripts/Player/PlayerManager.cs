using UnityEngine;
using Photon.Pun;
using System.IO;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerManager : MonoBehaviour
{
    private PhotonView pv;
    public GameObject controller;
    [SerializeField] private ParticleSystem killIndicator;

    // Character selection data
    [System.Serializable]
    public class CharacterData
    {
        public string characterName;
        public string prefabPath; //filename
    }

    [SerializeField] private CharacterData[] characters; // Define your characters in the Inspector

    private void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (pv.IsMine)
        {
            CreateController();
            if (pv.IsMine)
            {
                // Create a new Hashtable to reset the player's custom properties
                Hashtable resetProps = new Hashtable
                {
                { "kills", 0 },
                { "deaths", 0 }
                };

                // Set the custom properties for the local player
                PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
                Debug.Log("Player data cleared on join.");
            }
        }
    }

    private void CreateController()
    {
        if (pv.IsMine)
        {
            PlayerMovement player = GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.isDead = false;
            }
        }
        // Get the selected character index from PlayerPrefs
        int selectedCharIndex = PlayerPrefs.GetInt("SelectedCharacter", 0); // Default to 0 if not set

        // Validate the index
        if (selectedCharIndex < 0 || selectedCharIndex >= characters.Length)
        {
            Debug.LogError("Invalid character index! Using default character.");
            selectedCharIndex = 0;
        }

        // Get the prefab path for the selected character
        string prefabPath = characters[selectedCharIndex].prefabPath;

        // Get the spawn point
        Transform spawnPoint = SpawnManager.Instance.GetSpawnPoint();

        // Instantiate the player prefab
        controller = PhotonNetwork.Instantiate(
            Path.Combine("PhotonPrefabs", prefabPath), // Ensure the path is correct
            spawnPoint.position,
            spawnPoint.rotation,
            0,
            new object[] { pv.ViewID }
        );


        Debug.Log($"Instantiated Player: {characters[selectedCharIndex].characterName}");

        // Additional setup for the player controller
        if (controller.TryGetComponent(out PlayerMovement playerMovement))
        {
            killIndicator = playerMovement.KillIndicator;
            playerMovement.transform.rotation = Quaternion.identity;
            playerMovement.orientation.rotation = spawnPoint.rotation;
        }

        // Set player names (if needed)
        foreach (PlayerMovement player in FindObjectsOfType<PlayerMovement>())
        {
            player.gameObject.name = player.playerName;
        }
    }

    public void Die()
    {
        Debug.Log("You Died!");
        Invoke("DestroyPlayerAndRespawn", 3.99f);
    }

    private void DestroyPlayerAndRespawn()
    {
        PhotonNetwork.Destroy(controller);
        killIndicator.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        CreateController();
    }
}
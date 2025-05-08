using System.IO;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    [HideInInspector] public Timer timer;
    [HideInInspector] public Scoreboard scoreboard;
    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Instance = this;
        if (GetComponent<ConnectionHandler>() == null)
        {
            var handler = gameObject.AddComponent<ConnectionHandler>();
            handler.KeepAliveInBackground = 60000; // 60 seconds
            handler.DisconnectAfterKeepAlive = false;
            handler.ApplyDontDestroyOnLoad = true;
            handler.StartFallbackSendAckThread();
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.buildIndex == 2)
        {
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerManager"), Vector3.zero, Quaternion.identity);
            timer = FindObjectOfType<Timer>();
            scoreboard = FindObjectOfType<Scoreboard>();
        }
    }
}

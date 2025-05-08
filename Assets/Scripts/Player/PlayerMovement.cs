//Script which handles the Player and its Movement

using System;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviourPunCallbacks, IDamageable, IConnectionCallbacks
{

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject diedPanel;
    public GameObject gameOverPanel;
    public GameObject pauseMenuPanel;

    [Header("References")]
    public Rigidbody rb;
    public Transform orientation;
    [Space]
    [SerializeField] private Camera _camera;
    [SerializeField] private WeaponSway sway;
    [SerializeField] private GameObject canvasObject;
    [Space]
    public GameObject capsuleObject;
    [Space]
    [SerializeField] private UsernameDisplay usernameDisplay;
    [SerializeField] public Look playerLook;
    [Space]
    [SerializeField] private TMP_Text killedByText;

    [Header("Movement")]
    [SerializeField] private float speed = 40f;
    [SerializeField] private float airSpeed = 20f;
    [Space]
    [SerializeField] private float jumpForce = 12.5f;
    [SerializeField] private float jumpRate = 15f;
    private float nextTimeToJump;
    [SerializeField] private PhysicMaterial playerMat;

    [Header("Ground Detection")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;

    [Header("Drag")]
    [SerializeField] private float drag = 6f;
    [SerializeField] private float airDrag = 2f;

    [Header("Weapon Equipment")]
    [SerializeField] public Item[] items;
    [Space]
    private int itemIndex;
    private int previousIndex = -1;
    [Space]
    public bool canSwitchGuns;

    [Header("Health Management")]
    [SerializeField] private Image healthbarImage;
    private const float maxHealth = 100f;
    private float currentHealth = maxHealth;

    [Header("Game Over Countdown")]
    public TextMeshProUGUI countdownText;

    [HideInInspector] public PlayerManager playerManager;

    [Space(height: 20)]
    public GameObject weaponHolder;

    private Vector3 moveDirection;
    public PhotonView pv;
    [HideInInspector] public string playerName;

    private float h;
    private float v;

    private float sendGameOverOnce;
    [SerializeField] private Animator playerAnim;
    public GameObject Body;
    [SerializeField] public bool isDead;
    public TextMeshProUGUI pingText;
    public Vector2 runAxis;
    public bool JuppAxis;
    public bool FireButton;
    public bool ScopeButton;
    public bool ReloadButton;
    public bool NextWeaponButton;
    public bool PrevWeaponButton;
    private bool prevNextWeaponButton;
    private bool prevPrevWeaponButton;
    [Header("Weapon UI")]
    [SerializeField] private Image currentWeaponIcon; // Reference to UI Image showing current weapon
    [SerializeField] private Sprite[] weaponIcons;
    public ParticleSystem KillIndicator;
    private int previousKills = 0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathSound;

    [Header("Control Scheme")]
    public bool isInComputer = false;
    public GameObject DesconnectUI;



    private void Awake()
    {
        //rb.freezeRotation = true;
        playerManager = PhotonView.Find((int)pv.InstantiationData[0]).GetComponent<PlayerManager>();
    }
    void IConnectionCallbacks.OnDisconnected(DisconnectCause cause)
    {
        DesconnectUI.SetActive(true);
        playerLook.cursorLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        if (isInComputer)
        {
            Resume();
        }
        
        InvokeRepeating(nameof(UpdatePing), 1f, 1f);

        if (pv.IsMine)
        {
            EquipItem(0);
            Body.SetActive(false);

            // Initialize cursor state based on control scheme
            Cursor.lockState = isInComputer ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isInComputer;

            // Initialize mobile UI visibility
            mainPanel.SetActive(!isInComputer);
        }
        else
        {
            _camera.gameObject.GetComponent<AudioListener>().enabled = false;
            Destroy(_camera.gameObject);
        }

        if (pv.IsMine)
        {
            // Initialize previousKills from player properties
            previousKills = (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("kills", out object killsObj)) ? (int)killsObj : 0;
        }
        UpdateWeaponIcon();
    }

    private void UpdatePing()
    {
        if (PhotonNetwork.IsConnected)
        {
            pingText.text = "Ping: " + PhotonNetwork.GetPing();
        }
        else
        {
            pingText.text = "Not Connected";
        }
    }

    private void Update()
    {
        if (pv == null) return;

        if (!pv.IsMine)
        {
            sway.enabled = false;
            canvasObject.SetActive(false);

            gameObject.tag = "OtherPlayer";
            return;
        }

        if (isInComputer)
        {
            HandlePCInput();
        }

        if (!RoomManager.Instance.timer.timerOn && RoomManager.Instance.timer.startedGame)
        {
            SetGameOver();
        }

        playerName = usernameDisplay.usernameText.text;

        gameObject.tag = "Player";

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (gameOverPanel.activeInHierarchy)
        {
            
        }

        if (!pauseMenuPanel.activeInHierarchy)
        {
            Movement();
            HandleDrag();
            if (!isInComputer) CheckWeaponSwitch();
            UseGun();

        }
        else
        {
        }

        if (isInComputer && Input.GetKeyDown(KeyCode.Escape) && !gameOverPanel.activeInHierarchy)
        {
            Pause();
        }


        if (photonView.IsMine)
        {
            GetComponent<MobileControll>().enabled = true;
        }
    }

    private void HandlePCInput()
    {
        // Movement input
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        // Button inputs
        FireButton = Input.GetMouseButton(0);
        ScopeButton = Input.GetMouseButton(1);
        ReloadButton = Input.GetKeyDown(KeyCode.R);
        JuppAxis = Input.GetKeyDown(KeyCode.Space);

        // Weapon switching
        HandleMouseWheelSwitching();
        HandleNumberKeySwitching();

        // Disable mobile controls component
        if (TryGetComponent<MobileControll>(out var mobileControll))
        {
            mobileControll.enabled = false;
        }
    }

    private void HandleMouseWheelSwitching()
    {
        if (canSwitchGuns)
        {
         float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) NextWeapon();
        else if (scroll < 0) PreviousWeapon();
        }
        
    }

    private void HandleNumberKeySwitching()
    {
        if (canSwitchGuns)
        {
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipItem(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipItem(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipItem(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipItem(3);
        }
        
    }

    private void CheckWeaponSwitch()
    {
        // Check for Next Weapon button press (rising edge)
        if (NextWeaponButton && !prevNextWeaponButton)
        {
            NextWeapon();
        }
        prevNextWeaponButton = NextWeaponButton;

        // Check for Previous Weapon button press (rising edge)
        if (PrevWeaponButton && !prevPrevWeaponButton)
        {
            PreviousWeapon();
        }
        prevPrevWeaponButton = PrevWeaponButton;
    }

    public void LeaveRoom()
    {

        PhotonNetwork.LeaveRoom();
        //PhotonNetwork.Disconnect();
        PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);
        Destroy(RoomManager.Instance.gameObject);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Connect");
    }

    public void DesconnectRoom()
    {

        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect();
        PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);
        Destroy(RoomManager.Instance.gameObject);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Connect");
    }
    public void Pause()
    {
        if (!pauseMenuPanel.activeInHierarchy && !gameOverPanel.activeInHierarchy)
        {
            pauseMenuPanel.SetActive(true);
            if (isInComputer)
            {
                playerLook.cursorLocked = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        if (isInComputer)
        {
            playerLook.cursorLocked = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetGameOver()
    {
        pv.RPC("GameOverRPC", RpcTarget.All);
        playerLook.cursorLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Movement()
    {
        Vector2 inputAxis = isInComputer ?
            new Vector2(h, v) :
            runAxis;

        float minSpeedFactor = 0.5f; // 50% minimum speed even with small drag
        float finalSpeedFactor = Mathf.Lerp(minSpeedFactor, 1f, inputAxis.magnitude);
        moveDirection = (orientation.forward * inputAxis.y + orientation.right * inputAxis.x).normalized * finalSpeedFactor;
        bool isRunning = moveDirection.magnitude > 0;

        // Handle jump input
        bool jumpInput = isInComputer ?
            Input.GetKeyDown(KeyCode.Space) :
            JuppAxis;

        if (isGrounded && jumpInput && Time.time >= nextTimeToJump)
        {
            nextTimeToJump = Time.time + 1f / jumpRate;
            Jump();
        }

        if (pv.IsMine)
        {
            playerAnim.SetBool("Running", isRunning);
        }
    }

    private void HandleDrag()
    {
        if (isGrounded)
        {
            rb.drag = drag;
            playerAnim.SetBool("Jump", false);
        }
        else
        {
            rb.drag = airDrag;
            playerAnim.SetBool("Jump", true);
        }
            
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (!pv.IsMine) return;

        if (isGrounded)
            rb.AddForce(moveDirection * speed, ForceMode.Acceleration);
        else
            rb.AddForce(moveDirection * airSpeed, ForceMode.Acceleration);
    }

    public void NextWeapon()
    {
        if (!canSwitchGuns) return;

        int newIndex = itemIndex + 1;
        if (newIndex >= items.Length)
        {
            newIndex = 0;
        }
        EquipItem(newIndex);
    }

    public void PreviousWeapon()
    {
        if (!canSwitchGuns) return;

        int newIndex = itemIndex - 1;
        if (newIndex < 0)
        {
            newIndex = items.Length - 1;
        }
        EquipItem(newIndex);
    }

    private void UseGun()
    {
        if (isDead) return;

        Gun currentGun = items[itemIndex].gameObject.GetComponent<Gun>();

        if (currentGun.isEquipping) return;

        if (currentGun.automatic)
        {
            if (FireButton && !currentGun.isReloading && Time.time >= currentGun.nextTimeToFire)
            {
                items[itemIndex].Use();
                currentGun.nextTimeToFire = Time.time + 1f / currentGun.fireRate;
            }
            else
            {
                currentGun.StopShootingAnimationAuto();
            }
        }
        else
        {
            if (FireButton)
            {
                items[itemIndex].Use();
            }
            else
            {
                currentGun.StopShootingAnimationNonAuto();
            }
        }
    }
    private void UpdateWeaponIcon()
    {
        if (currentWeaponIcon != null && weaponIcons.Length > itemIndex)
        {
            currentWeaponIcon.sprite = weaponIcons[itemIndex];
        }
    }
    private void EquipItem(int _index)
    {
        if (pv == null) return;

        if (_index == previousIndex) return;

        itemIndex = _index;
        items[itemIndex].itemGameObject.SetActive(true);

        if (previousIndex != -1)
        {
            items[previousIndex].itemGameObject.SetActive(false);
        }

        previousIndex = itemIndex;

        // Update UI for local player
        if (pv.IsMine)
        {
            UpdateWeaponIcon();
            Hashtable hash = new Hashtable();
            hash.Add("itemIndex", itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!pv.IsMine && targetPlayer == pv.Owner)
        {
            EquipItem((int)changedProps["itemIndex"]);
        }

        // Check if the updated player is the local player and if kills were updated
        if (targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey("kills"))
        {
            int newKills = (int)changedProps["kills"];

            // Check if kills increased
            if (newKills > previousKills)
            {
                // Play the kill indicator particle
                if (KillIndicator != null)
                {
                    KillIndicator.Play();
                }
                previousKills = newKills;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        pv.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    private void Die(Player player)
    {
        playerManager.Die();
        pv.RPC("DieRPC", RpcTarget.All, player);
    }

    private void OnCollisionStay(Collision collisionInfo)
    {
        if (pv.IsMine)
        {
            if (collisionInfo.gameObject.CompareTag("Stairs"))
            {
                gameObject.GetComponent<CapsuleCollider>().material = null;
                rb.AddForce(moveDirection * speed * 3f, ForceMode.Acceleration);
            }
            else
            {
                gameObject.GetComponent<CapsuleCollider>().material = playerMat;
            }
        }
    }

    [PunRPC]
    private void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        if (!pv.IsMine)
            return;

        if (isDead)
            return;

        Player sender = info.Sender;

        Debug.Log("Took damage:" + damage);
        currentHealth -= damage;
        healthbarImage.fillAmount = currentHealth / maxHealth;

        if (currentHealth <= 0 && RoomManager.Instance.timer.timerOn)
        {
            Die(sender);
        }
    }

    [PunRPC]
    private void DieRPC(Player sender)
    {
        isDead = true;
        GetComponent<CapsuleCollider>().enabled = false;

        capsuleObject.SetActive(false);
        weaponHolder.SetActive(false);

        mainPanel.SetActive(false);
        diedPanel.SetActive(true);

        usernameDisplay.gameObject.SetActive(false);
        rb.useGravity = false;

        killedByText.text = "KILLED BY: " + sender.NickName;

        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Update kills and deaths
        Player localPlayer = PhotonNetwork.LocalPlayer;

        // Check if this client is the killer and update kills
        if (sender != null && sender.Equals(localPlayer))
        {
            int kills = 0;
            if (localPlayer.CustomProperties.TryGetValue("kills", out object killsObj))
            {
                kills = (int)killsObj;
            }
            kills += 1;
            Hashtable killHash = new Hashtable();
            killHash["kills"] = kills;
            localPlayer.SetCustomProperties(killHash);
        }

        // Check if this client is the victim and update deaths
        if (pv.IsMine)
        {
            int deaths = 0;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("deaths", out object deathsObj))
            {
                deaths = (int)deathsObj;
            }
            deaths++;
            Hashtable deathHash = new Hashtable();
            deathHash["deaths"] = deaths;
            PhotonNetwork.LocalPlayer.SetCustomProperties(deathHash);
        }
    }

    [PunRPC]
    private void GameOverRPC()
    {
        isDead = true;
        GetComponent<CapsuleCollider>().enabled = false;

        capsuleObject.SetActive(false);
        weaponHolder.SetActive(false);

        mainPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        RoomManager.Instance.scoreboard.FinalScore();

        usernameDisplay.gameObject.SetActive(false);

        rb.useGravity = false;

        Debug.Log("Game OVER!");
    }
}
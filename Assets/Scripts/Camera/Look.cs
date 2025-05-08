using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Look : MonoBehaviour
{
    [Header("Control Scheme")]
    public bool isInComputer = false;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform orientation;
    [SerializeField] private PhotonView pv;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider mobileSensitivitySlider; // Add mobile slider reference

    [Header("Look Settings")]
    [SerializeField] private float sensX = 10f;
    [SerializeField] private float sensY = 10f;
    [SerializeField] private float mobileSensMultiplier = 0.1f; // Mobile-specific sensitivity

    [Space]
    public bool cursorLocked = true;

    private float y;
    private float x;
    public Vector2 lookAxis;

    // PlayerPrefs keys
    private const string PC_SENS_KEY = "PCMouseSensitivity";
    private const string MOBILE_SENS_KEY = "MobileTouchSensitivity";

    private void Start()
    {
        // Load saved sensitivities
        sensX = PlayerPrefs.GetFloat(PC_SENS_KEY, 10f);
        sensY = sensX; // Keep X/Y synced for this example
        mobileSensMultiplier = PlayerPrefs.GetFloat(MOBILE_SENS_KEY, 0.1f);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = sensX;
            sensitivitySlider.onValueChanged.AddListener(UpdatePCSensitivity);
        }

        if (mobileSensitivitySlider != null)
        {
            mobileSensitivitySlider.value = mobileSensMultiplier;
            mobileSensitivitySlider.onValueChanged.AddListener(UpdateMobileSensitivity);
        }

        UpdateCursorState();
    }

    private void Update()
    {
        if (!pv.IsMine) return;

        HandleInput();
        UpdateCursorState();

        x = Mathf.Clamp(x, -90f, 90f);

        cameraHolder.rotation = Quaternion.Euler(x, y, 0f);
        orientation.rotation = Quaternion.Euler(0, y, 0);
    }

    private void UpdateCursorState()
    {
        if (isInComputer)
        {
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !cursorLocked;

            if (Input.GetKeyDown(KeyCode.Escape) && Cursor.lockState == CursorLockMode.Locked)
            {
                cursorLocked = !cursorLocked;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleInput()
    {
        float mouseX, mouseY;

        if (isInComputer)
        {
            mouseX = Input.GetAxis("Mouse X") * sensX;
            mouseY = Input.GetAxis("Mouse Y") * sensY;
        }
        else
        {
            mouseX = lookAxis.x * mobileSensMultiplier * sensX;
            mouseY = lookAxis.y * mobileSensMultiplier * sensY;
        }

        y += mouseX;
        x -= mouseY;
    }

    public void UpdatePCSensitivity(float newSensitivity)
    {
        sensX = newSensitivity;
        sensY = newSensitivity;
        PlayerPrefs.SetFloat(PC_SENS_KEY, newSensitivity);
        PlayerPrefs.Save();
    }

    public void UpdateMobileSensitivity(float newSensitivity)
    {
        mobileSensMultiplier = newSensitivity;
        PlayerPrefs.SetFloat(MOBILE_SENS_KEY, newSensitivity);
        PlayerPrefs.Save();
    }
}
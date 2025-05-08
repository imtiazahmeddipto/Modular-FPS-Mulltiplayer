using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EZCameraShake;
using Photon.Pun;
using TMPro;

public class Gun : Weapons
{
    [Header("References")]
    [SerializeField] private Transform scopedTransform;
    [SerializeField] private Transform normalTransform;
    [Space]
    [SerializeField] private Camera cam;
    [Space]
    [SerializeField] private GameObject crossHair;
    [SerializeField] private ParticleSystem muzzleFlashPrefab;
    [SerializeField] private Animator animator;
    [Space]
    [SerializeField] private TMP_Text ammoText;
    [Space]
    [SerializeField] private PhotonView pv;

    [Header("Settings")]
    [SerializeField] private int magSize = 10;
    [SerializeField] private int amountLeft = 0;
    [SerializeField] private float reloadTime = 1f;
    [Space]
    [Range(0f, 10f)] [SerializeField] private float lerpTime = 10f;
    [Space]
    [SerializeField] private float scopedFOV = 42.5f;
    [SerializeField] private float normalFOV = 65f;
    [Space]
    [SerializeField] private float scopedSwaySpeed = 4f;
    [SerializeField] private float normalSwaySpeed = 8f;
    [Space]
    public bool automatic;

    [Header("Shooting")]
    //[SerializeField] private float shootForce = 3f;
    public float fireRate = 15f;
    [HideInInspector] public float nextTimeToFire = 0f;

    [Header("Camera Shake")]
    [SerializeField] private float intensity = 0.6f;
    [SerializeField] private float roughness = 0.8f;

    [Header("KeyCodes")]
    [SerializeField] private KeyCode shootKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode scopeKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    [Header("Script References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private WeaponSway sway;

    [Header("Debug")]
    public bool isScoped;
    public bool isShooting;
    public bool isReloading;

    private bool shootNonAuto;
    private bool shootAuto;
    private bool reload;
    public CameraShaker cameraShaker;
    private RaycastHit hit;
    [SerializeField] private AudioSource audioSource;
    public AudioClip shootSound;
    public GameObject HitParticle;
    [SerializeField] private GameObject hitMarkerUI;
    private Coroutine hitMarkerCoroutine;
    [Header("Equip Animation")]
    public bool isEquipping;

    private void Start()
    {
        amountLeft = magSize;
        if (pv.IsMine)
        {
            cameraShaker.enabled = true;
        }
    }
    private void OnEnable()
    {
        if (pv.IsMine)
        {
         StartCoroutine(EquipRoutine());
        }
    }
    private IEnumerator EquipRoutine()
    {
        isEquipping = true;
        animator.SetTrigger("Equip");
        yield return new WaitForSeconds(0.3f);
        isEquipping = false;
    }


    private void Update()
    {
        if (pv.IsMine)
        {
            shootNonAuto = Input.GetKeyDown(shootKey);
            shootAuto = Input.GetKey(shootKey);
            reload = playerMovement.ReloadButton;
            playerMovement.canSwitchGuns = !(isEquipping || isReloading || isShooting);
            if (isReloading || isShooting)
            {
                playerMovement.canSwitchGuns = false;
            }
            else
            {
                playerMovement.canSwitchGuns = true;
            }

            HandleScoping();

            if (automatic)
            {
                if (shootAuto)
                {
                    isShooting = true;
                }
                else
                {
                    isShooting = false;
                }
            }


            if (isScoped && !isReloading)
            {
                sway.speed = scopedSwaySpeed;
            }
            else
            {
                sway.speed = normalSwaySpeed;
            }

            if (!isReloading && amountLeft != 0 && amountLeft != magSize && reload)
            {
                Reload();
            }
            else if (!isReloading && amountLeft == 0)
            {
                Reload();
            }

            SetAmmoText();
        }
    }

    public override void Use()
    {
        //Debug.Log("Using gun: " + itemInfo.itemName);

        if (automatic)
        {
            Shoot();
            StartShootingAnimationAuto();
        }
        else
        {
            Shoot();
            StartShootingAnimationNonAuto();
        }
    }

    private void Shoot()
    {
        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out hit, 500f))
        {
            if (isReloading) return;

            // Check if the hit object is an enemy player
            PhotonView targetPV = hit.collider.GetComponent<PhotonView>();
            if (targetPV != null && !targetPV.IsMine)
            {
                // Show the hit marker
                ShowHitMarker();
            }

            // Apply damage if the object is damageable
            hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);

            // Instantiate hit effects (if needed)
            if (!hit.transform.CompareTag("Player"))
            {
                pv.RPC("Shoot_RPC", RpcTarget.All, hit.point, hit.normal);
            }
        }

        amountLeft--;
        pv.RPC("PlayMuzzleFlash", RpcTarget.All);

        cameraShaker.ShakeOnce(intensity, roughness, 0.3f, 0.3f);
    }

    private void ShowHitMarker()
    {
        if (hitMarkerUI != null)
        {
            // Reset the hit marker timer on every hit
            if (hitMarkerCoroutine != null)
            {
                StopCoroutine(hitMarkerCoroutine); // Stop the existing coroutine
            }
            hitMarkerUI.SetActive(true); // Ensure it's visible
            hitMarkerCoroutine = StartCoroutine(HideHitMarker()); // Start a new coroutine
        }
    }

    private IEnumerator HideHitMarker()
    {
        yield return new WaitForSeconds(0.15f); // Wait 0.15s AFTER THE LAST HIT
        hitMarkerUI.SetActive(false);
        hitMarkerCoroutine = null; // Reset the coroutine reference
    }

    [PunRPC]
    private void PlayMuzzleFlash()
    {
        muzzleFlashPrefab.Play();
        audioSource.PlayOneShot(shootSound);
    }


    private void Reload()
    {
        if (pv.IsMine)
        {
            isReloading = true;
            animator.SetBool("isReloading", true);
            Invoke("StopReload", reloadTime);
        }
    }

    private void StopReload()
    {
        if (pv.IsMine)
        {
            animator.SetBool("isReloading", false);
            amountLeft = magSize;
            isReloading = false;
        }
    }

    private bool canToggleScope = true; // Prevents multiple toggles per press

    private void HandleScoping()
    {
        if (pv.IsMine)
        {
            // Check if the button is pressed and it's allowed to toggle
            if (playerMovement.ScopeButton && canToggleScope)
            {
                isScoped = !isScoped; // Toggle scope state
                canToggleScope = false; // Prevents multiple toggles until button is released
            }

            // Reset toggle ability when the button is released
            if (!playerMovement.ScopeButton)
            {
                canToggleScope = true;
            }

            if (isScoped && !isReloading)
            {
                // Move camera to scoped position
                gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, scopedTransform.position, Time.deltaTime * 12f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, scopedFOV, Time.deltaTime * lerpTime);

                // Fade out crosshair
                Image img = crossHair.GetComponent<Image>();
                img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Lerp(img.color.a, 0f, Time.deltaTime * lerpTime));
            }
            else
            {
                // Move camera to normal position
                gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, normalTransform.position, Time.deltaTime * 12f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, normalFOV, Time.deltaTime * lerpTime);

                // Fade in crosshair
                Image img = crossHair.GetComponent<Image>();
                img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Lerp(img.color.a, 1f, Time.deltaTime * lerpTime));
            }
        }
    }




    public void StartShootingAnimationAuto()
    {
        if (pv.IsMine)
        {
            animator.SetTrigger("isShooting");
        }
    }

    public void StopShootingAnimationAuto()
    {
        if (pv.IsMine)
        {
            //animator.SetBool("isShooting", false);
            animator.gameObject.transform.position = new Vector3(animator.gameObject.transform.position.x, animator.gameObject.transform.position.y, normalTransform.position.z);
        }
    }

    public void StartShootingAnimationNonAuto()
    {
        if (pv.IsMine)
        {
            animator.SetTrigger("isShooting");
        }

    }

    public void StopShootingAnimationNonAuto()
    {
        if (pv.IsMine)
        {
            //animator.SetBool("isShooting", false);
            //animator.gameObject.transform.position = new Vector3(animator.gameObject.transform.position.x, animator.gameObject.transform.position.y, normalTransform.position.z);   
        }
    }

    private void SetAmmoText()
    {
        if (!isReloading)
        {
            ammoText.text = magSize + "/" + amountLeft;
        }
        else
        {
            ammoText.text = "RELOADING";
        }
    }

    [PunRPC]
    private void Shoot_RPC(Vector3 hitPosition, Vector3 hitNormal)
    {
        if (!playerMovement.gameObject.GetComponent<PhotonView>().IsMine)
            return;

        /*if (hit.rigidbody != null)
        {
            Vector3 forceDir = shootPoint.position - hit.point;
            hit.rigidbody.AddForce(-forceDir * shootForce);
        }*/

        if (hit.transform.CompareTag("OtherPlayer"))
        {
            Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);
            if (colliders.Length != 0)
            {
                GameObject bulletImpact = PhotonNetwork.Instantiate(bulletImpactPrefab.name, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal, Vector3.up) * bulletImpactPrefab.transform.rotation);
            }
        }

        if (!hit.transform.CompareTag("OtherPlayer"))
        {
            GameObject hitParticleInstance = PhotonNetwork.Instantiate(HitParticle.name, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal));
            Destroy(hitParticleInstance, .3f); // Destroy the hit particle after 2 seconds
        }
    }
}

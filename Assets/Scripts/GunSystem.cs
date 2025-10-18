using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class GunSystem : MonoBehaviour
{
    [Header("Setup")]
    public ItemData weaponData;
    public Transform firePoint;
    public Transform aimPosition;
    public Transform hipPosition;
    public TextMeshProUGUI ammoText;

    [Header("Runtime State")]
    private int currentAmmo;
    private bool isReloading = false;
    private AudioSource audioSource;

    [Header("Aiming / Hand Offset")]
    public Transform handSocket;
    public float normalX = 0.2f;
    public float aimX = 0f;
    public float aimTransitionSpeed = 10f;

    [Header("Recoil Settings")]
    public float recoilUp = 2f;
    public float recoilSide = 0.4f;
    public float recoilBack = 0.05f;
    public float recoilReturnSpeed = 8f;
    private Vector2 recoilAngles = Vector2.zero;
    private Vector2 recoilTarget = Vector2.zero;

    private Vector3 weaponRecoilOffset;
    private Vector3 targetWeaponRecoil;

    public float recoilRotationUp = 5f;
    public float recoilRotationSide = 2f;
    public float recoilSnappiness = 8f;
    public float recoilReturn = 4f;
    public float recoilMultiplier = 1f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    [Header("Camera & FOV")]
    public Camera playerCamera;
    public float normalFOV = 60f;
    public float aimFOV = 45f;
    public float fovTransitionSpeed = 8f;
    private Transform cameraTransform;

    private float nextShotTime = 0f;
    public enum FireMode { FullAuto, SemiAuto }
    public FireMode fireMode = FireMode.FullAuto;

    void Awake()
    {
        enabled = false;
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                playerCamera = cam;
                cameraTransform = cam.transform;
                Debug.Log($"[GunSystem] Caméra trouvée automatiquement : {cam.name}");
            }
            else
            {
                Debug.LogWarning("[GunSystem] Aucune caméra trouvée !");
            }
        }
        else
        {
            cameraTransform = playerCamera.transform;
        }

        currentAmmo = weaponData != null ? weaponData.ammoCapacity : 0;
    }

    void Update()
    {
        if (!weaponData || !weaponData.isGun) return;
        if (!gameObject.activeSelf) return;

        HandleShooting();
        HandleReload();
        UpdateUI();
    }

    void LateUpdate()
    {
        if (!weaponData || !weaponData.isGun) return;

        HandleAiming();
        HandleWeaponRecoil();
    }

    // === 🔫 SHOOTING ===
    void HandleShooting()
    {
        if (isReloading) return;
        if (!weaponData || !weaponData.isGun) return;

        // 🔊 Click quand vide
        if (currentAmmo <= 0)
        {
            if (Input.GetButtonDown("Fire1") && weaponData.emptyClickSound && audioSource != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(weaponData.emptyClickSound);
            }
            return;
        }

        bool wantsToShoot =
            (fireMode == FireMode.FullAuto && Input.GetButton("Fire1")) ||
            (fireMode == FireMode.SemiAuto && Input.GetButtonDown("Fire1"));

        if (!wantsToShoot) return;
        if (Time.time < nextShotTime) return;

        Shoot();
        nextShotTime = Time.time + weaponData.fireRate;
    }

    void Shoot()
    {
        if (weaponData.bulletPrefab == null)
        {
            Debug.LogError("[GunSystem] Aucun prefab de balle assigné !");
            return;
        }

        // Direction réelle du canon
        Vector3 shootDir = firePoint.forward;

        // ✅ Création de la balle
        GameObject bullet = Instantiate(weaponData.bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDir));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        // 🔒 Ignore collisions avec le joueur et l’arme
        Collider bulletCol = bullet.GetComponent<Collider>();
        Collider[] playerCols = GetComponentsInParent<Collider>();
        foreach (var col in playerCols)
        {
            if (bulletCol && col)
                Physics.IgnoreCollision(bulletCol, col);
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = shootDir * weaponData.bulletSpeed;
        }

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.speed = weaponData.bulletSpeed;

        if (weaponData.muzzleFlash)
        {
            ParticleSystem flash = Instantiate(weaponData.muzzleFlash, firePoint.position, firePoint.rotation);
            flash.Play();
            Destroy(flash.gameObject, 0.2f);
        }

        if (weaponData.fireSound && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(weaponData.fireSound);
        }

        currentAmmo--;

        // 🎯 Recoil
        float recoilVertical = Random.Range(recoilUp * 0.8f, recoilUp * 1.2f);
        float recoilHorizontal = Random.Range(-recoilSide, recoilSide);
        recoilTarget += new Vector2(recoilVertical, recoilHorizontal);

        float recoilX = Random.Range(recoilRotationUp * 0.8f, recoilRotationUp * 1.2f) * recoilMultiplier;
        float recoilY = Random.Range(-recoilRotationSide, recoilRotationSide) * recoilMultiplier;
        targetRotation += new Vector3(-recoilX, recoilY, 0f);

        // 🔙 petit recul physique du modèle
        targetWeaponRecoil += -Vector3.forward * recoilBack;
    }

    // === 🔁 RELOAD ===
    void HandleReload()
    {
        if (isReloading) return;
        if (Input.GetKeyDown(KeyCode.R))
            StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        if (isReloading) yield break;
        isReloading = true;

        Debug.Log("[GunSystem] Rechargement...");

        // 🔊 Son de rechargement
        if (weaponData.reloadSound && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(weaponData.reloadSound);
        }

        // Attente du rechargement
        yield return new WaitForSeconds(weaponData.reloadTime);

        int needed = weaponData.ammoCapacity - currentAmmo;

        // 🔎 Récupère l’inventaire
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[GunSystem] Aucun inventaire trouvé !");
            isReloading = false;
            yield break;
        }

        // 🧩 Consomme les munitions compatibles
        int collected = inv.ConsumeAmmo(weaponData.bulletType, needed);

        // Met à jour le chargeur
        currentAmmo += collected;
        currentAmmo = Mathf.Clamp(currentAmmo, 0, weaponData.ammoCapacity);

        if (collected > 0)
            Debug.Log($"[GunSystem] Rechargé {collected}/{needed} balles ({weaponData.bulletType})");
        else
            Debug.Log("[GunSystem] ❌ Aucune balle compatible trouvée !");

        isReloading = false;
    }

    // === 🎯 AIMING ===
    void HandleAiming()
    {
        bool aiming = Input.GetButton("Fire2");

        if (aimPosition != null && hipPosition != null)
        {
            Vector3 targetLocal = aiming ? aimPosition.localPosition : hipPosition.localPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocal + weaponRecoilOffset, Time.deltaTime * weaponData.aimSpeed);
        }

        if (handSocket != null)
        {
            Vector3 p = handSocket.localPosition;
            float targetX = aiming ? aimX : normalX;
            p.x = Mathf.Lerp(p.x, targetX, Time.deltaTime * aimTransitionSpeed);
            handSocket.localPosition = p;
        }

        if (playerCamera != null)
        {
            float targetFOV = aiming ? aimFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
        }
    }

    // === 💥 RECOIL VISUEL ===
    void HandleWeaponRecoil()
    {
        // Rotation visuelle de l’arme (indépendante de la caméra)
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, recoilReturn * Time.deltaTime);
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * recoilSnappiness);
        transform.localRotation = Quaternion.Euler(currentRotation);

        // Mouvement de recul (vers l’arrière)
        weaponRecoilOffset = Vector3.Lerp(weaponRecoilOffset, targetWeaponRecoil, Time.deltaTime * 10f);
        targetWeaponRecoil = Vector3.Lerp(targetWeaponRecoil, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
    }

    // === 💬 UI ===
    void UpdateUI()
    {
        if (ammoText)
            ammoText.text = $"{currentAmmo} / {weaponData.ammoCapacity}";
    }

    public void EquipWeapon(ItemData data)
    {
        weaponData = data;
        enabled = true;
        currentAmmo = weaponData.ammoCapacity;
        if (hipPosition)
            transform.localPosition = hipPosition.localPosition;
        if (handSocket)
        {
            Vector3 p = handSocket.localPosition;
            p.x = normalX;
            handSocket.localPosition = p;
        }
        nextShotTime = Time.time + 0.05f;
    }

    public void DisableWeapon() => gameObject.SetActive(false);
}

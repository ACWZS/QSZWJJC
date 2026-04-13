using UnityEngine;
using System.Collections;
using Photon.Pun;

public class Gun : MonoBehaviour
{
    [Header("枪械参数")]
    public float damage = 0f;
    public float fireRate = 0f;
    public int maxAmmo = 0;
    public int currentAmmo = 0;
    public int totalAmmo = 0;
    public float reloadTime = 2f;
    public bool autoReload = true;

    [Header("子弹")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("射击效果")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;

    private float nextFireTime = 0f;
    private AudioSource audioSource;
    public bool isReloading = false;
    private Coroutine reloadCoroutine;

    public bool IsCoolingDown => Time.time < nextFireTime;
    public float GetNextFireTime() => nextFireTime;

    private PhotonView playerPhotonView;

    void Awake()
{
    audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
        audioSource = gameObject.AddComponent<AudioSource>();
    audioSource.enabled = true;
    audioSource.playOnAwake = false;
    audioSource.volume = 1f;
}

    void Start()
    {
        currentAmmo = maxAmmo;
        if (PhotonNetwork.InRoom && PlayerMain.Instance != null)
        {
            playerPhotonView = PlayerMain.Instance.GetComponent<PhotonView>();
            if (playerPhotonView == null)
                Debug.LogError("PlayerMain has no PhotonView!");
        }
    }

    public bool TryFire()
    {
        if (isReloading) return false;
        if (Time.time < nextFireTime) return false;
        if (currentAmmo <= 0)
        {
            if (autoReload)
                TryReload();
            return false;
        }

        FireWithDirection(firePoint.forward);
        return true;
    }

    public bool TryReload()
    {
        if (!gameObject.activeInHierarchy) return false;
        if (isReloading) return false;
        if (currentAmmo == maxAmmo) return false;
        if (totalAmmo <= currentAmmo) return false;

        isReloading = true;
        PlayerMain.Instance?.OnReloadStart();

        string weaponName = PlayerMain.Instance.CurrentWeaponData?.weaponName;
        if (!string.IsNullOrEmpty(weaponName))
        {
            PlayerMain.Instance.RegisterReload(weaponName, reloadTime);
        }

        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
        return true;
    }

    private IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(reloadTime);

        int needed = maxAmmo - currentAmmo;
        int available = totalAmmo - currentAmmo;
        int toTake = Mathf.Min(needed, available);
        currentAmmo += toTake;

        isReloading = false;
        PlayerMain.Instance?.OnReloadEnd();
        PlayerMain.Instance?.UpdateAmmoUI();
        reloadCoroutine = null;
    }

    public void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        isReloading = false;
        PlayerMain.Instance?.OnReloadEnd();
    }

    void OnDisable()
    {
        CancelReload();
    }

    public void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.enabled = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
    }

    public void SetWeaponData(WeaponData data, bool resetAmmo = true)
    {
        damage = data.damage;
        fireRate = data.fireRate;
        maxAmmo = data.maxAmmo;
        reloadTime = data.reloadTime;
        totalAmmo = data.totalAmmo;
        bulletPrefab = data.bulletPrefab;
        shootSound = data.shootSound;
        isReloading = false;

        if (resetAmmo)
            currentAmmo = maxAmmo;

        PlayerMain.Instance?.UpdateAmmoUI();
    }

    public void FireWithWeaponData(WeaponData data, Vector3 direction)
    {
        if (isReloading) return;
        if (Time.time < nextFireTime) return;
        if (currentAmmo <= 0)
        {
            if (autoReload)
                TryReload();
            return;
        }
        nextFireTime = Time.time + Mathf.Max(0.05f, fireRate);
        currentAmmo--;

        if (audioSource != null && !audioSource.enabled)
            audioSource.enabled = true;

        // 本地播放音效
        if (audioSource != null && audioSource.enabled && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        // 通知其他客户端播放音效（仅联机模式）
        if (PhotonNetwork.InRoom && PlayerMain.Instance != null && shootSound != null)
        {
            PhotonView playerPhotonView = PlayerMain.Instance.GetComponent<PhotonView>();
            if (playerPhotonView != null)
            {
                // 改为传递武器索引
                int weaponIdx = PlayerMain.Instance.CurrentWeaponIndex;
                playerPhotonView.RPC("RPC_PlayShootSound", RpcTarget.Others, weaponIdx);
            }
        }

        switch (data.type)
        {
            case WeaponType.Pistol:
            case WeaponType.Sniper:
                FireSingleBullet(direction, data.damage);
                break;
            case WeaponType.ShotgunPistol:
                FireShotgun(direction, data.pelletCount, data.spreadAngle, data.damage);
                break;
            case WeaponType.Shotgun:
                FireShotgun(direction, data.pelletCount, data.spreadAngle, data.damage);
                break;
            case WeaponType.RocketLauncher:
                FireRocket(direction, data.damage);
                break;
        }

        PlayerMain.Instance?.UpdateAmmoUI();

        if (currentAmmo == 0 && autoReload && gameObject.activeInHierarchy)
            TryReload();
    }

    private void FireWithDirection(Vector3 direction)
    {
        if (bulletPrefab == null || firePoint == null) return;
        if (currentAmmo <= 0) return;

        nextFireTime = Time.time + Mathf.Max(0.05f, fireRate);
        currentAmmo--;

        if (audioSource != null && !audioSource.enabled)
            audioSource.enabled = true;
        if (audioSource != null && audioSource.enabled && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        FireSingleBullet(direction, damage);

        PlayerMain.Instance?.UpdateAmmoUI();

        if (currentAmmo == 0 && autoReload)
            TryReload();
    }

    private void FireSingleBullet(Vector3 direction, float dmg)
    {
        if (direction == Vector3.zero || bulletPrefab == null) return;

        Vector3 spawnPos = firePoint.position + direction * 0.2f;
        Quaternion bulletRot = Quaternion.LookRotation(direction);
        string bulletPrefabName = bulletPrefab.name;

        if (PhotonNetwork.InRoom && playerPhotonView != null)
        {
            // 本地生成
            GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRot);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
                bulletScript.Initialize(dmg, transform.root.gameObject, direction);

            // 通知其他客户端
            int shooterViewID = transform.root.GetComponent<PhotonView>().ViewID;
            playerPhotonView.RPC("RPC_FireSingleBullet", RpcTarget.Others, dmg, shooterViewID, direction, spawnPos, bulletPrefabName);
        }
        else
        {
            GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRot);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
                bulletScript.Initialize(dmg, transform.root.gameObject, direction);
        }
    }

    private void FireShotgun(Vector3 direction, int pelletCount, float spreadAngle, float baseDamage)
    {
        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spreadDir = GetSpreadDirection(direction, spreadAngle);
            FireSingleBullet(spreadDir, baseDamage / pelletCount);
        }
        PlayerMain.Instance?.UpdateAmmoUI();
        if (currentAmmo == 0 && autoReload)
            TryReload();
    }

    private Vector3 GetSpreadDirection(Vector3 baseDir, float angleDeg)
    {
        float randomY = Random.Range(-angleDeg, angleDeg);
        float randomX = Random.Range(-angleDeg, angleDeg);
        Quaternion rotation = Quaternion.Euler(randomX, randomY, 0);
        return rotation * baseDir;
    }

    private void FireRocket(Vector3 direction, float dmg)
    {
        if (bulletPrefab == null || firePoint == null) return;

        Vector3 spawnPos = firePoint.position + direction * 0.2f;
        Quaternion bulletRot = Quaternion.LookRotation(direction);
        string rocketPrefabName = bulletPrefab.name;

        if (PhotonNetwork.InRoom && playerPhotonView != null)
        {
            GameObject rocket = Instantiate(bulletPrefab, spawnPos, bulletRot);
            Rocket rocketScript = rocket.GetComponent<Rocket>();
            if (rocketScript != null)
                rocketScript.Initialize(transform.root.gameObject, direction);

            int shooterViewID = transform.root.GetComponent<PhotonView>().ViewID;
            playerPhotonView.RPC("RPC_FireRocket", RpcTarget.Others, shooterViewID, direction, spawnPos, rocketPrefabName);
        }
        else
        {
            GameObject rocket = Instantiate(bulletPrefab, spawnPos, bulletRot);
            Rocket rocketScript = rocket.GetComponent<Rocket>();
            if (rocketScript != null)
                rocketScript.Initialize(transform.root.gameObject, direction);
        }
    }

    public void ResetFireTime()
    {
        nextFireTime = 0f;
    }

    public void Reload()
    {
        TryReload();
    }

    public void BlockShooting(float duration)
    {
        nextFireTime = Mathf.Max(nextFireTime, Time.time + duration);
    }
}
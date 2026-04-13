using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;
    public WeaponType type;

    [Header("Weapon Stats")]
    public float damage = 20f;
    public float fireRate = 0.5f;          // Éä»÷¼ä¸ô£¨Ãë£©
    public float reloadTime = 2f;           // »»µ¯Ê±¼ä£¨Ãë£©
    public int maxAmmo = 30;                // µ¯¼ÐÈÝÁ¿
    public int totalAmmo = 30;              // ×Üµ¯Ò©£¨°üº¬µ¯¼ÐÄÚ£©
    public GameObject bulletPrefab;

    [Header("Shotgun")]
    public int pelletCount = 1;
    public float spreadAngle = 0f;

    [Header("Audio")]
    public AudioClip shootSound;
}

public enum WeaponType
{
    Pistol,
    ShotgunPistol,
    Shotgun,
    Sniper,
    RocketLauncher
}
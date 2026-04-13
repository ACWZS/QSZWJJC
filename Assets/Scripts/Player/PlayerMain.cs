using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class PlayerMain : MonoBehaviour
{
    public static PlayerMain Instance { get; private set; }

    [Header("移动参数")]
    public float speed = 1.2f;
    public float jumpHeight = 0.5f;
    public float rotationSpeed = 1000f;

    [Header("相机引用")]
    public Transform cameraTransform;

    [Header("移动端控制")]
    public Joystick moveJoystick;
    public Button jumpButton;

    [Header("武器系统")]
    public List<WeaponData> weapons;
    public Transform weaponContainer;

    [Header("瞄准线")]
    public LineRenderer aimLine;
    public float aimLineDistance = 5f;
    public Vector3 aimLineOffset = new Vector3(0, 0.15f, 0);

    [Header("自动瞄准")]
    public float autoAimRange = 4f;

    [Header("动画")]
    public float attackAnimDuration = 0.5f;

    [Header("调试")]
    public bool simulateMobileInEditor = true;

    [Header("射击")]
    public KeyCode fireKey = KeyCode.Mouse0;

    [Header("重生")]
    public Transform respawnPoint;

    [Header("狙击枪开镜")]
    public float scopedFOV = 3f;
    public float normalFOV = 2f;
    public float scopeTransitionSpeed = 5f;
    private bool isScoping = false;

    [Header("辅助瞄准")]
    public float aimAssistRadius = 5f;
    public float aimAssistAngle = 15f;
    public float aimAssistSmooth = 0.2f;
    private bool aimAssistActive = false;

    [Header("治疗")]
    private Coroutine _currentHealCoroutine;
    //private float _maxHealth;
    private PlayerStats _stats;
    private float lastSpeedPrintTime = 0f;

    // 内部状态
    private CharacterController _controller;
    private Animator _animator;
    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _jumpPressed;

    private Dictionary<string, GameObject> _weaponModels = new Dictionary<string, GameObject>();
    private Dictionary<string, Gun> _weaponGuns = new Dictionary<string, Gun>();
    private int _currentWeaponIndex = 0;
    private WeaponData _currentWeaponData;
    private Gun _currentGun;
    private GameObject _gunModel;
    public Dictionary<string, GameObject> WeaponModels => _weaponModels;
    private Dictionary<string, (int currentAmmo, int totalAmmo)> weaponAmmoStates = new Dictionary<string, (int currentAmmo, int totalAmmo)>();
    private Dictionary<string, float> weaponReloadEndTimes = new Dictionary<string, float>();
    private Dictionary<string, int> weaponNameToIndex = new Dictionary<string, int>();
    private int _pendingWeaponIndex = -1;

    private int _fireTouchFingerId = -1;    // 射击按钮触摸手指ID
    private bool _isTrackingFireTouch = false;
    private Vector2 _lastDragScreenPos;      // 上一帧拖拽的屏幕坐标

    // 攻击状态
    private bool _isAttacking = false;
    private float _attackAnimEndTime = 0f;
    private bool _isInContinuousAttack = false;
    private bool _isSingleShotPending = false;
    private bool _isFireButtonTouching = false;

    // 移动端瞄准状态
    private bool _isAiming = false;
    private Vector3 _aimDirection;
    private bool _isLongPressConfirmed = false;
    private float _mobileDownTime = 0f;
    private Coroutine _longPressCoroutine;
    private Vector2 _currentButtonCenterScreenPos;

    public const float Gravity = -9.81f;

    // 公共属性
    public CharacterController Controller => _controller;
    public Animator Animator => _animator;
    public Vector3 Velocity { get => _velocity; set => _velocity = value; }
    public bool IsGrounded { get => _isGrounded; set => _isGrounded = value; }
    public bool JumpPressed { get => _jumpPressed; set => _jumpPressed = value; }
    public int CurrentWeaponIndex => _currentWeaponIndex;
    public WeaponData CurrentWeaponData => _currentWeaponData;
    public Gun CurrentGun => _currentGun;
    public GameObject GunModel => _gunModel;
    public bool IsAttacking { get => _isAttacking; set => _isAttacking = value; }
    public float AttackAnimEndTime { get => _attackAnimEndTime; set => _attackAnimEndTime = value; }
    public bool IsInContinuousAttack { get => _isInContinuousAttack; set => _isInContinuousAttack = value; }
    public bool IsSingleShotPending { get => _isSingleShotPending; set => _isSingleShotPending = value; }
    public bool IsAiming { get => _isAiming; set => _isAiming = value; }
    public Vector3 AimDirection { get => _aimDirection; set => _aimDirection = value; }
    public bool IsLongPressConfirmed { get => _isLongPressConfirmed; set => _isLongPressConfirmed = value; }
    public float MobileDownTime { get => _mobileDownTime; set => _mobileDownTime = value; }
    public Coroutine LongPressCoroutine { get => _longPressCoroutine; set => _longPressCoroutine = value; }

    private enum HealType { None, EnergyDrink, Bandage, Medkit }
    private HealType _currentHealType = HealType.None;
    private float _nextEnergyDrinkTime = 0f;   // 能量饮料冷却结束时间

    // 联机
    private PhotonView _photonView;
    private Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();
    private int killsThisMatch = 0;   // 本局获得的击杀数

    public Vector2 _moveInput = Vector2.zero;  // 存储 UI 传入UseEnergyDrink的移动方向

    // 公共方法
    public static int LastMatchKills { get;set; }
    public static void ResetLastMatchKills()
    {
        LastMatchKills = 0;
    }
    public void AddKill()
    {
        killsThisMatch++;
        LastMatchKills = killsThisMatch;
        //Debug.Log($"[{_photonView.Owner}] 击杀数增加，当前={killsThisMatch}");
        if (PlayerUI.Instance != null)
            PlayerUI.Instance.UpdateInGameKillCount(killsThisMatch);
    }

    [PunRPC]
    public void RPC_AddKill()
    {
        // 确保只有攻击者自己执行
        if (_photonView.IsMine)
        {
            AddKill();
        }
    }

    public int GetKillsThisMatch() => killsThisMatch;

    public void ResetKillsThisMatch() => killsThisMatch = 0;


    // 添加公共方法供 UI 调用
    public void OnMoveInput(Vector2 direction)
    {
        _moveInput = direction;
    }

    public void OnJumpPressed()
    {
        _jumpPressed = true;
    }

    void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        InitializeWeapons();

        if (_photonView.IsMine)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void InitializeWeapons()
    {
        if (weaponContainer == null) return;

        _weaponModels.Clear();
        _weaponGuns.Clear();
        audioClipCache.Clear();

        foreach (Transform child in weaponContainer)
        {
            string childName = child.name;
            _weaponModels[childName] = child.gameObject;
            Gun gun = child.GetComponent<Gun>();
            if (gun != null)
            {
                _weaponGuns[childName] = gun;
                gun.InitializeAudio();   // ★ 关键：为远程客户端启用 AudioSource
            }
            child.gameObject.SetActive(false);
        }

        // 预加载所有武器的音效（直接从 WeaponData 引用）
        foreach (WeaponData data in weapons)
        {
            if (data != null && data.shootSound != null && !audioClipCache.ContainsKey(data.shootSound.name))
            {
                audioClipCache[data.shootSound.name] = data.shootSound;
            }
        }
    }

    public void UpdateAmmoUI()
    {
        if (PlayerUI.Instance != null)
            PlayerUI.Instance.UpdateAllAmmoDisplays();
    }

    void Start()
    {
        PhotonNetwork.SendRate = 30;          // 控制数据发送频率，默认是20，改为30会更流畅[reference:4]
        PhotonNetwork.SerializationRate = 30; // 控制序列化频率，与SendRate保持一致，确保数据能及时处理[reference:5]

        _stats = GetComponent<PlayerStats>();

        // 非本地玩家不执行任何初始化
        if (_photonView != null && !_photonView.IsMine)
            return;

        speed = 1.2f;

        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null) cameraTransform = mainCam.transform;
            else Debug.LogError("未找到主相机！");
        }

        Health playerHealth = GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.OnDeath.AddListener(OnPlayerDeath);
            Debug.Log("PlayerMain subscribed to OnDeath event");
        }

        if (aimLine != null) aimLine.enabled = false;

        if (weaponContainer != null)
        {
            foreach (Transform child in weaponContainer)
            {
                string childName = child.name;
                _weaponModels[childName] = child.gameObject;
                Gun gun = child.GetComponent<Gun>();
                if (gun != null) _weaponGuns[childName] = gun;
                else Debug.LogWarning($"武器 {childName} 没有 Gun 组件！");
                child.gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponData data = weapons[i];
            if (data == null) continue;
            if (_weaponGuns.TryGetValue(data.weaponName, out Gun gun))
            {
                gun.SetWeaponData(data, resetAmmo: true);
            }
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
                weaponNameToIndex[weapons[i].weaponName] = i;
        }

        // 如果 moveJoystick 为空，尝试在场景中查找（仅本地玩家）
        if (moveJoystick == null)
        {
            moveJoystick = FindObjectOfType<Joystick>();
            if (moveJoystick == null)
                Debug.LogWarning("未找到 Joystick，请确保场景中有 Joystick 组件");
        }

        if (_photonView != null && _photonView.IsMine)
        {
            killsThisMatch = 0;
            LastMatchKills = 0;  // 新增
            if (PlayerUI.Instance != null)
                PlayerUI.Instance.UpdateInGameKillCount(0);
        }

        // 同步速度属性
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            speed = stats.CurrentSpeed;

        // 订阅速度变化
        if (stats != null)
            stats.OnSpeedChanged.AddListener(() => speed = stats.CurrentSpeed);

        UpdateAmmoUI();
    }

    void Update()
    {
        // 如果不是本地玩家，不执行任何输入相关逻辑
        if (_photonView != null && !_photonView.IsMine)
            return;

        // 每2秒打印一次速度信息（仅本地玩家，因为非本地玩家已返回）
        if (Time.time - lastSpeedPrintTime >= 2f)
        {
            lastSpeedPrintTime = Time.time;
        }

        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        PlayerMovement.UpdateMovement();
        PlayerCombat.UpdateCombat();
        PlayerAiming.UpdateAiming();

        if (!simulateMobileInEditor)
        {
            // 治疗物品快捷键（数字4、5、6）
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                UseEnergyDrink();
            }
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                UseBandage();
            }
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                UseMedkit();
            }

            // 武器切换快捷键
            // 数字1-3对应武器1-3 (索引1-3)
            for (int i = 1; i <= 3 && i < weapons.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    SwitchToWeapon(i);
                    StartSingleAttack();
                    break;
                }
            }
            // 数字0对应武器4 (索引4)
            if (weapons.Count > 4)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
                {
                    SwitchToWeapon(4);
                    StartSingleAttack();
                }
            }
        }

        if (transform.position.y < -10f)
            Die();

        HandleScopeInput();
        UpdateAimAssist();
        UpdateBackgroundReloads();

        if (_isAiming && _aimDirection.magnitude > 0.1f)
        {
            Vector3 horizontalDir = _aimDirection;
            horizontalDir.y = 0;
            if (horizontalDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        if (_isLongPressConfirmed && _isAiming && _gunModel != null && !_gunModel.activeSelf)
            _gunModel.SetActive(true);

        if (!_isFireButtonTouching)
        {
            if (_isAiming || _isLongPressConfirmed || isScoping)
            {
                _isAiming = false;
                _isLongPressConfirmed = false;
                // 强制关闭开镜，无论当前武器是什么
                isScoping = false;
                if (_longPressCoroutine != null)
                {
                    StopCoroutine(_longPressCoroutine);
                    _longPressCoroutine = null;
                }
            }
        }

        PlayerActions actions = GetComponent<PlayerActions>();
        if (actions != null && actions.IsBusy)
        {
            bool keyboardMove = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
            bool uiMove = (Mathf.Abs(_moveInput.x) > 0.1f || Mathf.Abs(_moveInput.y) > 0.1f);
            bool isMoving = keyboardMove || uiMove;
            bool isAttacking = _isAttacking || _isInContinuousAttack;
            if (isMoving || isAttacking)
            {
                actions.CancelAction();
                PlayerUI.Instance?.HideInfo();
                // 重置绷带/急救包按钮文字
                PlayerUI.Instance?.UpdateBandageTimer(0f);
                PlayerUI.Instance?.UpdateMedkitTimer(0f);
            }
        }

        // 处理长按后的自由瞄准拖拽（独立于 UI 事件）
        if (_isLongPressConfirmed && _isTrackingFireTouch)
        {
            Vector2 currentScreenPos = Vector2.zero;   // 必须初始化
            bool gotTouch = false;

            if (_fireTouchFingerId != -1 && Input.touchCount > 0)
            {
                foreach (Touch t in Input.touches)
                {
                    if (t.fingerId == _fireTouchFingerId)
                    {
                        currentScreenPos = t.position;
                        gotTouch = true;
                        break;
                    }
                }
            }
            else
            {
                // 编辑器模拟或鼠标
                currentScreenPos = Input.mousePosition;
                gotTouch = true;
            }

            if (gotTouch)
            {
                Vector2 delta = currentScreenPos - _lastDragScreenPos;
                if (delta.magnitude > 0.5f) // 移动阈值
                {
                    // 将屏幕偏移转换为世界方向（基于相机）
                    Vector3 worldDelta = (cameraTransform.right * delta.x + cameraTransform.up * delta.y).normalized;
                    // 混合当前瞄准方向，避免突变
                    _aimDirection = (_aimDirection + worldDelta * 0.1f).normalized;
                    _lastDragScreenPos = currentScreenPos;
                }
            }
        }
    }

    // 治疗相关方法
    public void UseEnergyDrink()
    {
        if (Time.time < _nextEnergyDrinkTime)
        {
            PlayerUI.Instance?.ShowInfo("能量饮料冷却中", 0.5f);
            return;
        }
        InterruptHeal(true);
        // 速度变为原来的 1.3 倍
        speed = 1.56f;
        _currentHealType = HealType.EnergyDrink;
        _currentHealCoroutine = StartCoroutine(EnergyDrinkCoroutine());
        _nextEnergyDrinkTime = Time.time + 5f;
    }

    public void UseBandage()
    {
        PlayerActions actions = GetComponent<PlayerActions>();
        if (actions == null || actions.IsBusy) return;

        actions.StartHeal(4f, 35f, true,
            onComplete: () =>
            {
                PlayerUI.Instance?.ShowInfo("绷带治疗完成");
                PlayerUI.Instance?.UpdateBandageTimer(0f);      // 重置按钮文字
            },
            onProgress: (progress) =>
            {
                float remaining = 4f * (1f - progress);
                PlayerUI.Instance?.UpdateBandageTimer(remaining);
            });
        PlayerUI.Instance?.ShowInfo("使用绷带中...4秒后治疗35%");
    }

    public void UseMedkit()
    {
        PlayerActions actions = GetComponent<PlayerActions>();
        if (actions == null || actions.IsBusy) return;

        actions.StartHeal(7f, 70f, true,
            onComplete: () =>
            {
                PlayerUI.Instance?.ShowInfo("急救包治疗完成");
                PlayerUI.Instance?.UpdateMedkitTimer(0f);       // 重置按钮文字
            },
            onProgress: (progress) =>
            {
                float remaining = 7f * (1f - progress);
                PlayerUI.Instance?.UpdateMedkitTimer(remaining);
            });
        PlayerUI.Instance?.ShowInfo("使用急救包中...7秒后治疗70%");
    }



    private void InterruptHeal(bool interruptEnergyDrink = false)
    {
        if (interruptEnergyDrink && _currentHealType == HealType.EnergyDrink)
        {
            if (_currentHealCoroutine != null)
            {
                StopCoroutine(_currentHealCoroutine);
                _currentHealCoroutine = null;
            }
            speed = 1.2f;   // 恢复基础速度
            _currentHealType = HealType.None;
            //PlayerUI.Instance?.UpdateEnergyDrinkButtonText("能量饮料");
            PlayerUI.Instance?.HideInfo();
        }
        else if (_currentHealType == HealType.Bandage || _currentHealType == HealType.Medkit)
        {
            if (_currentHealCoroutine != null)
            {
                StopCoroutine(_currentHealCoroutine);
                _currentHealCoroutine = null;
            }
            _currentHealType = HealType.None;
            PlayerUI.Instance?.UpdateBandageButtonText("");
            PlayerUI.Instance?.UpdateMedkitButtonText("");
            PlayerUI.Instance?.HideInfo();
        }
    }

    private IEnumerator EnergyDrinkCoroutine()
    {
        float elapsed = 0f;
        float duration = 6f;
        float tickInterval = 1f;
        float nextTick = 0f;
        Health health = GetComponent<Health>();
        if (health == null || _stats == null) yield break;

        while (elapsed < duration)
        {
            PlayerUI.Instance?.UpdateEnergyDrinkTimer(duration - elapsed - 1);
            if (Time.time >= nextTick)
            {
                // 改为使用 PlayerStats.MaxHealth 计算百分比治疗量
                float healAmount = _stats.MaxHealth * 0.05f;
                health.Heal(healAmount);
                nextTick = Time.time + tickInterval;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        PlayerUI.Instance?.UpdateEnergyDrinkTimer(0);
        speed = 1.2f;
        _currentHealCoroutine = null;
        _currentHealType = HealType.None;
        PlayerUI.Instance?.HideInfo();
    }

    private IEnumerator BandageCoroutine()
    {
        float delay = 4f;
        float remaining = delay;
        Health health = GetComponent<Health>();
        if (health == null || _stats == null) yield break;

        while (remaining > 0)
        {
            PlayerUI.Instance?.UpdateBandageTimer(remaining);
            remaining -= Time.deltaTime;
            yield return null;
        }
        PlayerUI.Instance?.UpdateBandageTimer(0);

        if (_currentHealType == HealType.Bandage)
        {
            // 使用百分比治疗（Health.Heal 已支持 isPercent 参数）
            health.Heal(30f, isPercent: true);
        }
        _currentHealCoroutine = null;
        _currentHealType = HealType.None;
        PlayerUI.Instance?.HideInfo();
    }

    private IEnumerator MedkitCoroutine()
    {
        float delay = 8f;
        float remaining = delay;
        Health health = GetComponent<Health>();
        if (health == null || _stats == null) yield break;

        while (remaining > 0)
        {
            PlayerUI.Instance?.UpdateMedkitTimer(remaining);
            remaining -= Time.deltaTime;
            yield return null;
        }
        PlayerUI.Instance?.UpdateMedkitTimer(0);

        if (_currentHealType == HealType.Medkit)
        {
            health.Heal(80f, isPercent: true);
        }
        _currentHealCoroutine = null;
        _currentHealType = HealType.None;
        PlayerUI.Instance?.HideInfo();
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;
        float target = isScoping ? scopedFOV : normalFOV;
        if (Camera.main.orthographic)
            Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, target, Time.deltaTime * scopeTransitionSpeed);
        else
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, target, Time.deltaTime * scopeTransitionSpeed);
    }

    private void HandleScopeInput()
    {
        bool isSniper = _currentWeaponData != null && _currentWeaponData.type == WeaponType.Sniper;
        if (!isSniper)
        {
            if (isScoping) isScoping = false;
            return;
        }

        bool pcScope = Input.GetMouseButton(1);
        bool mobileScope = _isLongPressConfirmed && isSniper;

        if (pcScope || mobileScope)
        {
            if (!isScoping) isScoping = true;
        }
        else
        {
            if (isScoping) isScoping = false;
        }
    }

    private void UpdateAimAssist()
    {
        bool isManualAiming = _isAiming || isScoping;
        if (isManualAiming && _currentGun != null && _currentGun.currentAmmo > 0)
            aimAssistActive = true;
        else
            aimAssistActive = false;
    }

    private void OnPlayerDeath()
    {
        Die();
    }

    public void SwitchToWeapon(int index)
    {
        if (_isSingleShotPending) EndSingleShot();
        if (_isInContinuousAttack) EndContinuousAttack();
        isScoping = false;
        _isLongPressConfirmed = false;
        _isAiming = false;
        if (_longPressCoroutine != null)
        {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
        }

        if (index == _currentWeaponIndex) return; // 相同武器不处理

        if (_currentGun != null)
        {
            _currentGun.BlockShooting(0f);
            _currentGun.isReloading = false;
        }

        if (index == _currentWeaponIndex) return;
        if (index < 0 || index >= weapons.Count) return;

        if (_currentGun != null && _currentWeaponData != null)
        {
            string name = _currentWeaponData.weaponName;
            weaponAmmoStates[name] = (_currentGun.currentAmmo, _currentGun.totalAmmo);
        }

        foreach (var model in _weaponModels.Values)
            model.SetActive(false);

        _currentWeaponIndex = index;
        _currentWeaponData = weapons[_currentWeaponIndex];
        string weaponName = _currentWeaponData.weaponName;

        if (_weaponModels.TryGetValue(weaponName, out GameObject foundModel))
        {
            _currentGun = _weaponGuns[weaponName];
            _gunModel = foundModel;
            if (_currentGun != null)
            {
                AudioSource asrc = _currentGun.GetComponent<AudioSource>();
                if (asrc != null) asrc.enabled = true;

                _currentGun.SetWeaponData(_currentWeaponData, false);

                if (weaponAmmoStates.TryGetValue(weaponName, out var ammoState))
                {
                    _currentGun.currentAmmo = ammoState.currentAmmo;
                    _currentGun.totalAmmo = ammoState.totalAmmo;
                }
                else
                {
                    _currentGun.currentAmmo = _currentGun.maxAmmo;
                }

                _currentGun.ResetFireTime();
            }
        }

        if (_currentGun != null)
        {
            if (_currentGun.currentAmmo == 0 && _currentGun.autoReload)
                _currentGun.TryReload();
            else
                PlayerUI.Instance?.SetFireButtonInteractable(_currentWeaponIndex, true);
        }

        UpdateAmmoUI();
    }

    private void AimAtTarget()
    {
        Vector3 attackDir = PlayerAiming.GetAttackDirection();
        if (aimAssistActive)
            attackDir = PlayerAiming.GetAimAssistedDirection(attackDir, transform.position, aimAssistRadius, aimAssistAngle, aimAssistSmooth);

        if (attackDir.magnitude > 0.1f)
        {
            Vector3 horizontalDir = attackDir;
            horizontalDir.y = 0;
            if (horizontalDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalDir);
                transform.rotation = targetRot;
            }
        }
    }

    public void PerformSingleShot()
    {
        Vector3 attackDir = PlayerAiming.GetAttackDirection();

        if (attackDir == Vector3.zero)
            attackDir = transform.forward;

        if (aimAssistActive)
            attackDir = PlayerAiming.GetAimAssistedDirection(attackDir, transform.position, aimAssistRadius, aimAssistAngle, aimAssistSmooth);

        if (attackDir.magnitude > 0.1f)
        {
            Vector3 horizontalDir = attackDir;
            horizontalDir.y = 0;
            if (horizontalDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalDir);
                transform.rotation = targetRot;
            }
        }

        if (_currentGun != null && _currentWeaponData != null)
            _currentGun.FireWithWeaponData(_currentWeaponData, attackDir);

        if (_currentWeaponData != null && (_currentWeaponData.type == WeaponType.ShotgunPistol || _currentWeaponData.type == WeaponType.Shotgun))
        {
            if (_hideModelCoroutine != null)
                StopCoroutine(_hideModelCoroutine);
            _hideModelCoroutine = StartCoroutine(HideWeaponModelDelay(1f));
        }
    }

    public void StartSingleAttack()
    {
        if (_isInContinuousAttack)
        {
            //Debug.Log("[StartSingleAttack] 被连续攻击状态阻止");
            return;
        }

        if (_currentGun != null && _currentGun.IsCoolingDown)
        {
            //Debug.Log($"[StartSingleAttack] 冷却中: nextFireTime={_currentGun.GetNextFireTime()}, currentTime={Time.time}");
            return;
        }

        if (_currentGun != null && _currentGun.currentAmmo <= 0)
        {
            //Debug.Log("[StartSingleAttack] 弹药不足");
            if (_currentGun.autoReload) _currentGun.TryReload();
            return;
        }

        // 只有当前处于单发待机状态且动画时间已过，才强制结束
        if (_isSingleShotPending && Time.time >= _attackAnimEndTime)
            EndSingleShot();

        if (_hideModelCoroutine != null)
        {
            StopCoroutine(_hideModelCoroutine);
            _hideModelCoroutine = null;
        }
        if (_gunModel != null) _gunModel.SetActive(true);

        PlayAttackAnimation();
        _isSingleShotPending = true;

        AimAtTarget();
        PerformSingleShot();
    }

    public void StartContinuousAttack()
    {
        if (_isInContinuousAttack) return;
        _isInContinuousAttack = true;
        if (_animator != null && !_isAttacking)
        {
            _isAttacking = true;
            _animator.SetBool("isAttacking", true);
            if (_gunModel != null) _gunModel.SetActive(true);
        }
    }

    public void EndContinuousAttack()
    {
        if (!_isInContinuousAttack) return;
        _isInContinuousAttack = false;
        if (_animator != null && _isAttacking)
        {
            _isAttacking = false;
            _animator.SetBool("isAttacking", false);
        }

        Vector3 attackDir = PlayerAiming.GetAttackDirection();
        if (aimAssistActive)
            attackDir = PlayerAiming.GetAimAssistedDirection(attackDir, transform.position, aimAssistRadius, aimAssistAngle, aimAssistSmooth);

        if (_currentGun != null && _currentWeaponData != null)
            _currentGun.FireWithWeaponData(_currentWeaponData, attackDir);
    }

    public void StopSingleShotAnimation()
    {
        if (_isSingleShotPending && _animator != null)
        {
            _isSingleShotPending = false;
            _animator.SetBool("isAttacking", false);
        }
    }

    public void PlayAttackAnimation()
    {
        if (_animator != null)
        {
            _isAttacking = true;
            _attackAnimEndTime = Time.time + attackAnimDuration;
            _animator.SetBool("isAttacking", true);
            if (_gunModel != null) _gunModel.SetActive(true);
        }
    }

    public void EndSingleShot()
    {
        _isSingleShotPending = false;
        if (_animator != null)
        {
            _animator.SetBool("isAttacking", false);
            _isAttacking = false;
        }
    }

    public void RegisterReload(string weaponName, float reloadTime)
    {
        weaponReloadEndTimes[weaponName] = Time.time + reloadTime;
    }

    private void UpdateBackgroundReloads()
    {
        float now = Time.time;
        List<string> completed = new List<string>();
        foreach (var kv in weaponReloadEndTimes)
        {
            if (now >= kv.Value)
            {
                string weaponName = kv.Key;
                if (_weaponGuns.TryGetValue(weaponName, out Gun gun))
                {
                    int needed = gun.maxAmmo - gun.currentAmmo;
                    int available = gun.totalAmmo - gun.currentAmmo;
                    int toTake = Mathf.Min(needed, available);
                    gun.currentAmmo += toTake;

                    if (weaponAmmoStates.ContainsKey(weaponName))
                        weaponAmmoStates[weaponName] = (gun.currentAmmo, gun.totalAmmo);
                    else
                        weaponAmmoStates.Add(weaponName, (gun.currentAmmo, gun.totalAmmo));

                    if (weaponNameToIndex.TryGetValue(weaponName, out int idx))
                        PlayerUI.Instance?.SetFireButtonInteractable(idx, true);

                    if (_currentWeaponData != null && _currentWeaponData.weaponName == weaponName)
                    {
                        UpdateAmmoUI();
                        OnReloadEnd();
                    }
                }
                completed.Add(weaponName);
            }
        }
        foreach (string name in completed)
            weaponReloadEndTimes.Remove(name);
    }

    public void OnFireButtonDown(RectTransform btnRect, int weaponIndex)
    {
        _isFireButtonTouching = true;
        _mobileDownTime = Time.time;
        _isAiming = true;
        _aimDirection = transform.forward;
        _isLongPressConfirmed = false;
        _pendingWeaponIndex = weaponIndex;

        // 记录触摸手指ID（移动端）
        if (Input.touchCount > 0)
        {
            // 找到正在触碰按钮的那个手指（通常是最新触摸的）
            // 简单起见，使用第一个触摸，因为按钮在右侧，通常手指按下时只有一个触摸
            _fireTouchFingerId = Input.GetTouch(0).fingerId;
        }
        else
        {
            _fireTouchFingerId = -1; // 编辑器模拟用
        }
        _isTrackingFireTouch = true;
        _lastDragScreenPos = Input.mousePosition;

        if (btnRect != null)
        {
            Vector3[] corners = new Vector3[4];
            btnRect.GetWorldCorners(corners);
            _currentButtonCenterScreenPos = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) / 2);
        }

        if (_longPressCoroutine != null) StopCoroutine(_longPressCoroutine);
        _longPressCoroutine = StartCoroutine(LongPressCheck());
    }

    public void OnFireButtonUp()
    {
        _isFireButtonTouching = false;
        _isTrackingFireTouch = false;
        _fireTouchFingerId = -1;
        if (_longPressCoroutine != null)
        {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
        }

        float pressDuration = Time.time - _mobileDownTime;
        bool isSniper = _currentWeaponData != null && _currentWeaponData.type == WeaponType.Sniper;

        if (_isLongPressConfirmed)
        {
            if (isSniper) isScoping = false;
            StartSingleAttack();
        }
        else if (pressDuration < 0.2f)
        {
            if (_pendingWeaponIndex != -1 && _currentWeaponIndex != _pendingWeaponIndex)
                SwitchToWeapon(_pendingWeaponIndex);
            StartSingleAttack();
        }

        _isAiming = false;
        _isLongPressConfirmed = false;
        _pendingWeaponIndex = -1;
    }

    public void OnFireButtonDrag()
    {
        if (!_isAiming) return;

        Vector2 currentPos = Vector2.zero; // 添加默认初始化
        if (_fireTouchFingerId != -1 && Input.touchCount > 0)
        {
            bool found = false;
            foreach (Touch t in Input.touches)
            {
                if (t.fingerId == _fireTouchFingerId)
                {
                    currentPos = t.position;
                    found = true;
                    break;
                }
            }
            if (!found) return;
        }
        else
        {
            currentPos = Input.mousePosition;
        }

        Vector2 offset = currentPos - _currentButtonCenterScreenPos;
        if (offset.magnitude < 10f) return;

        Vector3 worldDir = (cameraTransform.right * offset.x + cameraTransform.up * offset.y).normalized;
        if (worldDir.magnitude > 0.1f)
            _aimDirection = worldDir;
    }

    private IEnumerator LongPressCheck()
    {
        yield return new WaitForSeconds(0.2f);
        if (_isAiming)
        {
            if (_pendingWeaponIndex != -1 && _currentWeaponIndex != _pendingWeaponIndex)
                SwitchToWeapon(_pendingWeaponIndex);

            _isLongPressConfirmed = true;
            // 拖拽瞄准标记在协程中不需要额外设置，Update 会处理
            // 但重新记录一下当前触摸位置
            if (_fireTouchFingerId != -1 && Input.touchCount > 0)
            {
                foreach (Touch t in Input.touches)
                {
                    if (t.fingerId == _fireTouchFingerId)
                    {
                        _lastDragScreenPos = t.position;
                        break;
                    }
                }
            }
            else
            {
                _lastDragScreenPos = Input.mousePosition;
            }

            if (_gunModel != null && !_gunModel.activeSelf)
                _gunModel.SetActive(true);

            if (_currentWeaponData != null && _currentWeaponData.type == WeaponType.Sniper)
                isScoping = true;
        }
    }

    public void Die()
    {
        //Debug.Log("Die() called");
        if (_controller != null) _controller.enabled = false;
        if (_animator != null) _animator.enabled = false;
        if (_currentGun != null) _currentGun.enabled = false;
        if (_gunModel != null) _gunModel.SetActive(false);
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        //Debug.Log("RespawnCoroutine started, waiting 1 second...");
        yield return new WaitForSeconds(1f);
        //Debug.Log("RespawnCoroutine: after delay");

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (PhotonNetwork.InRoom)
        {
            GameNetworkManager networkManager = FindObjectOfType<GameNetworkManager>();
            if (networkManager != null && networkManager.spawnPoints != null && networkManager.spawnPoints.Length > 0)
            {
                int index = PhotonNetwork.LocalPlayer.ActorNumber % networkManager.spawnPoints.Length;
                spawnPos = networkManager.spawnPoints[index].position;
                spawnRot = networkManager.spawnPoints[index].rotation;
                //Debug.Log($"使用网络出生点 {index}：{spawnPos}");
            }
            else
            {
                //Debug.LogError("未找到 GameNetworkManager 或出生点数组为空！使用 respawnPoint 或原地复活");
                if (respawnPoint != null)
                {
                    spawnPos = respawnPoint.position;
                    spawnRot = respawnPoint.rotation;
                }
                else
                {
                    spawnPos = transform.position;
                    spawnRot = transform.rotation;
                }
            }
        }
        else
        {
            // 单人模式：使用 respawnPoint
            if (respawnPoint != null)
            {
                spawnPos = respawnPoint.position;
                spawnRot = respawnPoint.rotation;
            }
            else
            {
                spawnPos = transform.position;
                spawnRot = transform.rotation;
            }
        }

        // 联机模式通过 RPC 同步复活
        if (PhotonNetwork.InRoom)
        {
            _photonView.RPC("RPC_Respawn", RpcTarget.All, spawnPos, spawnRot);
        }
        else
        {
            // 单人模式直接设置
            transform.position = spawnPos;
            transform.rotation = spawnRot;
            Health health = GetComponent<Health>();
            if (health != null) health.ResetHealth();
            if (_controller != null) _controller.enabled = true;
            if (_animator != null) _animator.enabled = true;
            if (_currentGun != null) _currentGun.enabled = true;
            _velocity = Vector3.zero;
        }
    }

    [PunRPC]
    void RPC_Respawn(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        // 重置速度
        _velocity = Vector3.zero;
        // 重置速度为基础速度
        speed = 1.2f;   // 恢复基础速度
        // 清除能量饮料状态
        if (_currentHealType == HealType.EnergyDrink)
            _currentHealType = HealType.None;
        // 清除输入缓存
        _moveInput = Vector2.zero;
        _jumpPressed = false;
        // 重置攻击状态（可选）
        if (_isAttacking) EndSingleShot();
        if (_isInContinuousAttack) EndContinuousAttack();
        // 重新启用组件
        if (_controller != null) _controller.enabled = true;
        if (_animator != null) _animator.enabled = true;
        if (_currentGun != null) _currentGun.enabled = true;
        // 恢复生命值
        Health health = GetComponent<Health>();
        if (health != null) health.ResetHealth();
        //Debug.Log($"RPC_Respawn: 玩家重生在 {position}");
    }

    [PunRPC]
    void RPC_FireSingleBullet(float damage, int shooterViewID, Vector3 direction, Vector3 spawnPos, string bulletPrefabName)
    {
        // 仅其他客户端执行，本地已生成
        GameObject shooter = PhotonView.Find(shooterViewID)?.gameObject;
        if (shooter == null) return;

        // 从 Resources 加载预制体（假设预制体放在 Resources/Bullets/ 下）
        GameObject bulletPrefab = Resources.Load<GameObject>("Bullets/" + bulletPrefabName);
        if (bulletPrefab == null)
        {
            Debug.LogError("Failed to load bullet prefab: " + bulletPrefabName);
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(direction));
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.Initialize(damage, shooter, direction);
    }

    [PunRPC]
    void RPC_FireRocket(int shooterViewID, Vector3 direction, Vector3 spawnPos, string rocketPrefabName)
    {
        GameObject shooter = PhotonView.Find(shooterViewID)?.gameObject;
        if (shooter == null) return;

        GameObject rocketPrefab = Resources.Load<GameObject>("Bullets/" + rocketPrefabName);
        if (rocketPrefab == null)
        {
            Debug.LogError("Failed to load rocket prefab: " + rocketPrefabName);
            return;
        }

        GameObject rocket = Instantiate(rocketPrefab, spawnPos, Quaternion.LookRotation(direction));
        Rocket rocketScript = rocket.GetComponent<Rocket>();
        if (rocketScript != null)
            rocketScript.Initialize(shooter, direction);
    }


    [PunRPC]
    void RPC_PlayShootSound(int weaponIndex)
    {
        // 边界检查
        if (weaponIndex < 0 || weaponIndex >= weapons.Count) return;

        WeaponData data = weapons[weaponIndex];
        if (data == null || data.shootSound == null) return;

        // 在远程客户端上，使用该玩家的位置播放音效（3D空间音效）
        // 注意：transform 是当前 PlayerMain 所在游戏对象的位置（即射击者位置）
        AudioSource.PlayClipAtPoint(data.shootSound, transform.position, 1f);
    }

    public void OnReloadStart()
    {
        if (_currentGun != null)
            PlayerUI.Instance?.SetFireButtonInteractable(_currentWeaponIndex, false);
    }

    public void OnReloadEnd()
    {
        if (_currentGun != null)
        {
            PlayerUI.Instance?.SetFireButtonInteractable(_currentWeaponIndex, true);
            if (!_isAttacking && !_isSingleShotPending && _gunModel != null)
                _gunModel.SetActive(false);
        }
    }

    private Coroutine _hideModelCoroutine;

    private IEnumerator HideWeaponModelDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_gunModel != null && !_isAttacking && !_isSingleShotPending && !_isInContinuousAttack)
            _gunModel.SetActive(false);
        _hideModelCoroutine = null;
    }
}
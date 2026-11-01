using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShipController : MonoBehaviour
{
    [Header("Referanslar (Manuel Atama)")]
    public ShipUIManager uiManager;

    [Header("Gemi Fiziği ve Drift Ayarları")]
    public float acceleration = 25f; // Motorun itme gücü
    public float maxSpeed = 8f; // Maksimum hız sınırı
    public float turnSpeed = 5f; // Dönüş yumuşaklığı (Düşürdükçe gemi daha hantal döner)
    [Range(0f, 1f)] public float driftFactor = 0.95f; // 1 = Rayda gider gibi, 0.90 = Kaygan drift

    [Header("Enerji / Yakıt Ayarları")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;
    public float energyDrainRate = 8f; // Hareket halindeyken saniyede eksilen enerji miktarı
    public float energyRegenRate = 0f; // EnergyRegen upgrade kartlarıyla artan pasif yenilenme

    [Header("Dash (Atılım) Ayarları")]
    public float dashSpeed = 24f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public int maxDashCharges = 3;
    public float currentDash = 3f;
    public float dashRegenRate = 0.5f;
    [SerializeField, Min(1)] private int dashCapacity = 3;

    [Header("Kamera Zoom (FOV) Ayarları")]
    public CinemachineCamera vcam;
    public float idleZoom = 5f;
    public float moveZoom = 7f;
    public float dashZoom = 9f;
    public float zoomSpeed = 4f;

    public bool IsDashing { get; private set; }
    public bool HasEnergy => currentEnergy > 0f;

    public int AvailableDashCharges
    {
        get
        {
            if (uiManager != null)
            {
                return Mathf.FloorToInt(uiManager.currentDash);
            }
            return Mathf.FloorToInt(currentDash);
        }
    }

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private float dashTimeCounter;
    private float[] dashRemaining = new float[3];
    private Vector2 dashDirection;

    private InputSystem_Actions controls;
    private PlayerHealth playerHealth;

    private int playerLayer;
    private int enemyLayer;
    private bool isGhostModeActive = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        // Suyun sürtünme hissi için Linear Drag değerini 1.5 civarı yapabilirsin
        rb.linearDamping = 1.5f;

        currentEnergy = maxEnergy;
        currentDash = maxDashCharges;
        EnsureDashSlots();

        playerHealth = GetComponent<PlayerHealth>();
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");

        controls = new InputSystem_Actions();
    }

    void Start()
    {
        SyncEnergyUI();
    }

    void OnEnable()
    {
        if (controls == null) controls = new InputSystem_Actions();
        controls.Player.Sprint.performed -= OnSprintPerformed;
        controls.Player.Sprint.performed += OnSprintPerformed;
        controls.Enable();
    }

    void OnDisable()
    {
        if (controls != null)
        {
            controls.Player.Sprint.performed -= OnSprintPerformed;
            controls.Disable();
        }

        IsDashing = false;
        dashTimeCounter = 0f;

        if (isGhostModeActive)
        {
            isGhostModeActive = false;
            if (playerLayer >= 0 && enemyLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
            }
        }
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx) => Dash();

    void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        // 1. Sadece oyuncu hareket tuşlarına basıyorsa enerjiyi azalt
        if (moveInput.sqrMagnitude > 0.01f && currentEnergy > 0f)
        {
            currentEnergy = Mathf.Max(0f, currentEnergy - energyDrainRate * Time.deltaTime);
            if (currentEnergy <= 0f)
            {
                IsDashing = false;
            }
            SyncEnergyUI();
        }
        // Pasif enerji yenilenmesi upgrade'i alınmışsa uygula
        else if (energyRegenRate > 0f && currentEnergy < maxEnergy)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenRate * Time.deltaTime);
            SyncEnergyUI();
        }

        // Dash dolumu: uiManager atanmışsa onun currentDash değerini senkronize tut, atanmamışsa kendi içinde doldur
        if (uiManager != null)
        {
            currentDash = uiManager.currentDash;
            maxDashCharges = uiManager.maxDashCharges;
        }
        else if (currentDash < maxDashCharges)
        {
            currentDash = Mathf.Min(maxDashCharges, currentDash + dashRegenRate * Time.deltaTime);
        }

        EnsureDashSlots();
        if (Time.deltaTime > 0f)
        {
            for (int i = 0; i < dashRemaining.Length; i++)
            {
                if (dashRemaining[i] > 0f)
                {
                    dashRemaining[i] = Mathf.Max(0f, dashRemaining[i] - Time.deltaTime);
                }
            }
        }

        if (IsDashing)
        {
            dashTimeCounter -= Time.deltaTime;
            if (dashTimeCounter <= 0f) IsDashing = false;
        }

        bool shouldBeGhost = IsDashing || (playerHealth != null && playerHealth.IsInvulnerable);

        if (shouldBeGhost != isGhostModeActive)
        {
            isGhostModeActive = shouldBeGhost;
            if (playerLayer >= 0 && enemyLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, isGhostModeActive);
            }
        }

        if (vcam != null)
        {
            float targetZoom = idleZoom;
            if (IsDashing && HasEnergy) targetZoom = dashZoom;
            else if (moveInput.magnitude > 0 && HasEnergy) targetZoom = moveZoom;

            vcam.Lens.OrthographicSize = Mathf.Lerp(vcam.Lens.OrthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // Enerji bittiyse gemi KESİNLİKLE HAREKET EDEMESİN
        if (currentEnergy <= 0f)
        {
            ApplyDrift();
            return;
        }

        if (IsDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
        }
        else
        {
            ApplyEngineForce();
            ApplyDrift();
        }
    }

    private void ApplyEngineForce()
    {
        // Enerji bittiyse gemi KESİNLİKLE HAREKET EDEMESİN (Rigidbody'ye güç uygulanmasın)
        if (currentEnergy <= 0f) return;

        if (moveInput.magnitude > 0)
        {
            // 1. YUMUŞAK DÖNÜŞ
            float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

            // 2. YENİ FİZİK DOSTU İVME MATEMATİĞİ
            float forwardSpeed = Vector2.Dot(rb.linearVelocity, transform.up);

            if (forwardSpeed < maxSpeed)
            {
                rb.AddForce(transform.up * acceleration);
            }
        }
    }

    private void ApplyDrift()
    {
        // Geminin ileri doğru olan hız vektörü
        Vector2 forwardVelocity = transform.up * Vector2.Dot(rb.linearVelocity, transform.up);
        // Geminin yanlara doğru olan (kayma) hız vektörü
        Vector2 rightVelocity = transform.right * Vector2.Dot(rb.linearVelocity, transform.right);

        // Yanlara kayma hızını belli bir oranda törpüleyerek suya tutunma hissi yarat
        rb.linearVelocity = forwardVelocity + (rightVelocity * driftFactor);
    }

    private void Dash()
    {
        TryDash(moveInput);
    }

    public bool TryDash(Vector2 direction)
    {
        if (!isActiveAndEnabled || IsDashing || currentEnergy <= 0f || direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        // Tüketim mantığı: currentDash >= 1f kontrolü ve tam 1 birim düşürme
        if (uiManager != null)
        {
            if (uiManager.currentDash < 1f) return false;
            uiManager.ConsumeDash();
            currentDash = uiManager.currentDash;
        }
        else
        {
            if (currentDash < 1f) return false;
            currentDash -= 1f;
        }

        EnsureDashSlots();
        for (int i = 0; i < dashRemaining.Length; i++)
        {
            if (dashRemaining[i] <= 0f)
            {
                dashRemaining[i] = Mathf.Max(0.01f, dashCooldown);
                break;
            }
        }

        IsDashing = true;
        dashTimeCounter = dashDuration;
        dashDirection = direction.normalized;
        return true;
    }

    public void SetDashCapacity(int capacity)
    {
        dashCapacity = Mathf.Max(1, capacity);
        maxDashCharges = dashCapacity;
        currentDash = Mathf.Min(currentDash, maxDashCharges);
        dashRemaining = new float[dashCapacity];
    }

    /// <summary>
    /// Mevcut dash dolum/süre oranını (0 ile 1 arası float) döndürür.
    /// </summary>
    public float GetDashProgress()
    {
        return GetDashProgress(0);
    }

    /// <summary>
    /// Belirtilen dash slotunun dolum oranını (0 ile 1 arası float) döndürür.
    /// </summary>
    public float GetDashProgress(int slotIndex)
    {
        float activeDash = uiManager != null ? uiManager.currentDash : currentDash;
        return Mathf.Clamp01(activeDash - slotIndex);
    }

    public float GetDashRemaining(int slotIndex = 0)
    {
        EnsureDashSlots();
        if (slotIndex < 0 || slotIndex >= dashRemaining.Length) return 0f;
        return dashRemaining[slotIndex];
    }

    private void EnsureDashSlots()
    {
        int count = Mathf.Max(1, dashCapacity);
        if (dashRemaining == null || dashRemaining.Length != count)
        {
            dashRemaining = new float[count];
        }
    }

    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        SyncEnergyUI();
    }

    public void IncreaseMaxEnergy(float amount)
    {
        maxEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        SyncEnergyUI();
    }

    public void SyncEnergyUI()
    {
        if (uiManager != null && maxEnergy > 0f)
        {
            uiManager.UpdateEnergyBar((currentEnergy / maxEnergy) * 100f);
        }
    }
}

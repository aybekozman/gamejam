using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShipController : MonoBehaviour
{
    [Header("Gemi Fiziği ve Drift Ayarları")]
    public float acceleration = 25f; // Motorun itme gücü
    public float maxSpeed = 8f; // Maksimum hız sınırı
    public float turnSpeed = 5f; // Dönüş yumuşaklığı (Düşürdükçe gemi daha hantal döner)
    [Range(0f, 1f)] public float driftFactor = 0.95f; // 1 = Rayda gider gibi, 0.90 = Kaygan drift

    [Header("Dash (Atılım) Ayarları")]
    public float dashSpeed = 24f;
    public float dashDuration = 0.2f;
    [Min(0.01f)] public float dashCooldown = 5f;
    [SerializeField, Min(1)] private int maxDashCharges = 3;

    [Header("Kamera Zoom (FOV) Ayarları")]
    public CinemachineCamera vcam;
    public float idleZoom = 5f;
    public float moveZoom = 7f;
    public float dashZoom = 9f;
    public float zoomSpeed = 4f;

    public bool IsDashing { get; private set; }

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private float dashTimeCounter;
    private float[] dashRemaining;
    private float[] dashDurations;
    private Vector2 dashDirection;

    private InputSystem_Actions controls;
    private PlayerHealth playerHealth;

    private int playerLayer;
    private int enemyLayer;
    private bool isGhostModeActive = false;
    private bool originalIgnoreCollision;
    public int MaxDashCharges => dashRemaining == null ? Mathf.Max(1, maxDashCharges) : dashRemaining.Length;
    public int AvailableDashCharges
    {
        get { int count = 0; if (dashRemaining != null) foreach (float remaining in dashRemaining) if (remaining <= 0) count++; return count; }
    }
    public float GetDashRemaining(int slot) => dashRemaining != null && slot >= 0 && slot < dashRemaining.Length ? dashRemaining[slot] : 0;
    public float GetDashProgress(int slot) => dashRemaining != null && slot >= 0 && slot < dashRemaining.Length
        ? (dashRemaining[slot] <= 0 ? 1 : Mathf.Clamp01(1 - dashRemaining[slot] / dashDurations[slot])) : 0;
    public void SetDashCapacity(int capacity)
    {
        maxDashCharges = Mathf.Clamp(capacity, 1, 16);
        System.Array.Resize(ref dashRemaining, maxDashCharges);
        System.Array.Resize(ref dashDurations, maxDashCharges);
    }
    public void ResetDashCharges()
    {
        SetDashCapacity(maxDashCharges);
        System.Array.Clear(dashRemaining, 0, dashRemaining.Length);
        IsDashing = false;
        dashTimeCounter = 0;
        SetGhostMode(false);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        // Suyun sürtünme hissi için Linear Drag değerini 1.5 civarı yapabilirsin
        rb.linearDamping = 1.5f;

        playerHealth = GetComponent<PlayerHealth>();
        playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0) playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) originalIgnoreCollision = Physics2D.GetIgnoreLayerCollision(playerLayer, enemyLayer);

        controls = new InputSystem_Actions();
        SetDashCapacity(maxDashCharges);
    }

    void OnEnable()
    {
        controls.Player.Sprint.performed += OnDashPerformed;
        controls.Enable();
    }
    void OnDisable()
    {
        controls.Player.Sprint.performed -= OnDashPerformed;
        controls.Disable();
        IsDashing = false;
        dashTimeCounter = 0;
        SetGhostMode(false);
    }
    void OnDestroy() { controls?.Dispose(); }
    private void OnDashPerformed(InputAction.CallbackContext context) => TryDash(controls.Player.Move.ReadValue<Vector2>());
    private void SetGhostMode(bool enabled)
    {
        if (enabled == isGhostModeActive) return;
        isGhostModeActive = enabled;
        if (enemyLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, enabled || originalIgnoreCollision);
    }

    void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        for (int i = 0; i < dashRemaining.Length; i++) dashRemaining[i] = Mathf.Max(0, dashRemaining[i] - Time.deltaTime);

        if (IsDashing)
        {
            dashTimeCounter -= Time.deltaTime;
            if (dashTimeCounter <= 0) IsDashing = false;
        }

        bool shouldBeGhost = IsDashing || (playerHealth != null && playerHealth.IsInvulnerable);

        SetGhostMode(shouldBeGhost);

        if (vcam != null)
        {
            float targetZoom = idleZoom;
            if (IsDashing) targetZoom = dashZoom;
            else if (moveInput.magnitude > 0) targetZoom = moveZoom;

            vcam.Lens.OrthographicSize = Mathf.Lerp(vcam.Lens.OrthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
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
        if (moveInput.magnitude > 0)
        {
            // 1. YUMUŞAK DÖNÜŞ
            float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

            // 2. YENİ FİZİK DOSTU İVME MATEMATİĞİ
            // Geminin anlık olarak "kendi baktığı yöndeki" hızını ölçeriz.
            float forwardSpeed = Vector2.Dot(rb.linearVelocity, transform.up);

            // Sadece ileri yöndeki hızımız maxSpeed'den düşükse motor gücü uygularız.
            // Bu sayede dışarıdan gelen (Geri tepme) kuvvetler hızı anlık olarak sınırsızca artırabilir 
            // ve suyun sürtünmesi (Rigidbody Linear Damping) ile doğal, kaygan bir şekilde yavaşlar.
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

    public bool TryDash(Vector2 direction)
    {
        if (!isActiveAndEnabled || IsDashing || Time.timeScale <= 0 || direction.sqrMagnitude < .0001f ||
            (playerHealth && playerHealth.IsDead)) return false;
        for (int slot = 0; slot < dashRemaining.Length; slot++)
        {
            if (dashRemaining[slot] > 0) continue;
            IsDashing = true;
            dashTimeCounter = Mathf.Max(.01f, dashDuration);
            dashDurations[slot] = Mathf.Max(.01f, dashCooldown);
            dashRemaining[slot] = dashDurations[slot];
            dashDirection = direction.normalized;
            SetGhostMode(true);
            return true;
        }
        return false;
    }
}

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
    public float dashCooldown = 1f;

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
    private float dashCooldownCounter;
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

        playerHealth = GetComponent<PlayerHealth>();
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");

        controls = new InputSystem_Actions();
        controls.Player.Sprint.performed += ctx => Dash();
    }

    void OnEnable() => controls.Enable();
    void OnDisable()
    {
        controls.Disable();
        controls.Player.Sprint.performed -= ctx => Dash();
    }

    void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        if (dashCooldownCounter > 0) dashCooldownCounter -= Time.deltaTime;

        if (IsDashing)
        {
            dashTimeCounter -= Time.deltaTime;
            if (dashTimeCounter <= 0) IsDashing = false;
        }

        bool shouldBeGhost = IsDashing || (playerHealth != null && playerHealth.IsInvulnerable);

        if (shouldBeGhost != isGhostModeActive)
        {
            isGhostModeActive = shouldBeGhost;
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, isGhostModeActive);
        }

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

    private void Dash()
    {
        if (dashCooldownCounter <= 0 && moveInput.magnitude > 0)
        {
            IsDashing = true;
            dashTimeCounter = dashDuration;
            dashCooldownCounter = dashCooldown;
            dashDirection = moveInput;
        }
    }
}
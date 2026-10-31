using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; // Unity 6 (Cinemachine 3) için. Hata verirse 'using Cinemachine;' olarak değiştirin.

[RequireComponent(typeof(Rigidbody2D))]
public class ShipController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float maxSpeed = 8f;
    public float acceleration = 15f;
    public float deceleration = 10f;

    [Header("Dash (Atılım) Ayarları")]
    public float dashSpeed = 24f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Kamera Zoom (FOV) Ayarları")]
    [Tooltip("Hierarchy'deki Cinemachine kameranızı buraya sürükleyin")]
    public CinemachineCamera vcam; // Sürümden dolayı hata verirse 'CinemachineVirtualCamera' yapın
    public float idleZoom = 5f;    // Dururken (yakın)
    public float moveZoom = 7f;    // Hareket ederken (orta)
    public float dashZoom = 9f;    // Dash atarken (uzak)
    public float zoomSpeed = 4f;   // Zoom geçiş hızı (yumuşaklık)

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocity;

    private bool isDashing;
    private float dashTimeCounter;
    private float dashCooldownCounter;
    private Vector2 dashDirection;

    private InputSystem_Actions controls;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        controls = new InputSystem_Actions();
        controls.Player.Sprint.performed += ctx => Dash();
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
        controls.Player.Sprint.performed -= ctx => Dash();
    }

    void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        if (dashCooldownCounter > 0)
        {
            dashCooldownCounter -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimeCounter -= Time.deltaTime;
            if (dashTimeCounter <= 0)
            {
                isDashing = false;
            }
        }

        // Kamera Dinamik Zoom Mantığı
        if (vcam != null)
        {
            float targetZoom = idleZoom;

            if (isDashing)
            {
                targetZoom = dashZoom;
            }
            else if (moveInput.magnitude > 0)
            {
                targetZoom = moveZoom;
            }

            vcam.Lens.OrthographicSize = Mathf.Lerp(vcam.Lens.OrthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            currentVelocity = dashDirection * dashSpeed;
        }
        else
        {
            if (moveInput.magnitude > 0)
            {
                currentVelocity = Vector2.MoveTowards(currentVelocity, moveInput * maxSpeed, acceleration * Time.fixedDeltaTime);

                float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, targetAngle);
            }
            else
            {
                currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            }
        }

        rb.linearVelocity = currentVelocity;
    }

    private void Dash()
    {
        if (dashCooldownCounter <= 0 && moveInput.magnitude > 0)
        {
            isDashing = true;
            dashTimeCounter = dashDuration;
            dashCooldownCounter = dashCooldown;
            dashDirection = moveInput;
        }
    }
}
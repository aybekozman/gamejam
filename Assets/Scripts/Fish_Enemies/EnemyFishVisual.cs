
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyFishVisual : MonoBehaviour
{
    [Header("Animasyon Kareleri")]
    public Sprite[] swimFrames;
    public Sprite[] attackFrames;

    [Header("Animasyon Hizi")]
    public float swimFps = 8f;
    public float attackFps = 12f;

    [Header("Saldiri Ayarlari")]
    public float attackStartDistance = 1.5f;
    public float rearmDistance = 1f;

    [Tooltip("Agzin tamamen acik oldugu kare (0'dan baslar)")]
    public int fullyOpenFrame = 4;

    private SpriteRenderer sr;
    private Collider2D fishCollider;
    private Collider2D shipCollider;
    private Transform ship;

    private float timer;
    private bool readyToAttack = true;

    private enum AnimState
    {
        Swimming,
        Opening,
        HoldingOpen,
        Closing
    }

    private AnimState state = AnimState.Swimming;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        fishCollider = GetComponent<Collider2D>();

        if (fishCollider == null)
            fishCollider = GetComponentInChildren<Collider2D>();
    }

    void Start()
    {
        ShipController target =
            FindAnyObjectByType<ShipController>();

        if (target != null)
        {
            ship = target.transform;

            shipCollider = target.GetComponent<Collider2D>();

            if (shipCollider == null)
                shipCollider =
                    target.GetComponentInChildren<Collider2D>();
        }
    }

    void Update()
    {
        CheckDistance();
        PlayAnimation();
    }

    void CheckDistance()
    {
        if (ship == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            ship.position
        );

        if (fishCollider != null &&
            shipCollider != null &&
            fishCollider.enabled &&
            shipCollider.enabled)
        {
            distance =
                Mathf.Max(0f,
                    fishCollider.Distance(shipCollider).distance);
        }

        if (distance >
            attackStartDistance + rearmDistance)
        {
            readyToAttack = true;

            // Gemi uzaklasirsa saldiridan vazgec
            if (state == AnimState.Opening ||
                state == AnimState.HoldingOpen)
            {
                state = AnimState.Swimming;
                timer = 0f;
            }
        }

        if (state == AnimState.Swimming &&
            readyToAttack &&
            distance <= attackStartDistance &&
            attackFrames != null &&
            attackFrames.Length > 0)
        {
            state = AnimState.Opening;
            readyToAttack = false;
            timer = 0f;
        }
    }

    void PlayAnimation()
    {
        float fps = Mathf.Max(
            1f,
            state == AnimState.Swimming ? swimFps : attackFps
        );

        if (state == AnimState.Swimming)
        {
            if (swimFrames == null || swimFrames.Length == 0)
                return;

            timer += Time.deltaTime;

            int index = Mathf.FloorToInt(timer * fps)
                % swimFrames.Length;

            ShowSprite(swimFrames[index]);
            return;
        }

        if (attackFrames == null || attackFrames.Length == 0)
            return;

        int openIndex = Mathf.Clamp(
            fullyOpenFrame,
            0,
            attackFrames.Length - 1
        );

        if (state == AnimState.Opening)
        {
            timer += Time.deltaTime;

            int frame = Mathf.Min(
                Mathf.FloorToInt(timer * fps),
                openIndex
            );

            ShowSprite(attackFrames[frame]);

            if (frame >= openIndex)
                state = AnimState.HoldingOpen;
        }
        else if (state == AnimState.HoldingOpen)
        {
            // Gemiye carpana kadar agiz acik kalsin
            ShowSprite(attackFrames[openIndex]);
        }
        else if (state == AnimState.Closing)
        {
            timer += Time.deltaTime;

            int frame = openIndex + 1 +
                Mathf.FloorToInt(timer * fps);

            if (frame >= attackFrames.Length)
            {
                state = AnimState.Swimming;
                timer = 0f;
            }
            else
            {
                ShowSprite(attackFrames[frame]);
            }
        }
    }

    void ShowSprite(Sprite sprite)
    {
        if (sprite != null)
            sr.sprite = sprite;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Yalnizca gemiye carpinca agzi kapat
        ShipController player =
            collision.gameObject
                .GetComponentInParent<ShipController>();

        if (player == null)
            return;

        if (state == AnimState.Opening ||
            state == AnimState.HoldingOpen)
        {
            int openIndex = Mathf.Clamp(
                fullyOpenFrame,
                0,
                attackFrames.Length - 1
            );

            ShowSprite(attackFrames[openIndex]);

            state = AnimState.Closing;
            timer = 0f;
        }
    }
}

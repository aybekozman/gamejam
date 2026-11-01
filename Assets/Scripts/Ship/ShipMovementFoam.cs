using UnityEngine;

/// <summary>Visuals only. Reads velocity; pooled wake slots remain in world space.</summary>
[DisallowMultipleComponent]
public sealed class ShipMovementFoam : MonoBehaviour
{
    public ShipController movement;
    public Rigidbody2D body;
    public Transform leftAnchor, rightAnchor, sternAnchor, bowAnchor;
    public Vector2 localForward = Vector2.up;
    public Sprite[] leftFrames, rightFrames, wakeFrames, bowFrames, sternArcFrames;
    public Vector2 sternArcScale = new Vector2(1.05f, 1.15f);
    public Vector2 bowScale = new Vector2(.48f, .24f);
    public Material foamMaterial;
    [Range(1, 20)] public float fps = 7;
    public float startSpeed = .65f, stopSpeed = .3f;
    public AnimationCurve speedResponse = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Range(0, 1)] public float opacity = .62f;
    public Vector2 sideScale = Vector2.one, wakeScale = Vector2.one;
    [Min(.1f)] public float trailLength = 7, trailLifetime = 1.3f, emissionDistance = .9f;
    [Min(.02f)] public float emissionInterval = .08f;
    [Min(.1f)] public float fadeTime = .6f;
    public float teleportDistance = 5;
    public string sortingLayer = "Default";
    public int sortingOrder = -20;
    [Range(8, 128)] public int poolSize = 40;
    public float Intensity { get; private set; }
    public int ActiveTrailCount { get; private set; }
    SpriteRenderer left, right, connector, bow, sternArc;
    Transform trailRoot;
    Slot[] pool;
    int cursor;
    bool moving;
    Vector3 previousPosition;
    float distance, sinceEmission, clock;
    sealed class Slot
    {
        public SpriteRenderer renderer;
        public float age, lifetime, strength, phase;
        public Vector3 scale;
    }
    void Awake()
    {
        if (!movement) movement = GetComponent<ShipController>();
        if (!body) body = GetComponent<Rigidbody2D>();
        left = Make("Left movement foam", leftAnchor ? leftAnchor : transform);
        right = Make("Right movement foam", rightAnchor ? rightAnchor : transform);
        connector = Make("Stern connection foam", sternAnchor ? sternAnchor : transform);
        bow = Make("Bow water cutting foam", bowAnchor ? bowAnchor : transform);
        // The V apex points forward; its arms extend backwards along the hull.
        bow.transform.localRotation = Quaternion.identity;
        sternArc = Make("Stern spreading wave arcs", sternAnchor ? sternAnchor : transform);
        sternArc.sortingOrder = sortingOrder + 1;
        trailRoot = new GameObject("Ship foam trail (pooled)").transform;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(trailRoot.gameObject, gameObject.scene);
        pool = new Slot[Mathf.Clamp(poolSize, 8, 128)];
        for (int i = 0; i < pool.Length; i++) pool[i] = new Slot { renderer = Make("Wake " + i, trailRoot) };
        ResetTrail();
    }
    SpriteRenderer Make(string label, Transform parent)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = foamMaterial;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = false;
        return renderer;
    }
    public void ResetTrail()
    {
        previousPosition = transform.position;
        distance = sinceEmission = clock = Intensity = 0;
        moving = false;
        ActiveTrailCount = 0;
        // Scene unload can destroy the detached world-space root before the ship.
        if (pool != null) foreach (var slot in pool) { slot.lifetime = 0; if (slot.renderer) slot.renderer.enabled = false; }
        if (left) left.enabled = false;
        if (right) right.enabled = false;
        if (connector) connector.enabled = false;
        if (bow) bow.enabled = false;
        if (sternArc) sternArc.enabled = false;
    }
    void OnEnable() { ResetTrail(); }
    void OnDisable() { ResetTrail(); }
    void OnDestroy() { if (trailRoot) Destroy(trailRoot.gameObject); }
    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0 || !body) return;
        clock += dt;
        Vector2 displacement = transform.position - previousPosition;
        float travelled = displacement.magnitude;
        if (travelled > Mathf.Max(teleportDistance, body.linearVelocity.magnitude * dt * 3 + 1))
        { ResetTrail(); return; }
        previousPosition = transform.position;
        Vector2 forward = transform.TransformDirection(localForward.normalized);
        float speed = Mathf.Max(0, Vector2.Dot(body.linearVelocity, forward));
        moving = speed > (moving ? stopSpeed : startSpeed);
        float normalized = moving ? Mathf.InverseLerp(stopSpeed, movement ? movement.maxSpeed : 15, speed) : 0;
        float target = speedResponse.Evaluate(normalized);
        Intensity = Mathf.MoveTowards(Intensity, target, dt / (target > Intensity ? .3f : Mathf.Max(.1f, fadeTime)));
        Draw(left, leftFrames, clock, Intensity, new Vector3(sideScale.x * Mathf.Lerp(.8f, 1, Intensity), sideScale.y, 1));
        Draw(right, rightFrames, clock + .19f, Intensity, new Vector3(sideScale.x * Mathf.Lerp(.8f, 1, Intensity), sideScale.y, 1));
        Draw(connector, wakeFrames, clock, Intensity * .65f, new Vector3(wakeScale.x * .55f, wakeScale.y * .28f, 1));
        Draw(bow, bowFrames, clock + .1f, Intensity * .85f,
            new Vector3(bowScale.x * Mathf.Lerp(.75f, 1, Intensity), bowScale.y * Mathf.Lerp(.8f, 1, Intensity), 1));
        Draw(sternArc, sternArcFrames, clock * (5f / fps), Intensity * .7f,
            new Vector3(sternArcScale.x, sternArcScale.y, 1));
        sinceEmission += dt;
        if (moving)
        {
            // A fixed physics step may span several render frames: do not clamp its
            // displacement to this render frame's deltaTime or emission becomes FPS dependent.
            distance += Mathf.Max(0, Vector2.Dot(displacement, forward));
            if (distance >= emissionDistance && sinceEmission >= emissionInterval)
            { Emit(Mathf.Clamp01(target)); distance = 0; sinceEmission = 0; }
        }
        else distance = 0;
        ActiveTrailCount = 0;
        foreach (var slot in pool)
        {
            if (slot.lifetime <= 0) continue;
            slot.age += dt;
            float life = 1 - slot.age / slot.lifetime;
            if (life <= 0) { slot.lifetime = 0; slot.renderer.enabled = false; continue; }
            Draw(slot.renderer, wakeFrames, clock + slot.phase, slot.strength * life * life, slot.scale * Mathf.Lerp(1.12f, 1, life));
            ActiveTrailCount++;
        }
    }
    void Emit(float strength)
    {
        if (strength <= .01f || wakeFrames == null || wakeFrames.Length == 0) return;
        Slot slot = pool[cursor];
        cursor = (cursor + 1) % pool.Length;
        slot.renderer.transform.SetPositionAndRotation(sternAnchor ? sternAnchor.position : transform.position, transform.rotation);
        float speed = Mathf.Max(.1f, Vector2.Dot(body.linearVelocity, transform.TransformDirection(localForward.normalized)));
        slot.age = 0;
        slot.lifetime = Mathf.Min(trailLifetime, trailLength / speed);
        slot.strength = strength * .5f;
        slot.phase = cursor / fps;
        slot.scale = new Vector3(wakeScale.x * Mathf.Lerp(.55f, 1, strength), wakeScale.y * Mathf.Lerp(.35f, 1, strength), 1);
    }
    void Draw(SpriteRenderer renderer, Sprite[] frames, float time, float alpha, Vector3 scale)
    {
        bool visible = frames != null && frames.Length > 0 && alpha > .005f;
        renderer.enabled = visible;
        if (!visible) return;
        renderer.sprite = frames[Mathf.FloorToInt(time * fps) % frames.Length];
        renderer.color = new Color(1, 1, 1, Mathf.Clamp01(alpha) * opacity);
        renderer.transform.localScale = scale;
    }
}

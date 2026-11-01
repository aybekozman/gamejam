using UnityEngine;

/// <summary>Small, independent, world-space waves. The pool belongs to this scene.</summary>
[DisallowMultipleComponent]
public sealed class OceanSurfaceWavelets : MonoBehaviour
{
    [System.Serializable] public class WaveType { public Sprite[] frames = new Sprite[4]; }
    public Camera view;
    public SpriteRenderer shipHull;
    public WaveType[] types = new WaveType[3];
    public Material material;
    [Range(1, 16)] public int activeLimit = 8;
    [Range(1, 12)] public float fps = 5;
    [Min(.02f)] public float spawnInterval = .075f;
    [Range(0, 1)] public float opacity = .32f;
    public Vector2 scaleRange = new Vector2(.95f, 1.05f);
    [Min(0)] public float minimumDistance = 6;
    [Min(0)] public float cameraMargin = 2;
    [Min(0)] public float fadeIn = .12f, fadeOut = .25f;
    public string sortingLayer = "Default";
    public int sortingOrder = -60;
    [Tooltip("Solid environment and player layers. Water/Enemy/Loot are excluded by default.")]
    public LayerMask exclusionLayers = (1 << 0) | (1 << 3);
    public Collider2D[] exclusionAreas = new Collider2D[0];
    public bool useWaterBounds;
    public Rect waterBounds = new Rect(-500, -500, 1000, 1000);
    [Tooltip("Optional water volume. All footprint corners must be inside it.")]
    public Collider2D waterArea;
    public float shipClearance = 1;
    public int ActiveCount { get; private set; }
    public int PoolCount => pool == null ? 0 : pool.Length;
    const int Capacity = 16;
    sealed class Slot
    {
        public SpriteRenderer renderer;
        public Sprite[] frames;
        public Vector2 footprint;
        public float age, lifetime;
        public bool active;
    }
    Slot[] pool;
    readonly Collider2D[] overlap = new Collider2D[16];
    ContactFilter2D filter;
    float timer;
    bool seeded;

    void Awake()
    {
        if (!view) view = Camera.main;
        pool = new Slot[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            var go = new GameObject("Surface wave " + i);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false;
            pool[i] = new Slot { renderer = renderer };
        }
    }
    void OnEnable() { seeded = false; timer = 0; }
    void OnDisable()
    {
        if (pool != null) foreach (var slot in pool) { slot.active = false; if (slot.renderer) slot.renderer.enabled = false; }
        ActiveCount = 0;
    }
    Rect CameraArea()
    {
        float height = view.orthographicSize + cameraMargin;
        float width = view.orthographicSize * view.aspect + cameraMargin;
        return new Rect(view.transform.position.x - width, view.transform.position.y - height, width * 2, height * 2);
    }
    public bool CanPlace(Vector2 position, Vector2 footprint)
    {
        Vector2 half = footprint * .5f;
        if (useWaterBounds && (!waterBounds.Contains(position - half) || !waterBounds.Contains(position + half))) return false;
        if (waterArea && (!waterArea.OverlapPoint(position - half) || !waterArea.OverlapPoint(position + half) ||
            !waterArea.OverlapPoint(position + new Vector2(half.x,-half.y)) || !waterArea.OverlapPoint(position + new Vector2(-half.x,half.y)))) return false;
        var bounds = new Bounds(position, new Vector3(footprint.x, footprint.y, 1));
        if (shipHull)
        {
            var shipBounds = shipHull.bounds;
            shipBounds.Expand(new Vector3(shipClearance * 2, shipClearance * 2, 2));
            if (shipBounds.Intersects(bounds)) return false;
        }
        filter.SetLayerMask(exclusionLayers);
        filter.useTriggers = true;
        if (Physics2D.OverlapBox(position, footprint, 0, filter, overlap) > 0) return false;
        foreach (var area in exclusionAreas)
            if (area && area.enabled && area.gameObject.activeInHierarchy && area.bounds.Intersects(bounds)) return false;
        return true;
    }
    bool Spawn(Rect cameraArea, bool warm)
    {
        Slot free = null;
        foreach (var slot in pool) if (!slot.active) { free = slot; break; }
        if (free == null || types == null || types.Length == 0) return false;
        var type = types[Random.Range(0, types.Length)];
        if (type == null || type.frames == null || type.frames.Length == 0 || !type.frames[0]) return false;
        float scale = Random.Range(Mathf.Max(.1f,scaleRange.x), Mathf.Max(scaleRange.x,scaleRange.y));
        Vector2 size = Vector2.zero;
        foreach (var sprite in type.frames) if (sprite) size = Vector2.Max(size, sprite.bounds.size);
        size *= scale;
        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector2 position = new Vector2(Random.Range(cameraArea.xMin, cameraArea.xMax), Random.Range(cameraArea.yMin, cameraArea.yMax));
            if (!CanPlace(position, size)) continue;
            bool tooNear = false;
            foreach (var other in pool)
                if (other.active && ((Vector2)other.renderer.transform.position-position).sqrMagnitude < minimumDistance * minimumDistance) { tooNear = true; break; }
            if (tooNear) continue;
            free.renderer.transform.position = new Vector3(position.x, position.y, transform.position.z);
            free.renderer.transform.rotation = Quaternion.identity;
            free.renderer.transform.localScale = Vector3.one * scale;
            free.frames = type.frames;
            free.footprint = size;
            free.lifetime = type.frames.Length / Mathf.Max(1, fps);
            free.age = warm ? Random.Range(.08f, free.lifetime * .8f) : 0;
            free.active = true;
            return true;
        }
        return false;
    }
    void LateUpdate()
    {
        if (!view || !view.orthographic || pool == null || Time.deltaTime <= 0) return;
        Rect visible = CameraArea();
        int limit = Mathf.Clamp(activeLimit, 1, Capacity);
        if (!seeded)
        {
            for (int i = 0; i < limit; i++) Spawn(visible, true);
            seeded = true;
        }
        ActiveCount = 0;
        foreach (var slot in pool)
        {
            if (!slot.active) continue;
            slot.age += Time.deltaTime;
            if (slot.age >= slot.lifetime || !visible.Contains(slot.renderer.transform.position) ||
                !CanPlace(slot.renderer.transform.position, slot.footprint) || ActiveCount >= limit)
            { slot.active = false; slot.renderer.enabled = false; continue; }
            int frame = Mathf.Min(slot.frames.Length - 1, Mathf.FloorToInt(slot.age * fps));
            float alpha = Mathf.Min(Mathf.Clamp01(slot.age / Mathf.Max(.001f, fadeIn)),
                Mathf.Clamp01((slot.lifetime - slot.age) / Mathf.Max(.001f, fadeOut)));
            slot.renderer.sprite = slot.frames[frame];
            slot.renderer.color = new Color(1, 1, 1, opacity * alpha);
            slot.renderer.enabled = true;
            ActiveCount++;
        }
        timer -= Time.deltaTime;
        if (timer <= 0 && ActiveCount < limit)
        {
            if (Spawn(visible, false)) ActiveCount++;
            timer = spawnInterval;
        }
    }
}

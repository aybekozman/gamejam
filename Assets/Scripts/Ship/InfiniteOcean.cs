using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(10000)]
public class InfiniteOcean : MonoBehaviour
{
    [Header("Kamera")]
    [Tooltip("Hierarchy'den kameranızı buraya sürükleyin")]
    public Transform mainCamera;

    [Tooltip("Enabled only in ShipScene; other scenes retain legacy tiling.")]
    public bool worldSpaceWater;
    public Texture2D oceanTexture;
    public Vector2 repeatSize = new Vector2(40, 30);
    public Color tint = Color.white;
    public Vector2 currentDirection = new Vector2(1, .25f);
    [Min(0)] public float currentSpeed = .12f;
    [Range(0, 1)] public float waveStrength;
    [Min(0)] public float waveSpeed = .3f;
    public float coverageMargin = 12;
    [Header("Beyaz dalga çizgileri (0 = kapalı)")]
    [Range(0, 2)] public float crestDisplacementPixels;
    [Min(0)] public float crestSpeed = 1.1f;
    [Range(0, .3f)] public float crestShimmer = .14f;
    [Range(2, 12)] public float crestFramesPerSecond = 6;
    SpriteRenderer surface;
    Camera view;
    MaterialPropertyBlock properties;
    float elapsed;
    private float tileSizeX;
    private float tileSizeY;

    void Start()
    {
        if (mainCamera == null)
        {
            if (Camera.main) mainCamera = Camera.main.transform;
        }

        // Tiled olarak kullandığın görselin "tek bir karesinin" dünya boyutunu otomatik hesaplıyoruz
        surface = GetComponent<SpriteRenderer>();
        if (mainCamera) view = mainCamera.GetComponent<Camera>();
        properties = new MaterialPropertyBlock();
        Sprite sprite = surface.sprite;
        if (!sprite) return;
        tileSizeX = sprite.bounds.size.x;
        tileSizeY = sprite.bounds.size.y;
    }

    void LateUpdate()
    {
        if (!mainCamera || !surface || !surface.sprite) return;
        elapsed += Time.deltaTime;
        if (worldSpaceWater)
        {
            Vector3 center = mainCamera.position;
            center.z = transform.position.z;
            transform.position = center;
            if (view && view.orthographic)
            {
                float height = view.orthographicSize * 2 + coverageMargin * 2;
                float width = view.orthographicSize * 2 * view.aspect + coverageMargin * 2;
                transform.localScale = new Vector3(width / surface.sprite.bounds.size.x, height / surface.sprite.bounds.size.y, 1);
            }
            surface.GetPropertyBlock(properties);
            properties.SetTexture("_OceanTex", oceanTexture ? oceanTexture : surface.sprite.texture);
            var texture = oceanTexture ? oceanTexture : surface.sprite.texture;
            properties.SetVector("_OceanPixelSize", new Vector4(1f / texture.width, 1f / texture.height, texture.width, texture.height));
            properties.SetVector("_RepeatSize", new Vector4(Mathf.Max(.1f, repeatSize.x), Mathf.Max(.1f, repeatSize.y), 0, 0));
            Vector2 flow = currentDirection.normalized * currentSpeed;
            properties.SetVector("_Flow", new Vector4(flow.x, flow.y, waveStrength, waveSpeed));
            properties.SetFloat("_OceanTime", elapsed);
            properties.SetColor("_Tint", tint);
            SetCrestProperties(false);
            surface.SetPropertyBlock(properties);
            return;
        }
        if (tileSizeX <= 0 || tileSizeY <= 0) return;
        // Kameranın mevcut pozisyonunu al
        Vector3 camPos = mainCamera.position;

        // Arka planı, kameranın pozisyonuna en yakın "kare (tile)" katlarına yuvarlayarak oturt
        float snapX = Mathf.Round(camPos.x / tileSizeX) * tileSizeX;
        float snapY = Mathf.Round(camPos.y / tileSizeY) * tileSizeY;

        // Su objesini yeni pozisyonuna ışınla (Z eksenindeki derinliği bozmadan)
        transform.position = new Vector3(snapX, snapY, transform.position.z);
        // Legacy sprite-sheet seas keep their Animator, tiling and original artwork.
        // Only explicitly configured ship surfaces use the crest shader.
        if (surface.sharedMaterial && surface.sharedMaterial.HasProperty("_CrestMotion"))
        {
            surface.GetPropertyBlock(properties);
            var texture = surface.sprite.texture;
            var rect = surface.sprite.textureRect;
            properties.SetVector("_OceanPixelSize", new Vector4(1f/texture.width,1f/texture.height,texture.width,texture.height));
            properties.SetVector("_SourceRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));
            properties.SetVector("_Flow", new Vector4(0,0,0,waveSpeed));
            properties.SetFloat("_OceanTime",elapsed);
            properties.SetColor("_Tint",tint);
            SetCrestProperties(true);
            surface.SetPropertyBlock(properties);
        }
    }
    void SetCrestProperties(bool spriteUV)
    {
        properties.SetFloat("_UseSpriteUV",spriteUV?1:0);
        if (!spriteUV) properties.SetVector("_SourceRect", new Vector4(0,0,1,1));
        properties.SetVector("_CrestMotion",new Vector4(crestDisplacementPixels,crestSpeed,crestShimmer,crestFramesPerSecond));
    }
}

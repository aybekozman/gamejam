using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class InfiniteOcean : MonoBehaviour
{
    [Header("Kamera")]
    [Tooltip("Hierarchy'den kameranızı buraya sürükleyin")]
    public Transform mainCamera;

    private float tileSizeX;
    private float tileSizeY;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
        }

        // Tiled olarak kullandığın görselin "tek bir karesinin" dünya boyutunu otomatik hesaplıyoruz
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        tileSizeX = sprite.bounds.size.x;
        tileSizeY = sprite.bounds.size.y;
    }

    void LateUpdate()
    {
        // Kameranın mevcut pozisyonunu al
        Vector3 camPos = mainCamera.position;

        // Arka planı, kameranın pozisyonuna en yakın "kare (tile)" katlarına yuvarlayarak oturt
        float snapX = Mathf.Round(camPos.x / tileSizeX) * tileSizeX;
        float snapY = Mathf.Round(camPos.y / tileSizeY) * tileSizeY;

        // Su objesini yeni pozisyonuna ışınla (Z eksenindeki derinliği bozmadan)
        transform.position = new Vector3(snapX, snapY, transform.position.z);
    }
}
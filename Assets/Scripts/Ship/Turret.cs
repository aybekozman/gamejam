using UnityEngine;
using System.Collections;

public class Turret : MonoBehaviour
{
    [Header("Bileşenler")]
    public Transform firePoint;
    public SpriteRenderer flashRenderer;
    public GameObject bulletPrefab;
    public Sprite[] flashFrames;

    [Header("Ses ve Efekt")]
    public AudioSource audioSource;
    public AudioClip shootSound;

    [Header("Geri Tepme (Recoil)")]
    public Rigidbody2D shipRb;
    public float recoilForce = 3f;

    [Header("Ayarlar")]
    public float flashFrameRate = 0.05f;
    public float rotationOffset = 0f;

    private float bulletAngle;
    private Vector2 currentTargetDir;

    void Start()
    {
        bool isLeftTurret = transform.localPosition.x < 0;
        if (isLeftTurret && flashRenderer != null)
        {
            flashRenderer.flipX = true;
        }
    }

    public void AimAt(Transform target)
    {
        if (target == null) return;

        currentTargetDir = (target.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(currentTargetDir.y, currentTargetDir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, targetAngle + rotationOffset);
        bulletAngle = targetAngle - 90f;
    }

    public void Shoot()
    {
        Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, bulletAngle));

        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

        if (shipRb != null)
        {
            shipRb.AddForce(-currentTargetDir * recoilForce, ForceMode2D.Impulse);
        }

        if (gameObject.activeInHierarchy && flashFrames.Length > 0)
        {
            StartCoroutine(PlayFlashAnimation());
        }
    }

    private IEnumerator PlayFlashAnimation()
    {
        flashRenderer.enabled = true;
        foreach (Sprite frame in flashFrames)
        {
            flashRenderer.sprite = frame;
            yield return new WaitForSeconds(flashFrameRate);
        }
        flashRenderer.enabled = false;
    }
}
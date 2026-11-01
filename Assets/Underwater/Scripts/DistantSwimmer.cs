using UnityEngine;

namespace Abyss
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class DistantSwimmer : MonoBehaviour
    {
        Camera view;
        SpriteRenderer visual;
        Sprite[] frames;
        Vector3 worldPosition, initialCamera;
        Color tint;
        float speed, parallax, opacity, surface, width, age, phase;

        public void Initialize(Camera camera, Sprite[] animation, Color color, float alpha, float horizontalSpeed, float depthParallax, float surfaceY, float visualWidth)
        {
            view = camera;
            visual = GetComponent<SpriteRenderer>();
            frames = animation;
            tint = color;
            opacity = alpha;
            speed = horizontalSpeed;
            parallax = depthParallax;
            surface = surfaceY;
            width = visualWidth;
            worldPosition = transform.position;
            initialCamera = camera.transform.position;
            phase = Random.Range(0, 20f);
        }

        void LateUpdate()
        {
            if (!view) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            worldPosition.x += speed * Time.deltaTime;
            Vector3 position = worldPosition + (view.transform.position - initialCamera) * parallax;
            position.z = 0;
            position.y = Mathf.Min(position.y + Mathf.Sin(age * .35f + phase) * .12f, surface - width * .2f - .6f);
            transform.position = position;
            if (frames != null && frames.Length > 0) visual.sprite = frames[(int)((age + phase) * 3) % frames.Length];
            tint.a = opacity * Mathf.Clamp01(age / 2);
            visual.color = tint;
            float horizontalLimit = view.orthographicSize * view.aspect + width + 6;
            float dx = position.x - view.transform.position.x;
            bool passed = speed > 0 ? dx > horizontalLimit : dx < -horizontalLimit;
            bool leftBehind = Vector2.Distance(position, view.transform.position) > horizontalLimit + 45;
            // Passing life leaves outside the view; no visible lifetime-based deletion.
            if (age > 5 && (passed || leftBehind)) Destroy(gameObject);
        }
    }
}

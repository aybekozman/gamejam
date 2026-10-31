using System.Collections.Generic;
using UnityEngine;

namespace Abyss
{
    public class DistantOceanLife : MonoBehaviour
    {
        public Sprite[] singleFish;
        public Sprite[] schoolFrames;
        public Sprite[] blueWhaleFrames;
        public Sprite[] humpbackFrames;
        public int initialFishGroups = 8;
        public int maximumFishGroups = 12;
        public int maximumWhales = 2;
        public Vector2 fishInterval = new(2.5f, 4.5f);
        public Vector2 whaleInterval = new(24, 38);
        [Range(0, 1)] public float fishOpacity = .34f;
        [Range(0, 1)] public float whaleOpacity = .28f;
        [Range(0, .5f)] public float parallax = .16f;
        public float surfaceY = 12;
        public int ActiveFishGroups => fishGroups.FindAll(g => g).Count;
        public int ActiveWhales => whales.FindAll(g => g).Count;
        readonly List<DistantSwimmer> fishGroups = new();
        readonly List<DistantSwimmer> whales = new();
        Camera view;
        float nextFish, nextWhale;

        void Start()
        {
            view = Camera.main;
            if (!view) { enabled = false; return; }
            for (int i = 0; i < initialFishGroups; i++) Spawn(false, true, i % 2 == 0);
            Spawn(true, true, false);
            nextFish = Random.Range(fishInterval.x, fishInterval.y);
            nextWhale = Random.Range(whaleInterval.x, whaleInterval.y);
        }

        void Update()
        {
            fishGroups.RemoveAll(g => !g);
            whales.RemoveAll(g => !g);
            if ((nextFish -= Time.deltaTime) <= 0)
            {
                nextFish = Random.Range(fishInterval.x, fishInterval.y);
                if (fishGroups.Count < maximumFishGroups) Spawn(false, false, Random.value < .65f);
            }
            if ((nextWhale -= Time.deltaTime) <= 0)
            {
                nextWhale = Random.Range(whaleInterval.x, whaleInterval.y);
                if (whales.Count < maximumWhales) Spawn(true, false, false);
            }
        }

        void Spawn(bool whale, bool initial, bool school)
        {
            Sprite[] frames = whale ? (Random.value < .5f ? blueWhaleFrames : humpbackFrames) : school ? schoolFrames : singleFish;
            if (frames == null || frames.Length == 0 || !frames[0]) return;
            bool right = Random.value < .5f;
            float width = whale ? Random.Range(6, 9) : school ? Random.Range(4.5f, 7.5f) : Random.Range(1.1f, 2.2f);
            float halfWidth = view.orthographicSize * view.aspect;
            float x = view.transform.position.x + (initial ? Random.Range(-halfWidth * .8f, halfWidth * .8f) : (right ? -1 : 1) * (halfWidth + width + 2));
            float y = Mathf.Clamp(view.transform.position.y + Random.Range(-5, 5), -28, surfaceY - (whale ? 3 : 1.5f));
            var go = new GameObject(whale ? "Distant whale • background only" : school ? "Shadow fish school" : "Distant shadow fish");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(x, y, 0);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = whale || school ? frames[0] : frames[Random.Range(0, frames.Length)];
            renderer.sortingOrder = whale ? -48 : -42;
            renderer.color = new Color(.55f, .8f, .9f, 0);
            renderer.flipX = !right;
            go.transform.localScale = Vector3.one * width / renderer.sprite.bounds.size.x;
            var swimmer = go.AddComponent<DistantSwimmer>();
            swimmer.Initialize(view, whale || school ? frames : null, renderer.color, whale ? whaleOpacity : fishOpacity,
                (right ? 1 : -1) * (whale ? Random.Range(.45f, .7f) : Random.Range(.8f, 1.5f)), parallax, surfaceY, width);
            (whale ? whales : fishGroups).Add(swimmer);
        }
    }
}

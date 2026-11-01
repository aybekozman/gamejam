using System;
using System.Collections.Generic;
using UnityEngine;

namespace Abyss
{
    public enum HabitatFamily { ReefFish, GentleLife, Shark, Whale }

    [Serializable]
    public class OceanSpecies
    {
        public string name;
        public HabitatFamily family;
        public Sprite[] frames;
        public float width = 1.4f;
        public float speed = 1;
        public float animationRate = 4;
        public float roamingRadius = 8;
    }

    public class OceanHabitat : MonoBehaviour
    {
        public Transform player;
        public Vector2 villageCenter;
        public Vector2 worldMin = new(-240, -31);
        public Vector2 worldMax = new(240, 11);
        [Header("Distance from the village")]
        public float villageRadius = 24;
        public float reefFadeEnd = 60;
        [Tooltip("Left edge of the shark habitat, in world X coordinates.")]
        public float sharkRegionStartX = 180;
        [Range(0, 8)] public int sharkRegionReefFishCount = 3;
        [HideInInspector] public float sharkStart = 100;
        [Header("Local population")]
        public int villageFishCount = 18;
        public int villageGentleCount = 2;
        public int openWaterGentleCount = 5;
        public int sharkZoneGentleCount = 1;
        public int sharksPerArea = 3;
        public int maxPopulation = 32;
        public float spawnInterval = .7f;
        public float despawnDistance = 52;
        public float fadeDuration = .8f;
        public OceanSpecies[] species;

        readonly List<RoamingCreature> population = new();
        readonly Collider2D[] overlaps = new Collider2D[32];
        Camera view;
        float nextSpawn;
        public IReadOnlyList<RoamingCreature> Population => population;
        public float EffectiveDespawnDistance => Mathf.Max(despawnDistance, ViewRadius + 15);
        float ViewRadius => view ? view.orthographicSize * Mathf.Sqrt(1 + view.aspect * view.aspect) : 15;

        void Start()
        {
            view = Camera.main;
            if (!player) player = FindFirstObjectByType<FishPilot>()?.transform;
            // Keep the authored placement for editing. In Play Mode its creatures are replaced by this population.
            foreach (var drift in FindObjectsByType<Drift>(FindObjectsSortMode.None))
            {
                if (drift.plant) continue;
                drift.enabled = false;
                var renderer = drift.GetComponent<SpriteRenderer>();
                if (renderer) renderer.enabled = false;
            }
            foreach (var shadow in FindObjectsByType<ShadowPass>(FindObjectsSortMode.None)) shadow.enabled = false;
            if (!player) { enabled = false; return; }
            for (int i = 0; i < maxPopulation * 4; i++) TrySpawn(true);
        }

        void Update()
        {
            population.RemoveAll(c => !c);
            if ((nextSpawn -= Time.deltaTime) > 0) return;
            nextSpawn = spawnInterval;
            for (int i = 0; i < 3; i++) TrySpawn(false);
        }

        Abyss.Story.UnderwaterThirdChapter warChapter;
        public bool WarRouteActive{get{if(!warChapter)warChapter=FindFirstObjectByType<Abyss.Story.UnderwaterThirdChapter>();return warChapter&&warChapter.Started;}}
        public bool WarRoute(Vector2 p)=>WarRouteActive&&p.x>-160&&p.x<170;
        public float VillageDistance(Vector2 position) => Vector2.Distance(position, villageCenter);
        public bool Allowed(OceanSpecies definition, Vector2 position)
        {
            if (position.x < worldMin.x || position.x > worldMax.x || position.y < worldMin.y || position.y > worldMax.y) return false;
            float distance = VillageDistance(position);
            switch (definition.family)
            {
                case HabitatFamily.ReefFish: return WarRoute(position)||distance < reefFadeEnd || position.x >= sharkRegionStartX;
                case HabitatFamily.Shark: return position.x >= sharkRegionStartX;
                case HabitatFamily.Whale: return false; // Whales belong exclusively to the distant background.
                default: return true;
            }
        }

        public int TargetCount(HabitatFamily family, Vector2 position)
        {
            float distance = VillageDistance(position);
            switch (family)
            {
                case HabitatFamily.ReefFish:
                    return WarRoute(position)?8:position.x >= sharkRegionStartX ? sharkRegionReefFishCount : Mathf.RoundToInt(villageFishCount * (1 - Mathf.InverseLerp(villageRadius, reefFadeEnd, distance)));
                case HabitatFamily.Shark: return position.x >= sharkRegionStartX ? sharksPerArea : 0;
                case HabitatFamily.Whale: return 0;
                default:
                    return distance <= villageRadius ? villageGentleCount : position.x < sharkRegionStartX ? openWaterGentleCount : sharkZoneGentleCount;
            }
        }

        public bool ClearWater(Vector2 point, float clearance)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.OverlapCircle(point, clearance, filter, overlaps);
            for (int i = 0; i < count; i++)
            {
                var collider = overlaps[i];
                if (!collider || !collider.enabled || collider.isTrigger || collider.attachedRigidbody) continue;
                return false;
            }
            return true;
        }

        bool InView(Vector2 position, float padding = .08f)
        {
            var p = view.WorldToViewportPoint(position);
            return p.x > -padding && p.x < 1 + padding && p.y > -padding && p.y < 1 + padding;
        }

        bool TrySpawn(bool initial)
        {
            if (!player || !view || species == null || species.Length == 0 || population.Count >= maxPopulation) return false;
            var family = (HabitatFamily)UnityEngine.Random.Range(0, 3);
            int current = 0;
            foreach (var creature in population) if (creature && creature.definition.family == family) current++;
            if (current >= TargetCount(family, player.position)) return false;
            var candidates = Array.FindAll(species, s => s.family == family && s.frames != null && s.frames.Length > 0 && s.frames[0]);
            if (candidates.Length == 0) return false;
            var definition = candidates[UnityEngine.Random.Range(0, candidates.Length)];
            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(initial ? 3 : ViewRadius + 3, ViewRadius + 13);
                Vector2 point = (Vector2)player.position + offset;
                if (!Allowed(definition, point) || (!initial && InView(point)) || !ClearWater(point, definition.width * .3f)) continue;
                if (family == HabitatFamily.ReefFish)
                {
                    float density = WarRoute(point)?.65f:point.x >= sharkRegionStartX ? .35f : 1 - Mathf.InverseLerp(villageRadius, reefFadeEnd, VillageDistance(point));
                    if (UnityEngine.Random.value > density) continue;
                }
                bool crowded = false;
                foreach (var other in population) if (other && Vector2.Distance(point, other.transform.position) < (other.definition.width + definition.width) * .45f) { crowded = true; break; }
                if (crowded) continue;
                var go = new GameObject(definition.name + " • roaming");
                go.transform.SetParent(transform, false);
                go.transform.position = point;
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = definition.frames[0];
                renderer.sortingOrder = definition.family == HabitatFamily.Whale ? 6 : 8;
                renderer.color = new Color(1, 1, 1, 0);
                go.transform.localScale = Vector3.one * definition.width / renderer.sprite.bounds.size.x;
                var roaming = go.AddComponent<RoamingCreature>();
                roaming.Initialize(this, definition, point);
                population.Add(roaming);
                return true;
            }
            return false;
        }
    }
}


using UnityEngine;

namespace Abyss
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class RoamingCreature : MonoBehaviour
    {
        public OceanSpecies definition;
        public OceanHabitat habitat;
        SpriteRenderer visual;
        Vector2 anchor, destination, velocity;
        float chooseAt, age, phase, opacity;
        bool retiring;

        public void Initialize(OceanHabitat owner, OceanSpecies species, Vector2 origin)
        {
            habitat = owner;
            definition = species;
            anchor = origin;
            visual = GetComponent<SpriteRenderer>();
            phase = Random.Range(0, 100f);
            ChooseDestination();
        }

        void ChooseDestination()
        {
            chooseAt = Random.Range(3, 8);
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var candidate = anchor + Random.insideUnitCircle * definition.roamingRadius;
                if (definition.name.Contains("Jelly") || definition.name.Contains("Seahorse")) candidate.x = Mathf.Lerp(anchor.x, candidate.x, .3f);
                if (!habitat.Allowed(definition, candidate) || !habitat.ClearWater(candidate, definition.width * .3f)) continue;
                destination = candidate;
                return;
            }
            destination = transform.position;
        }

        void Update()
        {
            if (!habitat || !habitat.player) { Destroy(gameObject); return; }
            float distance = Vector2.Distance(transform.position, habitat.player.position);
            if (distance > habitat.EffectiveDespawnDistance) retiring = true;
            opacity = Mathf.MoveTowards(opacity, retiring ? 0 : 1, Time.deltaTime / Mathf.Max(.1f, habitat.fadeDuration));
            visual.color = new Color(1, 1, 1, opacity);
            if (retiring && opacity <= 0) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            visual.sprite = definition.frames[(int)((age + phase) * definition.animationRate) % definition.frames.Length];
            // Sprite canvas and body landmark are aligned offline. Scale stays constant throughout the animation.
            if ((chooseAt -= Time.deltaTime) <= 0 || Vector2.Distance(transform.position, destination) < .5f) ChooseDestination();
            Vector2 desired = (destination - (Vector2)transform.position).normalized * definition.speed;
            velocity = Vector2.MoveTowards(velocity, desired, definition.speed * 1.4f * Time.deltaTime);
            Vector2 next = (Vector2)transform.position + velocity * Time.deltaTime;
            if (habitat.Allowed(definition, next) && habitat.ClearWater(next, definition.width * .28f)) transform.position = next;
            else { velocity *= -.2f; ChooseDestination(); }
            if (Mathf.Abs(velocity.x) > .08f) visual.flipX = velocity.x < 0;
        }
    }
}

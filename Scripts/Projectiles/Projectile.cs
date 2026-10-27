using UnityEngine;
namespace SpaceInvaders
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] bool fromPlayer;
        [SerializeField] float speed = 12, lifetime = 4;
        [SerializeField] float enemyStartSpeed = 3.5f; // velocidad inicial del disparo enemigo
        Rigidbody2D body;
        Collider2D hitbox;
        bool consumed;
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        ContactFilter2D filter;
        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<Collider2D>();
            filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(fromPlayer ? LayerMask.GetMask("Invader", "Barrier") : LayerMask.GetMask("Ship", "Barrier"));
            Destroy(gameObject, lifetime);
        }
        void Start()
        {
            // El disparo enemigo sale lento y la gravedad lo va acelerando
            if (!fromPlayer) body.linearVelocity = Vector2.down * enemyStartSpeed;
        }
        void FixedUpdate()
        {
            if (consumed || !GameManager.Instance.Playing) return;

            Vector2 velocity = body.linearVelocity;
            Vector2 direction = fromPlayer ? Vector2.up
                : (velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector2.down);
            float currentSpeed = fromPlayer ? speed : velocity.magnitude;

            // El barrido usa la velocidad actual, así no atraviesa nada al acelerar
            int count = hitbox.Cast(direction, filter, hits, currentSpeed * Time.fixedDeltaTime + 0.02f);
            if (count > 0)
            {
                int closest = 0;
                for (int i = 1; i < count; i++) if (hits[i].distance < hits[closest].distance) closest = i;
                Impact(hits[closest].collider);
            }
            // Solo el disparo del jugador mantiene velocidad fija
            if (!consumed && fromPlayer) body.linearVelocity = direction * speed;
            if (Mathf.Abs(body.position.y) > 5.5f) Destroy(gameObject);
        }
        void OnTriggerEnter2D(Collider2D other) { Impact(other); }
        void Impact(Collider2D other)
        {
            if (consumed || !GameManager.Instance.Playing) return;
            var barrier = other.GetComponent<Barrier>();
            var enemy = other.GetComponent<EnemyController>();
            var player = other.GetComponent<PlayerHealth>();
            if (barrier == null && !(fromPlayer && enemy != null) && !(!fromPlayer && player != null)) return;
            consumed = true; hitbox.enabled = false; body.linearVelocity = Vector2.zero;
            if (barrier != null) barrier.Hit();
            else if (fromPlayer) enemy.Hit();
            else player.Hit(body.position);
            Destroy(gameObject);
        }
    }
}
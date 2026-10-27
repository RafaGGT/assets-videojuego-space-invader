using UnityEngine;
namespace SpaceInvaders
{
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] int points = 10;
        [SerializeField] ExplosionEffect explosion;
        public int Column { get; set; }
        public EnemyManager Formation { get; set; }
        public Rigidbody2D Body { get; private set; }
        bool dead;
        void Awake() { Body = GetComponent<Rigidbody2D>(); }
        public void Hit()
        {
            if (dead) return;
            dead = true;
            GetComponent<Collider2D>().enabled = false;
            Instantiate(explosion, transform.position, Quaternion.identity);
            AudioManager.Instance.Explosion();
            ScoreManager.Instance.Add(points);
            Formation.Remove(this);
            Destroy(gameObject);
        }
    }
}

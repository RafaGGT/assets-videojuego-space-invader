using UnityEngine;
namespace SpaceInvaders
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] int initialLives = 3;
        [SerializeField] float invulnerability = 1.5f;
        public int Lives { get; private set; }
        float protectedUntil;
        SpriteRenderer visual;
        void Awake() { Lives = initialLives; visual = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            visual.color = Time.time < protectedUntil && Mathf.FloorToInt(Time.time * 14) % 2 == 0
                ? new Color(1, 0.35f, 0.4f, 0.4f) : Color.white;
        }
        public void Hit(Vector2 source)
        {
            if (!GameManager.Instance.Playing || Time.time < protectedUntil) return;
            Lives--; protectedUntil = Time.time + invulnerability;
            GetComponent<PlayerController>().Recoil(source.x >= transform.position.x ? -1 : 1);
            AudioManager.Instance.Damage();
            if (Lives <= 0) GameManager.Instance.EndGame(false);
        }
    }
}

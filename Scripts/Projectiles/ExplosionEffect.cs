using UnityEngine;
namespace SpaceInvaders
{
    public sealed class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] float duration = 0.3f;
        float age;
        SpriteRenderer visual;
        Vector3 startScale;
        void Awake() { visual = GetComponent<SpriteRenderer>(); startScale = transform.localScale; }
        void Update()
        {
            age += Time.deltaTime;
            float t = age / duration;
            transform.localScale = startScale * (0.5f + t * 1.5f);
            visual.color = new Color(1, 0.7f, 0.25f, 1 - t);
            transform.Rotate(0, 0, 160 * Time.deltaTime);
            if (age >= duration) Destroy(gameObject);
        }
    }
}

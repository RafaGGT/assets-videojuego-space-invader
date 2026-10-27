using UnityEngine;
namespace SpaceInvaders
{
    public sealed class Barrier : MonoBehaviour
    {
        [SerializeField] int hitPoints = 12;
        [SerializeField] Sprite[] damageStages;
        int remaining;
        void Awake() { remaining = hitPoints; }
        public void Hit()
        {
            remaining--;
            if (remaining <= 0) { Destroy(gameObject); return; }
            int stage = Mathf.Min(damageStages.Length - 1, (hitPoints - remaining) * damageStages.Length / hitPoints);
            GetComponent<SpriteRenderer>().sprite = damageStages[stage];
            GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(1, 0.4f, 0.25f), Color.white, (float)remaining / hitPoints);
        }
    }
}

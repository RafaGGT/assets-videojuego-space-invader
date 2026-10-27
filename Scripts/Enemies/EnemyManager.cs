using System.Collections.Generic;
using UnityEngine;
namespace SpaceInvaders
{
    public sealed class EnemyManager : MonoBehaviour
    {
        [SerializeField] Projectile projectile;
        [SerializeField] float speed = 0.65f, sideLimit = 7.8f, descent = 0.35f, defenseLine = -2.35f;
        [SerializeField] float shotInterval = 0.85f;
        [SerializeField] float descentSpeedBoost = 0.8f;
        readonly List<EnemyController> enemies = new List<EnemyController>();
        readonly List<EnemyController> shooters = new List<EnemyController>();
        float direction = 1, nextShot = 1.5f;
        int descents;
        public int Remaining => enemies.Count;
        void Start()
        {
            foreach (var enemy in GetComponentsInChildren<EnemyController>())
            {
                enemy.Formation = this;
                enemy.Column = Mathf.RoundToInt((enemy.transform.position.x + 5.25f) / 1.5f);
                enemies.Add(enemy);
            }
        }
        void FixedUpdate()
        {
            if (!GameManager.Instance.Playing || enemies.Count == 0) return;
            float step = speed * (1 + (24 - enemies.Count) * 0.045f)
                       * (1 + descents * descentSpeedBoost) * Time.fixedDeltaTime;
            bool turn = false;
            foreach (var enemy in enemies)
                if (direction > 0 ? enemy.Body.position.x + step >= sideLimit : enemy.Body.position.x - step <= -sideLimit) turn = true;
            if (turn) { direction *= -1; descents++; }
            foreach (var enemy in enemies)
            {
                Vector2 position = enemy.Body.position + new Vector2(direction * step, turn ? -descent : 0);
                enemy.Body.MovePosition(position);
                if (position.y - 0.4f <= defenseLine) { GameManager.Instance.EndGame(false); return; }
            }
            if (Time.time >= nextShot)
            {
                nextShot = Time.time + shotInterval * Random.Range(0.8f, 1.3f);
                shooters.Clear();
                foreach (var enemy in enemies)
                {
                    bool bottom = true;
                    foreach (var other in enemies)
                        if (other.Column == enemy.Column && other.Body.position.y < enemy.Body.position.y) { bottom = false; break; }
                    if (bottom) shooters.Add(enemy);
                }
                var shooter = shooters[Random.Range(0, shooters.Count)];
                Instantiate(projectile, shooter.transform.position + Vector3.down * 0.5f, Quaternion.identity);
            }
        }
        public void Remove(EnemyController enemy)
        {
            enemies.Remove(enemy);
            if (enemies.Count == 0) GameManager.Instance.EndGame(true);
        }
    }
}
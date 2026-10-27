using TMPro;
using UnityEngine;
namespace SpaceInvaders
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] TMP_Text score, lives, remaining;
        [SerializeField] PlayerHealth player;
        [SerializeField] EnemyManager formation;
        [SerializeField] GameObject pausePanel;
        void Update()
        {
            score.text = $"PUNTAJE  {ScoreManager.Instance.Score:0000}";
            lives.text = $"VIDAS  {player.Lives}";
            remaining.text = $"INVASORES  {formation.Remaining:00}";
            pausePanel.SetActive(GameManager.Instance.Paused);
        }
        public void Resume() { if (GameManager.Instance.Paused) GameManager.Instance.TogglePause(); }
    }
}

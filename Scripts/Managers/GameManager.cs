using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace SpaceInvaders
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static int FinalScore { get; private set; }
        public bool Paused { get; private set; }
        public bool Finished { get; private set; }
        public bool Playing => !Paused && !Finished;
        void Awake() { Instance = this; Time.timeScale = 1; FinalScore = 0; }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
        }
        public void TogglePause()
        {
            if (Finished) return;
            Paused = !Paused;
            Time.timeScale = Paused ? 0 : 1;
            AudioManager.Instance.SetPaused(Paused);
        }
        public void EndGame(bool victory)
        {
            if (Finished) return;
            Finished = true;
            FinalScore = ScoreManager.Instance.Score;
            Time.timeScale = 1;
            SceneManager.LoadScene(victory ? "Victory" : "GameOver");
        }
        void OnDestroy() { if (Instance == this) Instance = null; Time.timeScale = 1; }
    }
}

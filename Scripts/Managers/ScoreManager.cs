using UnityEngine;
namespace SpaceInvaders
{
    public sealed class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }
        public int Score { get; private set; }
        void Awake() { Instance = this; Score = 0; }
        public void Add(int points) { Score += points; }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}

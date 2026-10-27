using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SpaceInvaders
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] TMP_Text finalScore;
        void Start() { if (finalScore != null) finalScore.text = $"PUNTAJE FINAL  {GameManager.FinalScore:0000}"; }
        public void Play() { Time.timeScale = 1; SceneManager.LoadScene("Game"); }
        public void Menu() { Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); }
        public void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}

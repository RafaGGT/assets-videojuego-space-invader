using UnityEngine;
using UnityEngine.SceneManagement;
namespace SpaceInvaders
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        [SerializeField] AudioSource music, effects;
        [SerializeField] AudioClip bgm, shot, explosion, damage, victory, defeat;
        [SerializeField, Range(0, 1)] float musicVolume = 0.22f, effectsVolume = 0.5f;
        float lastShot = -1, lastExplosion = -1;
        void Awake() { Instance = this; }
        void Start()
        {
            music.volume = musicVolume; effects.volume = effectsVolume;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "Game") { music.clip = bgm; music.loop = true; music.Play(); }
            if (scene == "Victory") effects.PlayOneShot(victory);
            if (scene == "GameOver") effects.PlayOneShot(defeat);
        }
        public void Shot()
        {
            if (Time.unscaledTime - lastShot < 0.06f) return;
            lastShot = Time.unscaledTime; effects.PlayOneShot(shot, 0.55f);
        }
        public void Explosion()
        {
            if (Time.unscaledTime - lastExplosion < 0.04f) return;
            lastExplosion = Time.unscaledTime; effects.PlayOneShot(explosion, 0.7f);
        }
        public void Damage() { effects.PlayOneShot(damage, 0.8f); }
        public void SetPaused(bool paused) { if (paused) music.Pause(); else music.UnPause(); }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}

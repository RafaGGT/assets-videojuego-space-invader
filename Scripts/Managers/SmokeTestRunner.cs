using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceInvaders
{
    // Prueba de integración optativa para la build: --smoke-test. No se activa al jugar normalmente.
    public sealed class SmokeTestRunner : MonoBehaviour
    {
        string report;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("--smoke-test")) return;
            var go = new GameObject("Automated integration checks"); DontDestroyOnLoad(go); go.AddComponent<SmokeTestRunner>();
        }
        void Awake()
        {
            Application.runInBackground = true;
            UnityEngine.Random.InitState(2317);
            report = Path.Combine(Application.dataPath, "..", "SmokeTest.txt");
            File.WriteAllText(report, "Space Invaders integration checks\n");
            Application.logMessageReceived += OnLog;
            StartCoroutine(Watchdog());
        }
        IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(45);
            File.AppendAllText(report, "FAIL watchdog timeout\n"); Application.Quit(2);
        }
        void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            { File.AppendAllText(report, "FAIL " + message + "\n" + stack + "\n"); Application.Quit(1); }
        }
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Smoke test: " + message);
            File.AppendAllText(report, "PASS " + message + "\n");
        }
        static T Find<T>() where T : UnityEngine.Object => FindAnyObjectByType<T>();
        static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        static void Click(string name) => FindObjectsByType<Button>().First(b => b.name == name).onClick.Invoke();
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "..", name + "Screenshot.png"));
            yield return null;
        }
        IEnumerator Start()
        {
            yield return null;
            Check(SceneManager.GetActiveScene().name == "MainMenu", "Initial menu scene");
            yield return Capture("MainMenu");
            Click("JUGAR"); yield return null; yield return null;
            Check(Find<EnemyManager>().Remaining == 24 && Find<PlayerHealth>().Lives == 3, "Play button, 24 enemies, 3 lives");
            Check(Field<AudioSource>(AudioManager.Instance, "music").isPlaying, "BGM playing");
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "..", "GameScreenshot.png"));
            var player = Find<PlayerController>(); var health = Find<PlayerHealth>();
            float original = player.transform.position.x;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.25f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            Check(player.transform.position.x > original + 0.5f, "Input System D moves rigidbody");
            var shipBody = player.GetComponent<Rigidbody2D>();
            var savedPosition = shipBody.position;
            shipBody.position = new Vector2(7.98f, savedPosition.y);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.1f);
            Check(shipBody.position.x <= 8.001f, "Ship cannot cross horizontal limit");
            shipBody.position = savedPosition;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Space));
            yield return new WaitForSeconds(0.12f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            Check(FindObjectsByType<Projectile>().Length == 1, "Space and cooldown produce one projectile");
            Check(Field<AudioSource>(AudioManager.Instance, "effects").isPlaying, "Shot SFX playing");
            foreach (var shot in FindObjectsByType<Projectile>()) Destroy(shot.gameObject);
            GameManager.Instance.TogglePause(); var position = player.transform.position;
            yield return new WaitForSecondsRealtime(0.15f);
            Check(Time.timeScale == 0 && player.transform.position == position, "Pause freezes physics");
            yield return Capture("Pause");
            GameManager.Instance.TogglePause(); Check(Time.timeScale == 1, "Resume");
            var formation = Find<EnemyManager>(); float previousY = Find<EnemyController>().transform.position.y;
            foreach (var enemy in FindObjectsByType<EnemyController>()) enemy.Body.position += Vector2.right * 2.6f;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Check(Find<EnemyController>().transform.position.y < previousY, "Formation descends at side boundary");
            var target = FindObjectsByType<EnemyController>().OrderBy(e => e.transform.position.y).First();
            Instantiate(Field<Projectile>(player, "projectile"), target.transform.position + Vector3.down * 0.8f, Quaternion.identity);
            yield return new WaitForSeconds(0.16f);
            Check(formation.Remaining == 23 && ScoreManager.Instance.Score == 10, "Swept projectile hits enemy and adds row score");
            var shield = Find<Barrier>(); var sprite = shield.GetComponent<SpriteRenderer>().sprite;
            var enemyProjectile = Field<Projectile>(formation, "projectile");
            Instantiate(enemyProjectile, shield.transform.position + new Vector3(-0.6f, 0.8f), Quaternion.identity);
            yield return new WaitForSeconds(0.2f);
            Check(Field<int>(shield, "remaining") == 11, "Barrier blocks physical enemy projectile");
            for (int i = 0; i < 4; i++) shield.Hit();
            Check(shield.GetComponent<SpriteRenderer>().sprite != sprite, "Barrier damage changes sprite");
            for (int i = 0; i < 7; i++) shield.Hit();
            yield return null; Check(shield == null, "Barrier destroyed at zero HP");
            Instantiate(enemyProjectile, player.transform.position + Vector3.up * 0.7f, Quaternion.identity);
            yield return new WaitForSeconds(0.15f);
            Check(health.Lives == 2, "Enemy projectile damages ship");
            health.Hit(player.transform.position); Check(health.Lives == 2, "Invulnerability prevents double damage");
            Check(Mathf.Abs(Field<float>(player, "recoil")) > 0, "Physical hit recoil");
            yield return new WaitForSeconds(1.6f);
            Check(FindObjectsByType<Projectile>().Any(p => !Field<bool>(p, "fromPlayer")), "Formation fires enemy projectiles");
            health.Hit(player.transform.position); yield return new WaitForSeconds(1.6f); health.Hit(player.transform.position);
            yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == "GameOver", "Zero lives leads to GameOver");
            Check(Field<AudioSource>(AudioManager.Instance, "effects").isPlaying, "Defeat SFX playing");
            yield return Capture("GameOver");
            Click("REINTENTAR"); yield return null; yield return null;
            Check(Find<PlayerHealth>().Lives == 3 && Find<EnemyManager>().Remaining == 24 && ScoreManager.Instance.Score == 0 && FindObjectsByType<Barrier>().Length == 3, "Retry resets all state");
            foreach (var enemy in FindObjectsByType<EnemyController>()) enemy.Body.position += Vector2.down * 6;
            yield return new WaitForFixedUpdate(); yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == "GameOver", "Defense line invasion ends game");
            Click("REINTENTAR"); yield return null; yield return null;
            foreach (var enemy in FindObjectsByType<EnemyController>()) enemy.Hit();
            yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == "Victory" && GameManager.FinalScore == 480, "24 kills produce victory and 480 points");
            Check(Field<AudioSource>(AudioManager.Instance, "effects").isPlaying, "Victory SFX playing");
            yield return Capture("Victory");
            Click("VOLVER A JUGAR"); yield return null; yield return null;
            Check(Find<EnemyManager>().Remaining == 24, "Victory replay button");
            Find<MainMenuController>().Menu(); yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == "MainMenu", "Return to menu");
            File.AppendAllText(report, "ALL CHECKS PASSED\n");
            Application.Quit(0);
        }
        void OnDestroy() { Application.logMessageReceived -= OnLog; }
    }
}

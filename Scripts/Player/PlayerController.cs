using UnityEngine;
using UnityEngine.InputSystem;
namespace SpaceInvaders
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] float speed = 7, cooldown = 0.26f, horizontalLimit = 8;
        [SerializeField] Projectile projectile;
        Rigidbody2D body;
        float axis, nextShot, recoil;
        void Awake() { body = GetComponent<Rigidbody2D>(); }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !GameManager.Instance.Playing) { axis = 0; return; }
            axis = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                 - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            if (keyboard.spaceKey.isPressed && Time.time >= nextShot)
            {
                nextShot = Time.time + cooldown;
                Instantiate(projectile, body.position + Vector2.up * 0.6f, Quaternion.identity);
                AudioManager.Instance.Shot();
            }
        }
        void FixedUpdate()
        {
            if (!GameManager.Instance.Playing) return;
            recoil = Mathf.MoveTowards(recoil, 0, 10 * Time.fixedDeltaTime);
            float velocity = axis * speed + recoil;
            // Limitar la velocidad del siguiente paso evita salir del área incluso con retroceso.
            velocity = Mathf.Clamp(velocity, (-horizontalLimit - body.position.x) / Time.fixedDeltaTime,
                (horizontalLimit - body.position.x) / Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(velocity, 0);
        }
        public void Recoil(float direction) { recoil = direction * 3; }
    }
}

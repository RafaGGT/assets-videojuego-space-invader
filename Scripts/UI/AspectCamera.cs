using UnityEngine;
namespace SpaceInvaders
{
    [RequireComponent(typeof(Camera))]
    public sealed class AspectCamera : MonoBehaviour
    {
        void OnPreCull()
        {
            // Mantener todo el campo 16:9 visible en ventanas más estrechas.
            var cameraComponent = GetComponent<Camera>();
            cameraComponent.orthographicSize = Mathf.Max(5, 5 * (16f / 9f) / cameraComponent.aspect);
        }
    }
}

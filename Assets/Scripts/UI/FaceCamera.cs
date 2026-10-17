using UnityEngine;

/// <summary>
/// Keeps a world-space label (e.g. a monster's level text) turned to the camera, so it always reads straight.
/// Put on the label object itself.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    [Tooltip("Camera to face. If empty, Camera.main is used.")]
    [SerializeField] private Camera _camera;

    // After all movement and camera updates, so the label doesn't lag a frame behind.
    private void LateUpdate()
    {
        Camera target = _camera != null ? _camera : Camera.main;
        if (target == null)
            return;

        // Copying the camera's rotation (not LookAt) keeps the text facing the screen without mirroring it.
        transform.rotation = target.transform.rotation;
    }
}

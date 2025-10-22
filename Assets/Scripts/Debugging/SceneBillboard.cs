using UnityEngine;

#if UNITY_EDITOR
// UnityEditor types are only referenced inside editor-only blocks
#endif

[ExecuteAlways]
public class SceneBillboard : MonoBehaviour
{
    [Tooltip("If true the billboard will only rotate around Y so it stays upright.")]
    public bool lockYAxis = true;

    [Tooltip("Flip 180°; TMP and many quads face -Z by default.")]
    public bool flipForTMP = true;

    [Tooltip("If true the script will prefer the Editor SceneView camera even during Play mode.")]
    public bool preferSceneViewCamera = true;

    void LateUpdate()
    {
        Camera cam = null;

#if UNITY_EDITOR
        // Prefer the SceneView camera
        if (preferSceneViewCamera)
        {
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView == null && UnityEditor.SceneView.sceneViews.Count > 0)
                sceneView = UnityEditor.SceneView.sceneViews[0] as UnityEditor.SceneView;

            if (sceneView != null && sceneView.camera != null)
            {
                cam = sceneView.camera;
            }
        }
#endif

        // Fallback to main/current camera at runtime (or if SceneView not found)
        if (cam == null)
            cam = Camera.main ?? Camera.current;

        if (cam == null)
            return;

        Vector3 lookTarget = cam.transform.position;

        if (lockYAxis)
            lookTarget.y = transform.position.y; // keep upright

        transform.LookAt(lookTarget, Vector3.up);

        if (flipForTMP)
            transform.Rotate(0f, 180f, 0f);
    }
}

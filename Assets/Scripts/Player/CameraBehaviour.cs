using Cinemachine;
using UnityEditor.VersionControl;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";

    [Header("Camera Settings")]
    [SerializeField] private float distance = 15f;   // default distance behind player
    [SerializeField] private float height = 8f;     // height above player
    [SerializeField] private float orbitSpeed = 5f; // horizontal orbit speed
    [SerializeField] private float zoomSpeed = 2f;  // scroll zoom speed
    [SerializeField] private float minDistance = 10f; // min zoom
    [SerializeField] private float maxDistance = 20f; // max zoom
    private float defaultDistance = 0;

    private CinemachineVirtualCamera vcam;
    private Transform player;
    private float currentYaw = 0f; // horizontal rotation

    private void Awake()
    {
        // Ensure Main Camera has a CinemachineBrain
        if (Camera.main != null && Camera.main.GetComponent<CinemachineBrain>() == null)
            Camera.main.gameObject.AddComponent<CinemachineBrain>();

        // Create a Cinemachine Virtual Camera
        GameObject vcamObj = new GameObject("Runtime Virtual Camera");
        vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();

        // Add Composer to look at player
        var composer = vcam.AddCinemachineComponent<CinemachineComposer>();
        composer.m_HorizontalDamping = 0;
        composer.m_VerticalDamping = 0;

        defaultDistance = distance;

    }

    private void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
            if (player != null)
            {
                vcam.Follow = player;
                vcam.LookAt = player;
            }
            return;
        }

        HandleOrbitInput();
        HandleZoomInput();
        HandleResetCamera();
        UpdateCameraPosition();
    }

    private void HandleOrbitInput()
    {
        if (Input.GetMouseButton(1)) // Right click held
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentYaw += mouseX * orbitSpeed;
        }
    }

    private void HandleResetCamera()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentYaw = 0f;
            distance = defaultDistance;
        }
    }

    private void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    private void UpdateCameraPosition()
    {
        if (player == null) return;

        // Fixed position above and slightly behind player
        Vector3 offset = Quaternion.Euler(0, currentYaw, 0) * new Vector3(0, height, -distance);
        vcam.transform.position = player.position + offset;

        // Look at player
        vcam.transform.LookAt(player.position + Vector3.up * height);
    }
}

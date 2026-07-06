using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public bool isFollowing = true;

    [Header("Bounds")]
    public BoxCollider2D bounds;

    private Vector3 _min;
    private Vector3 _max;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();

        if (bounds != null)
        {
            _min = bounds.bounds.min;
            _max = bounds.bounds.max;
        }
        else
        {
            Debug.LogWarning("CameraMovement: Bounds chưa được gán trong Inspector!");
        }

        // Snap camera to player at start
        if (player != null)
        {
            transform.position = new Vector3(
                player.position.x,
                player.position.y,
                transform.position.z
            );
        }
        else
        {
            Debug.LogWarning("CameraMovement: Player chưa được gán trong Inspector!");
        }
    }

    private void LateUpdate()
    {
        if (player == null || !isFollowing) return;

        // Start from player position, not camera position
        float x = player.position.x;
        float y = player.position.y;

        // Only clamp if bounds is assigned
        if (bounds != null)
        {
            // Calculate camera half dimensions
            float cameraHalfHeight = cam.orthographicSize;
            float cameraHalfWidth = cameraHalfHeight * ((float)Screen.width / Screen.height);

            // Clamp so camera never leaves bounds
            x = Mathf.Clamp(x, _min.x + cameraHalfWidth, _max.x - cameraHalfWidth);
            y = Mathf.Clamp(y, _min.y + cameraHalfHeight, _max.y - cameraHalfHeight);
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
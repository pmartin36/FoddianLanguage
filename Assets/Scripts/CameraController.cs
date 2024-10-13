using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Camera camera;
    float xRotation = 0;

    void Start()
    {
        camera = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UpdateRotation(float x)
    {
        SetRotation(Mathf.Clamp(xRotation - x, -90f, 90f));
    }

    public void SetRotation(float x)
    {
        xRotation = x;
        this.transform.localRotation = Quaternion.Euler(xRotation, 0, 0); 
    }

	public void Flip()
    {
        // Keep same rotation as before going through the portal
        SetRotation(-xRotation);
    }
}

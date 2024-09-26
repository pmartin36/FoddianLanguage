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

    public void UpdateRotation(float v)
    {
        Vector3 r = this.transform.rotation.eulerAngles;
        xRotation = Mathf.Clamp(xRotation - v, -90f, 90f);
		this.transform.rotation = Quaternion.Euler(xRotation, r.y, r.z);
    }
}

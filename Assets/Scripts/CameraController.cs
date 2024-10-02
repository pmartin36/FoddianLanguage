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
        Vector3 r = this.transform.rotation.eulerAngles;
        xRotation = Mathf.Clamp(xRotation - x, -90f, 90f);
		this.transform.rotation = Quaternion.Euler(xRotation, r.y, r.z);
    }

    public void Flip()
    {
		Vector3 r = this.transform.rotation.eulerAngles;
		this.transform.rotation = Quaternion.Euler(xRotation, r.y, r.z + 180);
        //UpdateRotation(-xRotation);
    }
}

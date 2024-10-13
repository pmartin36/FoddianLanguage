using UnityEngine;
using UnityEngine.InputSystem.XR;

public class Torch : MonoBehaviour
{
    private Light light;

    [Header("Light properties")]
    private float baseIntensity;
    [SerializeField] private AnimationCurve flickerCurve;
    [SerializeField][Range(0f, 2f)] private float flickerTimeScale = 0.5f;
    [SerializeField][Range(0f, 2f)] private float flickerPower = 1;

    [Header("Transform properties")]
    private float xAngle;
    private Vector3 basePosition;
    private float transformTime;
    [SerializeField] private float positionMagnitude = 0.2f;
    [SerializeField] private float positionSpeed = 0.05f;

    public bool IsExtended { get; private set; }
    public bool IsFullyExtended { get => xAngle > 59.5f; }

    void Start()
    {
        light = GetComponentInChildren<Light>();
        baseIntensity = light.intensity;
        basePosition = this.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        float t = Time.time * flickerTimeScale;
        light.intensity = baseIntensity * (1 + flickerPower * flickerCurve.Evaluate(Mathf.Ceil(t) - t));
    }

    public void PlayerUpdate(bool extending, Vector3 movement)
    {
		transformTime += Time.deltaTime * (1 + movement.sqrMagnitude * positionSpeed);

		IsExtended = extending;
        var target = extending ? 60f : 0f;
        if (Mathf.Abs(xAngle - target) > 0.1f)
        {
			var dir = Mathf.Sign(target - xAngle);
			xAngle += dir * Time.deltaTime * 600f;
			xAngle = Mathf.Clamp(xAngle, 0f, 60f);

			baseIntensity = 3 + (2 * xAngle / 60f);
		}
		transform.localEulerAngles = new Vector3(
		    xAngle + positionMagnitude * Mathf.Sin(transformTime),
		    0,
		    positionMagnitude * 2 * Mathf.Cos(transformTime * 0.13f)
	    );
	}
}

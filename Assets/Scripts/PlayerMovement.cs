using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Sensitivity")]
	[SerializeField][Range(1, 20)] private float xSensitivity = 10f;
	[SerializeField][Range(1, 20)] private float ySensitivity = 10f;
    

	private Rigidbody rigidbody;
    private CapsuleCollider capsuleCollider;

    private CameraController cameraController;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
	private InputAction sprintAction;
	private InputAction crouchAction;

    [Header("Physics")]
    [SerializeField][Range(1f, 20f)] private float sprintSpeed = 8f;
	[SerializeField][Range(1f, 20f)] private float walkSpeed = 6f;
	[SerializeField][Range(1f, 20f)] private float crouchedSpeed = 4f;
	[SerializeField][Range(1, 10)] private float jumpStrength = 5f;
    [SerializeField] private float groundJumpBuffer = 0.1f;
    [SerializeField] private AnimationCurve accelerationCurve;
    [SerializeField] private LayerMask groundMask;
	private bool isGrounded;

    private Vector3 lastMove;
    private Vector3 lastNonzeroMoveDirection;
    private bool frontCastResult;
    private bool backCastResult;
	private bool leftCastResult;
	private bool rightCastResult;

	private float bufferedJumpPressTime = -1f;
    private float leftGroundTime = -1f;

    private bool canJump
    {
        get
        {
            return isGrounded || (Time.time - leftGroundTime < groundJumpBuffer);
        }
    }

	void Start()
    {
        cameraController = GetComponentInChildren<CameraController>();
        rigidbody = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
		jumpAction = InputSystem.actions.FindAction("Jump");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        sprintAction = InputSystem.actions.FindAction("Sprint");

        lastNonzeroMoveDirection = Vector3.forward;
	}

    // Update is called once per frame
    void Update()
    {
        Vector2 lookValue = lookAction.ReadValue<Vector2>();
        this.transform.rotation *= Quaternion.Euler(0, lookValue.x * xSensitivity * Time.deltaTime, 0);
        this.cameraController.UpdateRotation(lookValue.y * ySensitivity * Time.deltaTime);

        Vector2 moveValue = moveAction.ReadValue<Vector2>() * Time.deltaTime * 100f;
        if (sprintAction.IsPressed())
        {
            moveValue *= sprintSpeed;
        }
        else if (crouchAction.IsPressed())
        {
            moveValue *= crouchedSpeed;
        }
        else
        {
            moveValue *= walkSpeed;
        }

        Vector3 xz = this.transform.forward * moveValue.y + this.transform.right * moveValue.x;
        if (xz.sqrMagnitude > 0.1f)
        {
            lastNonzeroMoveDirection = xz;
        }

		lastMove = new Vector3(xz.x, rigidbody.linearVelocity.y, xz.z);
        rigidbody.linearVelocity = lastMove;


		if (jumpAction.WasPressedThisFrame())
        {
            if (canJump)
            {
                Jump();
            }
            else
            {
                bufferedJumpPressTime = Time.time;
            }
		}
    }

	private void FixedUpdate()
	{
		bool wasGrounded = isGrounded;
        getRaycastResults();
		if (!isGrounded)
        {
			var lv = rigidbody.linearVelocity;
            if (lv.y > -50f)
            {
				rigidbody.AddForce(Vector3.down * accelerationCurve.Evaluate(Time.time - leftGroundTime), ForceMode.Acceleration);
            }

            if (frontCastResult ^ backCastResult ^ leftCastResult ^ rightCastResult)
            {
				rigidbody.AddForce(Vector3.down * 20f, ForceMode.Acceleration);
			}

            if (wasGrounded)
            {
				leftGroundTime = Time.time;
            }
        }
        else if (!wasGrounded && Time.time - bufferedJumpPressTime < 0.2f)
        {
            Jump();
        }
	}

    private void Jump()
    {
        var lv = rigidbody.linearVelocity;
		rigidbody.linearVelocity = new Vector3(lv.x, 0, lv.z);
		rigidbody.AddForce(Vector3.up * jumpStrength, ForceMode.VelocityChange);
	}

    private void getRaycastResults()
    {
		var ray = Vector3.down * this.transform.lossyScale.y * capsuleCollider.height * 0.51f;
		isGrounded = Physics.Linecast(this.transform.position, this.transform.position + ray, groundMask);

		var frontBackOffset = new Vector3(lastNonzeroMoveDirection.x, 0, lastNonzeroMoveDirection.z).normalized * this.transform.lossyScale.x * capsuleCollider.radius * 0.95f;
        var leftRightOffset = Quaternion.Euler(0, 90, 0) * frontBackOffset;
		frontCastResult =   Physics.Linecast(this.transform.position + frontBackOffset, this.transform.position + frontBackOffset + ray, groundMask);
		backCastResult =    Physics.Linecast(this.transform.position - frontBackOffset, this.transform.position - frontBackOffset + ray, groundMask);
		rightCastResult =    Physics.Linecast(this.transform.position + leftRightOffset, this.transform.position + leftRightOffset + ray, groundMask);
		leftCastResult =   Physics.Linecast(this.transform.position - leftRightOffset, this.transform.position - leftRightOffset + ray, groundMask);

#if UNITY_EDITOR
        Debug.DrawLine(transform.position, this.transform.position + ray, isGrounded ? Color.green : Color.red, 0.1f);
		Debug.DrawLine(this.transform.position + frontBackOffset, this.transform.position + frontBackOffset + ray, frontCastResult ? Color.green : Color.red, 0.1f);
		Debug.DrawLine(this.transform.position - frontBackOffset, this.transform.position - frontBackOffset + ray, backCastResult ? Color.green : Color.red, 0.1f);
		Debug.DrawLine(this.transform.position + leftRightOffset, this.transform.position + leftRightOffset + ray, rightCastResult ? Color.green : Color.red, 0.1f);
		Debug.DrawLine(this.transform.position - leftRightOffset, this.transform.position - leftRightOffset + ray, leftCastResult ? Color.green : Color.red, 0.1f);
#endif
	}
}

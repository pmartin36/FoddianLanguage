using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	private CharacterController controller;
	private CapsuleCollider capsuleCollider;
	private CameraController cameraController;

	private InputAction moveAction;
	private InputAction lookAction;
	private InputAction jumpAction;
	private InputAction sprintAction;
	private InputAction crouchAction;

	[Header("Look Sensitivity")]
	[SerializeField][Range(1, 20)] private float xSensitivity = 10f;
	[SerializeField][Range(1, 20)] private float ySensitivity = 10f;
    
    [Header("Physics")]
    [SerializeField][Range(1f, 20f)] private float sprintSpeed = 8f;
	[SerializeField][Range(1f, 20f)] private float walkSpeed = 6f;
	[SerializeField][Range(1f, 20f)] private float crouchedSpeed = 4f;

    [SerializeField][Tooltip("how much time after player leaves ground can they press jump and have it work")]
    private float groundJumpBuffer = 0.1f; 
	[SerializeField][Tooltip("how much time before player lands can they press jump and have it work")]
    private float airJumpBuffer = 0.2f;  

	[SerializeField] private AnimationCurve jumpAccelerationCurve;
    [SerializeField] private AnimationCurve fallAccelerationCurve;
    [SerializeField] private LayerMask groundMask;

    // Current Position State
	private bool isGrounded;
    private Vector3 velocity;

    // Current Jump State
	private float bufferedJumpPressTime = -1f;
    private float leftGroundTime = -1f;
    private bool leftGroundViaJump = false;
    private bool jumpHeld = false;

    // Unused for now
	private Vector3 lastNonzeroMoveDirection;
	private bool frontCastResult;
	private bool backCastResult;
	private bool leftCastResult;
	private bool rightCastResult;

	private bool canJump
    {
        get
        {
            return isGrounded || ((Time.time - leftGroundTime < groundJumpBuffer) && !leftGroundViaJump);
        }
    }

	void Start()
    {
        cameraController = GetComponentInChildren<CameraController>();
        controller = GetComponent<CharacterController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
		jumpAction = InputSystem.actions.FindAction("Jump");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        sprintAction = InputSystem.actions.FindAction("Sprint");

        lastNonzeroMoveDirection = Vector3.forward;
	}

	private void Update()
	{
		
	}

	// Update is called once per frame
	void FixedUpdate()
    {
        Vector2 lookValue = lookAction.ReadValue<Vector2>();
        this.transform.rotation *= Quaternion.Euler(0, lookValue.x * xSensitivity * Time.fixedDeltaTime, 0);
        this.cameraController.UpdateRotation(lookValue.y * ySensitivity * Time.fixedDeltaTime);

        Vector2 moveValue = moveAction.ReadValue<Vector2>() * Time.fixedDeltaTime;
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

		bool wasGrounded = isGrounded;
        GetRaycastResults();
        if (!isGrounded)
        {
            if (wasGrounded)
            {
				leftGroundTime = Time.time - Time.fixedDeltaTime;
			}
			ApplyVerticalVelocity();
        }
        else {
            velocity.y = 0;
            if (!wasGrounded)
			{
				leftGroundTime = -1f;
                leftGroundViaJump = false;
				if (Time.time - bufferedJumpPressTime < airJumpBuffer) {
				    StartJump();
                }
			}
		}

        var jumpPressed = jumpAction.IsPressed();
		if (jumpPressed && !jumpHeld)
        {
            if (canJump)
            {
				StartJump();
            }
            else
            {
                bufferedJumpPressTime = Time.time;
            }
		}
        jumpHeld = jumpPressed;
		velocity = new Vector3(xz.x, velocity.y, xz.z);
		controller.Move(velocity);
	}

    private void StartJump() {
        leftGroundViaJump = true;
        leftGroundTime = Time.time;
        velocity.y = jumpAccelerationCurve.Evaluate(0) * Time.fixedDeltaTime; //initial boost
	}
    private void ApplyVerticalVelocity()
    {
        float jTime = Mathf.Clamp01(Time.time - leftGroundTime);
        float v = 0f;
        if (leftGroundViaJump && jumpHeld)
        {
            v = jumpAccelerationCurve.Evaluate(jTime);
		}
        else
        {
            v = fallAccelerationCurve.Evaluate(jTime);
		}
        velocity.y += v * Time.fixedDeltaTime;
	}

    private void GetRaycastResults()
    {

        var radius = this.transform.lossyScale.x * controller.radius;
		isGrounded = Physics.SphereCast(this.transform.position,
            radius,
            Vector3.down,
            out RaycastHit rayInfo,
			this.transform.lossyScale.y * (controller.height * 0.5f + controller.skinWidth + 0.01f) - radius,
            groundMask);

		// var ray = Vector3.down * this.transform.lossyScale.y * (controller.height * 0.5f + controller.skinWidth + 0.01f);
        // isGrounded = Physics.Linecast(this.transform.position, this.transform.position + ray, groundMask);
		//var frontBackOffset = new Vector3(lastNonzeroMoveDirection.x, 0, lastNonzeroMoveDirection.z).normalized * this.transform.lossyScale.x * controller.radius * 0.95f;
        //var leftRightOffset = Quaternion.Euler(0, 90, 0) * frontBackOffset;
		//frontCastResult = Physics.Linecast(this.transform.position + frontBackOffset, this.transform.position + frontBackOffset + ray, groundMask);
		//backCastResult = Physics.Linecast(this.transform.position - frontBackOffset, this.transform.position - frontBackOffset + ray, groundMask);
		//rightCastResult = Physics.Linecast(this.transform.position + leftRightOffset, this.transform.position + leftRightOffset + ray, groundMask);
		//leftCastResult = Physics.Linecast(this.transform.position - leftRightOffset, this.transform.position - leftRightOffset + ray, groundMask);

#if UNITY_EDITOR
  //      Debug.DrawLine(transform.position, this.transform.position + ray, isGrounded ? Color.green : Color.red, 0.1f);
		//Debug.DrawLine(this.transform.position + frontBackOffset, this.transform.position + frontBackOffset + ray, frontCastResult ? Color.green : Color.red, 0.1f);
		//Debug.DrawLine(this.transform.position - frontBackOffset, this.transform.position - frontBackOffset + ray, backCastResult ? Color.green : Color.red, 0.1f);
		//Debug.DrawLine(this.transform.position + leftRightOffset, this.transform.position + leftRightOffset + ray, rightCastResult ? Color.green : Color.red, 0.1f);
		//Debug.DrawLine(this.transform.position - leftRightOffset, this.transform.position - leftRightOffset + ray, leftCastResult ? Color.green : Color.red, 0.1f);
#endif
	}
}

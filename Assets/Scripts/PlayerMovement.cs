using System.Collections;
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
    [SerializeField][Range(0.1f, 2f)][Tooltip("Maximum velocity in any single direction")] private float maxDirectionalVelocity = 0.8f;

    [SerializeField][Tooltip("how much time after player leaves ground can they press jump and have it work")]
    private float groundJumpBuffer = 0.1f; 
	[SerializeField][Tooltip("how much time before player lands can they press jump and have it work")]
    private float airJumpBuffer = 0.2f;  

	[SerializeField] private AnimationCurve jumpAccelerationCurve;
    [SerializeField] private AnimationCurve fallAccelerationCurve;
    [SerializeField] private LayerMask groundMask;

    // Current Position State
	private bool isGrounded;
    private Vector3 _velocity;
    public Vector3 velocity { get => _velocity; }

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

    // Gravity
    private Quaternion gravityRotation = Quaternion.identity;
    private Vector3 gravityDirection = Vector3.down;
    private float gravityFlipTime = -1f;

    // Crouch
    private bool isCrouched;
    private bool isAnimatingCrouch;

    // Misc Getters
    private float standingRaycastHeight;
    public float Height { get => this.transform.lossyScale.y * (controller.height * 0.5f + controller.skinWidth + 0.01f); }
    public float Radius { get => this.transform.lossyScale.x * controller.radius; }

	private bool canJump
    {
        get => isGrounded || ((Time.time - leftGroundTime < groundJumpBuffer) && !leftGroundViaJump);
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
        standingRaycastHeight = Height;
	}

	private void Update()
	{
		
	}

	// Update is called once per frame
	void FixedUpdate()
    {
        Vector2 lookValue = lookAction.ReadValue<Vector2>();
        this.transform.rotation *= Quaternion.Euler(0, lookValue.x * xSensitivity * Time.fixedDeltaTime, 0);

        var camRotation = lookValue.y * ySensitivity * Time.fixedDeltaTime;
		this.cameraController.UpdateRotation(camRotation);

        // check crouched
        if (isCrouched)
        {
            // try uncrouch
            if (!crouchAction.IsPressed())
            {
                TryUncrouch();
            }
        }
        else if (!isCrouched && crouchAction.IsPressed())
        {
            SetCrouched();
        }


        Vector2 moveValue = moveAction.ReadValue<Vector2>() * Time.fixedDeltaTime;
        if (isCrouched)
        {
			moveValue *= crouchedSpeed;
		}
		else if (sprintAction.IsPressed())
        {
            moveValue *= sprintSpeed;
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
            _velocity.y = 0;
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
            Debug.Log("canjump" + canJump);
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
		_velocity = new Vector3(xz.x, _velocity.y, xz.z);
		controller.Move(_velocity);
	}

    private void StartJump() {
        leftGroundViaJump = true;
        leftGroundTime = Time.time;

        // clear velocity in gravity direction
        _velocity -= Vector3.Project(velocity, gravityDirection);

        // add initial jump velocity
        _velocity -= gravityDirection * jumpAccelerationCurve.Evaluate(0) * Time.fixedDeltaTime; //initial boost
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
        var tempv = _velocity - gravityDirection * v * Time.fixedDeltaTime;
        _velocity = new Vector3(
            Mathf.Clamp(tempv.x, -maxDirectionalVelocity, maxDirectionalVelocity),
            Mathf.Clamp(tempv.y, -maxDirectionalVelocity, maxDirectionalVelocity),
            Mathf.Clamp(tempv.z, -maxDirectionalVelocity, maxDirectionalVelocity)
        );
		Debug.Log(velocity.y);
	}

    private void GetRaycastResults()
    {
		isGrounded = Physics.SphereCast(this.transform.position,
            Radius,
            gravityDirection,
            out RaycastHit rayInfo,
			Height - Radius,
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

    public bool canInvertGravity()
    {
        return !isGrounded && Vector3.Dot(velocity, gravityDirection) > 0.2f && Time.time - gravityFlipTime > 2f;
    }

    public void UpdateGravityDirection(Vector3 dir)
    {
        Debug.Log("Updating gravity to: " + dir.ToString());
        var oldGrav = gravityRotation;
        gravityRotation = Quaternion.FromToRotation(Vector3.down, dir);
        gravityDirection = Vector3.Normalize(dir);
        gravityFlipTime = Time.time;

        this.transform.rotation *= (gravityRotation * Quaternion.Inverse(oldGrav))
			 * Quaternion.Euler(0, 180, 0); // workaround - only works if up/down are the only directions we flip
        this.cameraController.Flip();
    }

    private void SetCrouched()
    {
		isCrouched = true;
		if (!isAnimatingCrouch)
		{
			StartCoroutine(TransitionCrouch());
		}
	}

    private bool TryUncrouch()
    {
		var hit = Physics.SphereCast(this.transform.position,
			Radius,
			-gravityDirection,
			out RaycastHit rayInfo,
			standingRaycastHeight - Radius + 1f, // 1f is extra buffer required - will sort with design
			groundMask);
        if (hit)
        {
            return false;
        }
        isCrouched = false;
        if (!isAnimatingCrouch)
        {
            StartCoroutine(TransitionCrouch());
        }
        return true;
	}

    IEnumerator TransitionCrouch()
    {
        isAnimatingCrouch = true;
        while (isAnimatingCrouch)
        {
            var target = isCrouched ? 1f : 2f;
            var dir = Mathf.Sign(target - controller.height);
            var startingHeight = controller.height;
            Debug.Log($"target: {target}, dir: {dir}");
            controller.height += dir * Time.deltaTime * 10f;
            if (dir > 0 && controller.height >= target || dir < 0 && controller.height <= target)
            {
                isAnimatingCrouch = false;
                controller.height = Mathf.Clamp(controller.height, 1f, 2f);
            }
            controller.Move(gravityDirection * (startingHeight - controller.height) * 0.5f);
            yield return null;
        }
    }
}

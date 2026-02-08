using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;
    public Camera playerCamera;
    public Transform visualCapsule;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float minPitch = -35f;
    public float maxPitch = 70f;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float acceleration = 12f;
    public float rotationSpeed = 12f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float gravity = -20f;
    public int maxAirJumps = 1;

    [Header("Burrow")]
    public float burrowSpeed = 10f;
    public float burrowSink = 1.5f;
    public float burrowTime = 1f;
    public float launchForce = 14f;
    public float burrowVisualSpeed = 3f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveDirection;
    private float currentSpeed;
    private int airJumpCount;
    private float jumpBufferTime = 0.15f;
    private float coyoteTime = 0.15f;
    private float jumpBufferCounter;
    private float coyoteCounter;

    private float yaw;
    private float pitch;

    private bool touchingFeathers;
    private bool isBurrowing;
    private bool readyToLaunch;
    private Collider currentBurrowCollider;

    private float originalVisualY;

    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsFalling { get; private set; }
    public bool IsBurrowing { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        originalVisualY = visualCapsule.localPosition.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleGround();
        HandleInput();
        HandleMovement();
        HandleGravity();
        HandleBurrowMovement();
        ApplyMovement();
        HandleVisualCapsule();
    }

    private void HandleMouseLook()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        if (input.magnitude > 0.1f)
        {
            Vector3 forward = cameraPivot.forward;
            Vector3 right = cameraPivot.right;
            forward.y = 0;
            right.y = 0;
            moveDirection = (forward.normalized * input.z + right.normalized * input.x).normalized;
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        IsRunning = Input.GetKey(KeyCode.LeftShift);

        if (touchingFeathers && Input.GetKeyDown(KeyCode.E))
        {
            if (!isBurrowing)
                StartCoroutine(BurrowRoutine());
            else if (readyToLaunch)
                LaunchFromBurrow();
        }
    }

    private void HandleGround()
    {
        IsGrounded = controller.isGrounded;

        if (IsGrounded)
        {
            coyoteCounter = coyoteTime;
            airJumpCount = 0;
            if (velocity.y < 0)
                velocity.y = -2f;
        }
        else
            coyoteCounter -= Time.deltaTime;
    }

    private void HandleMovement()
    {
        if (isBurrowing) return;

        float targetSpeed = IsRunning ? sprintSpeed : walkSpeed;
        targetSpeed *= moveDirection.magnitude;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        if (moveDirection.magnitude > 0.1f)
        {
            Vector3 flatDir = new Vector3(moveDirection.x, 0, moveDirection.z);
            if (Vector3.Dot(flatDir, transform.forward) > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        if (jumpBufferCounter > 0)
        {
            if (coyoteCounter > 0)
                Jump();
            else if (airJumpCount < maxAirJumps)
                AirJump();
        }
    }

    private void Jump()
    {
        velocity.y = jumpForce;
        jumpBufferCounter = 0;
        coyoteCounter = 0;
        IsJumping = true;
    }

    private void AirJump()
    {
        velocity.y = jumpForce * 0.9f;
        airJumpCount++;
        jumpBufferCounter = 0;
        IsJumping = true;
    }

    private void HandleGravity()
    {
        if (isBurrowing) return;
        velocity.y += gravity * Time.deltaTime;
        IsFalling = velocity.y < -1f && !IsGrounded;
    }

    private void HandleBurrowMovement()
    {
        if (!isBurrowing || currentBurrowCollider == null) return;
        
        Vector3 burrowMove = moveDirection * burrowSpeed * Time.deltaTime;
        
        Vector3 nextPos = transform.position + burrowMove;
        Vector3 closest = currentBurrowCollider.ClosestPoint(nextPos);
        Vector3 delta = closest - transform.position;

        controller.Move(delta);
    }

    private void HandleVisualCapsule()
    {
        if (!visualCapsule) return;

        float targetY = isBurrowing ? originalVisualY - burrowSink : originalVisualY;
        Vector3 localPos = visualCapsule.localPosition;
        localPos.y = Mathf.Lerp(localPos.y, targetY, Time.deltaTime * burrowVisualSpeed);
        visualCapsule.localPosition = localPos;
    }

    private IEnumerator BurrowRoutine()
    {
        isBurrowing = true;
        readyToLaunch = false;
        velocity = Vector3.zero;
        yield return new WaitForSeconds(burrowTime);
        readyToLaunch = true;
    }

    private void LaunchFromBurrow()
    {
        isBurrowing = false;
        readyToLaunch = false;
        velocity.y = launchForce;
        IsJumping = true;
        currentBurrowCollider = null;
    }

    private void ApplyMovement()
    {
        if (isBurrowing) return;
        Vector3 move = moveDirection * currentSpeed + Vector3.up * velocity.y;
        controller.Move(move * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("feathers"))
        {
            touchingFeathers = true;
            currentBurrowCollider = hit.collider;
        }
        else if (currentBurrowCollider == hit.collider)
        {
            touchingFeathers = false;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerMovement : MonoBehaviour
{
    private InputSystem_Actions input;
    private CharacterController controller;

    [Header("移动")]
    public float speed = 8f;          // 最大水平速度
    public float acceleration = 60f;  // 加速
    public float deceleration = 80f;  // 松手后的减速

    [Header("跳跃与重力")]
    public float gravity = -19.62f;   // 约 2 倍重力
    public float jumpHeight = 2f;
    public float coyoteTime = 0.12f;  // 离地后仍可起跳的缓冲时间(手感)

    [Header("地面检测")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Vector3 horizontalVelocity; // 水平速度(带平滑加减速)
    private float verticalVelocity;     // 垂直速度(y 分量)
    private float coyoteTimer;

    public bool IsGrounded { get; private set; }
    public bool IsMoving { get; private set; }
    public Vector3 HorizontalVelocity => horizontalVelocity;

    void Awake()
    {
        input = new InputSystem_Actions();
    }

    void OnEnable()  => input.Player.Enable();
    void OnDisable() => input.Player.Disable();

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        UpdateGrounded();
        HandleJump();
        HandleMove();
        ApplyMovement();
    }

    private void UpdateGrounded()
    {
        IsGrounded = groundCheck != null
            ? Physics.CheckSphere(groundCheck.position, groundDistance, groundMask)
            : controller.isGrounded;

        if (IsGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // 贴地时施加一个向下的力保持接触

        // 离地缓冲计时
        if (IsGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;
    }

    private void HandleJump()
    {
        if (input.Player.Jump.triggered && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimer = 0f; // 防止空中连跳
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleMove()
    {
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();

        // 用水平面上的朝向，避免视角俯仰影响移动方向
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 move = right * moveInput.x + forward * moveInput.y;

        Vector3 target = move.normalized * speed;
        float rate = target.sqrMagnitude > 0.01f ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, rate * Time.deltaTime);

        IsMoving = horizontalVelocity.sqrMagnitude > 0.01f;
    }

    private void ApplyMovement()
    {
        Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }
}

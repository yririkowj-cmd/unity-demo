using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 相机装置（Camera Rig）。
///
/// 职责边界：
///  - 掌管相机的 yaw / pitch，支持第一/第三人称切换
///  - 按住自由视角键时，相机 yaw 与身体解耦（镜头转、身体不转）
///  - 驱动身体 yaw：非自由视角时转向镜头方向；自由视角时保持
///
/// 与移动的契约：<see cref="PlayerMovement"/> 读取 <see cref="CameraYaw"/> 决定移动方向，
/// 保证"往哪看就往哪走"（自由视角下也成立）。
///
/// 旧的 MouseMovement 已由本类取代（脚本 GUID 保持不变，场景引用不会丢）。
/// </summary>
[DefaultExecutionOrder(-10)] // 先于 PlayerMovement 更新，保证本帧 yaw 是最新的
public class CameraRig : MonoBehaviour
{
    public enum ViewMode { FirstPerson, ThirdPerson }

    [Header("灵敏度")]
    [Tooltip("鼠标灵敏度（度/秒，内部已乘 Time.deltaTime）")]
    public float mouseSensitivity = 100f;
    public bool invertY = false;

    [Header("俯仰限制(上下看的角度)")]
    public float topClamp = -80f;
    public float bottomClamp = 80f;

    [Header("相机位置")]
    public float eyeHeight = 1.7f;        // 第一人称眼睛高度
    public float shoulderHeight = 1.5f;   // 第三人称肩部(俯仰枢纽)高度
    public float shoulderDistance = 2.5f; // 第三人称相机到肩部的距离

    [Header("视角切换")]
    public Key toggleKey = Key.V;
    public bool firstPerson = false;      // false = 第三人称

    [Header("自由视角(按住)")]
    public Key freeLookKey = Key.LeftAlt;
    [Tooltip("松手后镜头是否回正到角色背后；关闭则保持当前角度，角色转身面向镜头")]
    public bool returnOnRelease = false;
    [Tooltip("身体转向镜头的速度(越大越快)")]
    public float bodyTurnSpeed = 12f;

    [Header("模型")]
    [Tooltip("第一人称时隐藏、第三人称时显示的模型根节点")]
    public Transform modelRoot;

    private Transform cameraHolder; // 俯仰枢纽(运行时自动创建)
    private Camera playerCamera;
    private float cameraYaw;         // 相机水平角(自由)
    private float pitch;             // 俯仰角，只影响相机
    private float bodyYaw;           // 身体水平角
    private bool returning;          // 回正过程中

    public float CameraYaw => cameraYaw;
    public float BodyYaw => bodyYaw;
    public bool FreeLooking { get; private set; }
    public ViewMode Mode => firstPerson ? ViewMode.FirstPerson : ViewMode.ThirdPerson;
    public Transform CameraTransform => playerCamera != null ? playerCamera.transform : null;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("CameraRig: 找不到子物体 Camera。", this);
            enabled = false;
            return;
        }

        BuildCameraHolder();

        bodyYaw = transform.eulerAngles.y;
        cameraYaw = bodyYaw;
        pitch = 0f;

        if (modelRoot == null)
            modelRoot = transform.Find("ModelRoot") ?? transform.Find("Body");

        ApplyCameraMode(instant: true);
    }

    /// <summary>在玩家和相机之间插入一个俯仰枢纽，让 pitch 只影响相机、不影响身体。</summary>
    private void BuildCameraHolder()
    {
        cameraHolder = transform.Find("CameraHolder");
        if (cameraHolder == null)
        {
            cameraHolder = new GameObject("CameraHolder").transform;
            cameraHolder.SetParent(transform, false);
        }

        // 把枢纽放到相机当前世界位置，再挂相机，保证重建瞬间画面不跳
        cameraHolder.position = playerCamera.transform.position;
        cameraHolder.rotation = transform.rotation;

        playerCamera.transform.SetParent(cameraHolder, true);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;
        playerCamera.transform.localScale = Vector3.one;
    }

    void Update()
    {
        // 1) 第一/第三人称切换
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            ToggleView();

        // 2) 自由视角按住检测(第一人称下无意义，直接禁用)
        bool wantFreeLook = !firstPerson && Keyboard.current != null && Keyboard.current[freeLookKey].isPressed;
        if (wantFreeLook != FreeLooking)
        {
            FreeLooking = wantFreeLook;
            returning = !wantFreeLook && returnOnRelease;
        }

        // 3) 鼠标驱动相机(自由视角/回正期间也允许继续看)
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            cameraYaw += delta.x * mouseSensitivity * Time.deltaTime;
            pitch -= delta.y * mouseSensitivity * Time.deltaTime * (invertY ? -1f : 1f);
            pitch = Mathf.Clamp(pitch, topClamp, bottomClamp);
        }

        // 4) 松手回正：相机追回身体朝向
        if (returning)
        {
            cameraYaw = Mathf.LerpAngle(cameraYaw, bodyYaw, 1f - Mathf.Exp(-bodyTurnSpeed * Time.deltaTime));
            if (Mathf.Abs(Mathf.DeltaAngle(cameraYaw, bodyYaw)) < 0.5f)
            {
                cameraYaw = bodyYaw;
                returning = false;
            }
        }

        // 5) 身体朝向
        if (firstPerson)
        {
            bodyYaw = cameraYaw; // 第一人称：身体必须跟视线一致
        }
        else if (!FreeLooking && !returning)
        {
            // 第三人称常规：身体平滑转向镜头；自由视角/回正期间保持不动
            bodyYaw = Mathf.LerpAngle(bodyYaw, cameraYaw, 1f - Mathf.Exp(-bodyTurnSpeed * Time.deltaTime));
        }

        transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);
        if (cameraHolder != null)
        {
            // 相机世界 yaw 始终等于 cameraYaw：本地 yaw 用差值抵消身体旋转
            cameraHolder.localRotation = Quaternion.Euler(pitch, cameraYaw - bodyYaw, 0f);
        }
    }

    void LateUpdate()
    {
        // 相机位置插值放在 LateUpdate，保证在 Update 旋转之后应用
        ApplyCameraMode(instant: false);
    }

    /// <summary>切换第一/第三人称。</summary>
    public void ToggleView()
    {
        firstPerson = !firstPerson;
        if (firstPerson) FreeLooking = false;
    }

    private void ApplyCameraMode(bool instant)
    {
        if (cameraHolder == null || playerCamera == null) return;

        Vector3 holderPos = new Vector3(0f, firstPerson ? eyeHeight : shoulderHeight, 0f);
        float camZ = firstPerson ? 0f : -shoulderDistance;

        float t = instant ? 1f : 1f - Mathf.Exp(-10f * Time.deltaTime);

        cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, holderPos, t);
        playerCamera.transform.localPosition =
            Vector3.Lerp(playerCamera.transform.localPosition, new Vector3(0f, 0f, camZ), t);

        // 第一人称隐藏模型，避免遮挡镜头
        if (modelRoot != null)
            modelRoot.gameObject.SetActive(!firstPerson);
    }
}

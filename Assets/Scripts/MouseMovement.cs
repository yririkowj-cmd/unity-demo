using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 视角控制 + 第一/第三人称切换。
/// - 身体(本物体)只做水平旋转(yaw)
/// - 相机挂在俯仰枢纽 CameraHolder 下，只做上下旋转(pitch)
/// - 按切换键在第一人称(相机在眼睛、隐藏模型)与第三人称(相机在肩后)之间切换
/// </summary>
public class MouseMovement : MonoBehaviour
{
    [Header("灵敏度")]
    public float mouseSensitivity = 100f;
    public bool invertY = false;

    [Header("俯仰限制(上下看的角度)")]
    public float topClamp = -80f;
    public float bottomClamp = 80f;

    [Header("相机位置")]
    public float eyeHeight = 1.7f;        // 第一人称眼睛高度
    public float shoulderHeight = 1.5f;   // 第三人称肩部(俯仰枢纽)高度
    public float shoulderDistance = 2.5f; // 第三人称相机到肩部的距离

    [Header("切换")]
    public Key toggleKey = Key.V;
    public bool firstPerson = false;      // 初始视角(false = 第三人称)

    [Header("可视模型(第一人称时隐藏，二游角色模型挂这里)")]
    public Transform modelRoot;

    private Transform cameraHolder; // 俯仰枢纽(运行时自动创建)
    private Camera playerCamera;
    private float xRotation;        // 俯仰角
    private float yRotation;        // 水平角

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("MouseMovement: 找不到子物体 Camera。", this);
            enabled = false;
            return;
        }

        BuildCameraHolder();

        yRotation = transform.eulerAngles.y; // 以当前身体朝向为初始水平角
        xRotation = 0f;

        // 约定：模型放在名为 ModelRoot 或 Body 的子物体下
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
        // 切换视角
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            ToggleView();

        // 鼠标输入
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            float mx = delta.x * mouseSensitivity * Time.deltaTime;
            float my = delta.y * mouseSensitivity * Time.deltaTime * (invertY ? -1f : 1f);

            yRotation += mx;
            xRotation -= my;
            xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);
        }

        // 身体只水平旋转，相机枢纽只俯仰
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
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

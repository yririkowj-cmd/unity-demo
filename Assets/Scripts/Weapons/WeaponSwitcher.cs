using UnityEngine;

/// <summary>
/// 武器切换：持有多把武器(每个武器是一个带 WeaponController 的物体)，用滚轮或数字键切换。
/// 输入统一走 <see cref="PlayerInputRouter"/>，不直接读设备，避免以后滚轮同时承担"切枪/检视"时冲突。
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    public WeaponController[] weapons;

    private PlayerInputRouter input;
    private int currentIndex = -1;

    void Awake()
    {
        input = PlayerInputRouter.GetOrCreate(gameObject);
    }

    void Start()
    {
        Select(weapons != null && weapons.Length > 0 ? 0 : -1);
    }

    void Update()
    {
        if (weapons == null || weapons.Length == 0 || input == null) return;

        // 滚轮切换
        float scroll = input.ScrollY;
        if (scroll > 0.1f) Select(currentIndex - 1);
        else if (scroll < -0.1f) Select(currentIndex + 1);

        // 数字键 1..N 直接选
        for (int i = 0; i < weapons.Length && i < 9; i++)
        {
            if (input.WasDigitPressed(i)) Select(i);
        }
    }

    private void Select(int index)
    {
        if (weapons == null || weapons.Length == 0) return;

        index = (index + weapons.Length) % weapons.Length; // 滚轮绕圈

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].gameObject.SetActive(i == index);
        }

        currentIndex = index;
    }
}

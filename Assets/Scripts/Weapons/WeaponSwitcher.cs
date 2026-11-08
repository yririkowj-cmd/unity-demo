using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 武器切换：持有多把武器(每个武器是一个带 WeaponController 的物体)，用滚轮或数字键切换。
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    public WeaponController[] weapons;

    private static readonly Key[] DigitKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    private int currentIndex = -1;

    void Start()
    {
        Select(weapons != null && weapons.Length > 0 ? 0 : -1);
    }

    void Update()
    {
        if (weapons == null || weapons.Length == 0) return;

        // 滚轮切换
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0.1f) Select(currentIndex - 1);
            else if (scroll < -0.1f) Select(currentIndex + 1);
        }

        // 数字键 1..N 直接选
        for (int i = 0; i < weapons.Length && i < DigitKeys.Length; i++)
        {
            if (Keyboard.current != null && Keyboard.current[DigitKeys[i]].wasPressedThisFrame)
                Select(i);
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

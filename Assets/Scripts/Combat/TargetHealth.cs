using System.Collections;
using UnityEngine;

/// <summary>
/// 靶子血量：受伤闪红、血量归零销毁。用于测试武器伤害(训练假人在第四步)。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TargetHealth : Health
{
    private Renderer rend;
    private Color originalColor;

    protected override void Awake()
    {
        base.Awake();
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
        OnDamaged += Flash;
    }

    private void Flash(DamageInfo info)
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        rend.material.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        rend.material.color = originalColor;
    }

    protected override void Die()
    {
        base.Die();
        Destroy(gameObject);
    }
}

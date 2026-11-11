using System.Collections;
using UnityEngine;

/// <summary>
/// 靶子血量：受伤闪红、被命中时轻微"弹一下"、血量归零销毁。
///
/// 同时实现 <see cref="IHitReactable"/>：它是"命中反应"这条链路的第一个真实使用者，
/// 以后技能（击飞/拉拽）打到靶子上会走 React，而不只是扣血。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TargetHealth : Health, IHitReactable
{
    private Renderer rend;
    private Color originalColor;
    private Coroutine flashRoutine;
    private Coroutine punchRoutine;

    protected override void Awake()
    {
        base.Awake();
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
        OnDamaged += Flash;
    }

    // —— 受伤闪红 ——

    private void Flash(DamageInfo info)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        rend.material.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        rend.material.color = originalColor;
        flashRoutine = null;
    }

    // —— 命中反应：轻微弹动 ——

    public void React(in HitResult hit)
    {
        float strength = Mathf.Clamp(hit.impulse.magnitude, 0f, 2f);
        if (strength <= 0.0001f) return;

        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine(strength));
    }

    private IEnumerator PunchRoutine(float strength)
    {
        Vector3 baseScale = transform.localScale;
        Vector3 punched = baseScale * (1f + 0.12f * strength);

        transform.localScale = punched;
        yield return new WaitForSeconds(0.05f);

        float t = 0f;
        const float duration = 0.12f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(punched, baseScale, t / duration);
            yield return null;
        }

        transform.localScale = baseScale;
        punchRoutine = null;
    }

    protected override void Die()
    {
        base.Die();
        Destroy(gameObject);
    }
}

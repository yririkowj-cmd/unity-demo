using System;
using UnityEngine;

/// <summary>
/// 通用血量组件：管理当前生命、受伤与死亡。
/// 玩家、敌人、可破坏物都可以挂它；要自定义死亡行为就继承并重写 Die()。
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [Header("生命值")]
    public float maxHealth = 100f;

    [SerializeField] private float currentHealth;

    public bool IsDead => currentHealth <= 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    /// <summary>0~1 的血量比例，方便做血条 UI。</summary>
    public float Health01 => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

    /// <summary>受伤时触发（参数：本次伤害信息）。</summary>
    public event Action<DamageInfo> OnDamaged;
    /// <summary>死亡时触发。</summary>
    public event Action OnDied;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    public virtual bool TakeDamage(DamageInfo info)
    {
        if (IsDead) return false; // 已死亡不再重复结算
        currentHealth -= info.amount;
        OnDamaged?.Invoke(info);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
        return true;
    }

    /// <summary>恢复生命，返回实际恢复量。</summary>
    public float Heal(float amount)
    {
        if (IsDead || amount <= 0f) return 0f;
        float before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        return currentHealth - before;
    }

    /// <summary>直接设置生命值(用于重生等)。</summary>
    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
    }

    protected virtual void Die()
    {
        OnDied?.Invoke();
    }
}

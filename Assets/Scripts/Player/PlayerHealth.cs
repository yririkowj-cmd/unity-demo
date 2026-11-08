using UnityEngine;

/// <summary>
/// 玩家血量：继承 Health，额外处理死亡后的重生。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerHealth : Health
{
    [Header("重生")]
    public bool respawnOnDeath = false;
    public float respawnDelay = 3f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    protected override void Awake()
    {
        base.Awake();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("玩家死亡");
        if (respawnOnDeath)
            Invoke(nameof(Respawn), respawnDelay);
    }

    private void Respawn()
    {
        // CharacterController 需要 disable/enable 才能强制移动位置
        var cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        cc.enabled = true;

        SetHealth(maxHealth);
    }
}

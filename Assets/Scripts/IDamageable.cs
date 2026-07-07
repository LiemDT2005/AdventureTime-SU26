using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, GameObject source);
    bool IsDead { get; }
}
using UnityEngine;

public class PlayerAnimationBridge : MonoBehaviour
{
    private PlayerMeleeAttack meleeAttack;
    private PlayerRangedSkill rangedSkill;

    void Start()
    {
        // Lấy các script ở đối tượng cha Player
        meleeAttack = GetComponentInParent<PlayerMeleeAttack>();
        rangedSkill = GetComponentInParent<PlayerRangedSkill>();
    }

    // Hàm cầu nối cho đòn đánh cận chiến
    public void PerformMeleeAttack()
    {
        if (meleeAttack != null)
        {
            meleeAttack.PerformMeleeAttack();
        }
    }

    // Hàm cầu nối cho đòn ném xa
    public void PerformThrow()
    {
        if (rangedSkill != null)
        {
            rangedSkill.PerformThrow();
        }
    }
}

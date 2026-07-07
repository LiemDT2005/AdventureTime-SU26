public interface IWeaponBehavior
{
    float GetDamage(int comboStep);
    float GetWindupTime(int comboStep);   // thời gian chờ trước khi đòn đánh ra hit (khớp animation event sau này)
    float GetAttackDuration(int comboStep); // tổng thời gian đứng yên khi ra đòn
    void PerformAttack(int comboStep, bool facingRight);
}
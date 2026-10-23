// 데미지를 받을 수 있다는 약속(인터페이스)

public interface IDamageable
{
    // 이 약속은 알애 메서드를 구현해야 한다는 규칙을 가짐
    public void TakeDamage(DamageInfo damageInfo);
}
// Assets/Scripts/Combat/Health/IDamageFilter.cs
public interface IDamageFilter
{
    /// <summary>
    /// return true => 允许扣血
    /// return false => 伤害被拦截（相当于无敌/护盾/阶段免疫）
    /// </summary>
    bool CanTakeDamage(int amount, object source = null);
}

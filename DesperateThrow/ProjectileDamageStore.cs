using System.Collections.Generic;

namespace DesperateThrow
{
    public static class ProjectileDamageStore
    {
        // 映射：Missile Index -> 该投射物对应的武器基础伤害
        // 使用 Dictionary 保证 O(1) 的读写效率
        public static Dictionary<int, int> MissileWeaponDamageMap = new Dictionary<int, int>();

        public static void Record(int index, int damage)
        {
            if (index >= 0)
                MissileWeaponDamageMap[index] = damage;
        }

        public static void Remove(int index)
        {
            if (index >= 0 && MissileWeaponDamageMap.ContainsKey(index))
                MissileWeaponDamageMap.Remove(index);
        }

        public static void Clear() => MissileWeaponDamageMap.Clear();
    }
}
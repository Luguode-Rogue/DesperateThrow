using DesperateThrow;
using HarmonyLib; 
using SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.MountAndBlade.View;
using static TaleWorlds.MountAndBlade.Mission;

namespace DesperateThrow
{
    [HarmonyPatch]
    public static class ProjectileBottleneckPatches
    {
        [HarmonyPatch(typeof(Agent), "HandleDropWeapon")]
        public class AgentHandleDropWeaponPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Agent __instance, bool isDefendPressed, EquipmentIndex forcedSlotIndexToDropWeaponFrom)
            {            
                //必须确保是玩家控制的角色，防止NPC乱丢
                if (!__instance.IsPlayerControlled)
                {
                    return true; // NPC 走原版逻辑（通常NPC不会按丢弃键，但以防万一）
                }
                // 获取当前动作类型
                var currentActionType = __instance.GetCurrentActionType(1);

                // 检查是否处于攻击状态（不满足原始if条件的情况）
                bool isAttackState = false;
                if (__instance.State == AgentState.Active && currentActionType != Agent.ActionCodeType.ReleaseMelee && currentActionType != Agent.ActionCodeType.ReleaseRanged && currentActionType != Agent.ActionCodeType.ReleaseThrowing && currentActionType != Agent.ActionCodeType.WeaponBash)
                { isAttackState = true; }

                // 如果处于攻击状态，执行新的投掷武器逻辑
                if (!isAttackState)
                {
                    EquipmentIndex equipmentIndex = forcedSlotIndexToDropWeaponFrom;
                    if (equipmentIndex == EquipmentIndex.None)
                    {
                        EquipmentIndex primaryWieldedItemIndex = __instance.GetPrimaryWieldedItemIndex();
                        EquipmentIndex offhandWieldedItemIndex = __instance.GetOffhandWieldedItemIndex();
                        if (offhandWieldedItemIndex >= EquipmentIndex.WeaponItemBeginSlot && isDefendPressed)
                        {
                            equipmentIndex = offhandWieldedItemIndex;
                        }
                        else if (primaryWieldedItemIndex >= EquipmentIndex.WeaponItemBeginSlot)
                        {
                            equipmentIndex = primaryWieldedItemIndex;
                        }
                        else if (offhandWieldedItemIndex >= EquipmentIndex.WeaponItemBeginSlot)
                        {
                            equipmentIndex = offhandWieldedItemIndex;
                        }
                        else
                        {
                            for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 < EquipmentIndex.ExtraWeaponSlot; equipmentIndex2++)
                            {
                                if (!__instance.Equipment[equipmentIndex2].IsEmpty && __instance.Equipment[equipmentIndex2].Item.PrimaryWeapon.IsConsumable)
                                {
                                    if (__instance.Equipment[equipmentIndex2].Item.PrimaryWeapon.IsRangedWeapon)
                                    {
                                        if (__instance.Equipment[equipmentIndex2].Amount == 0)
                                        {
                                            equipmentIndex = equipmentIndex2;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        bool flag = false;
                                        for (EquipmentIndex equipmentIndex3 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex3 < EquipmentIndex.ExtraWeaponSlot; equipmentIndex3++)
                                        {
                                            if (!__instance.Equipment[equipmentIndex3].IsEmpty && __instance.Equipment[equipmentIndex3].HasAnyUsageWithAmmoClass(__instance.Equipment[equipmentIndex2].Item.PrimaryWeapon.WeaponClass) && __instance.Equipment[equipmentIndex2].Amount > 0)
                                            {
                                                flag = true;
                                                break;
                                            }
                                        }
                                        if (!flag)
                                        {
                                            equipmentIndex = equipmentIndex2;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    HandleAttackDrop(__instance, equipmentIndex);
                    return false; // 跳过原始方法
                }

                return true; // 执行原始方法
            }

            private static void HandleAttackDrop(Agent agent, EquipmentIndex itemIndex)
            {
                // 检查物品索引有效性
                if (itemIndex < EquipmentIndex.WeaponItemBeginSlot ||
                    itemIndex >= EquipmentIndex.ExtraWeaponSlot)
                {
                    return;
                }
                MissionWeapon missionWeapon   = agent.Equipment[itemIndex];
                
                if (missionWeapon.IsEmpty)
                {
                    return;
                }

                // 获取武器信息
                WeaponComponentData weaponData = missionWeapon.CurrentUsageItem;
                int damage = 0;
                bool shouldThrow = false;

                // 计算投掷伤害（根据武器类型）
                switch (weaponData.WeaponClass)
                {
                    // 近战武器投掷逻辑
                    case WeaponClass.Dagger:
                    case WeaponClass.OneHandedSword:
                    case WeaponClass.OneHandedAxe:
                    case WeaponClass.Mace:
                        damage = (int)(missionWeapon.GetModifiedSwingDamageForCurrentUsage() * 1.5f);
                        shouldThrow = true;
                        break;
                    case WeaponClass.TwoHandedMace:
                    case WeaponClass.TwoHandedSword:
                    case WeaponClass.TwoHandedAxe:
                        damage = missionWeapon.GetModifiedSwingDamageForCurrentUsage();
                        shouldThrow = true;
                        break;
                    case WeaponClass.Pick:
                    case WeaponClass.OneHandedPolearm:
                    case WeaponClass.TwoHandedPolearm:
                    case WeaponClass.LowGripPolearm:
                        damage = missionWeapon.GetModifiedThrustDamageForCurrentUsage() * 2;
                        shouldThrow = true;
                        break;
                    // 投掷武器保持原逻辑（不处理）
                    case WeaponClass.ThrowingAxe:
                    case WeaponClass.ThrowingKnife:
                    case WeaponClass.Javelin:
                        break;
                    default:
                        break;
                }

                if (shouldThrow)
                {
                    // 使用默认投矛模型（western_javelin_1_t2）作为投射物
                    ItemObject weaponItem = Game.Current.ObjectManager.GetObject<ItemObject>("western_javelin_1_t2");
                    MissionWeapon ammoWeapon = new MissionWeapon(weaponItem, null, null); 
                    // 获取发射点（玩家眼睛位置）
                    Vec3 startPos = agent.GetEyeGlobalPosition();

                    // 获取射击方向（玩家视线方向）
                    Vec3 direction = agent.LookDirection;

                    // 获取投射物速度
                    float baseSpeed = ammoWeapon.GetModifiedMissileSpeedForCurrentUsage();
                    float speed = baseSpeed * agent.AgentDrivenProperties.MissileSpeedMultiplier;

                    // 创建投射物
                    Mission.Missile missile = Mission.Current.AddCustomMissile(
                        agent,
                        ammoWeapon,
                        startPos,
                        direction,
                        agent.LookRotation,
                        baseSpeed,
                        speed,
                        true,
                        null,
                        -1
                    );


                    GameEntity missileEntity = missile.Entity;
                    MatrixFrame newFrame = MatrixFrame.Identity;
                    float newScale = 1f;
                    newFrame.rotation.s *= newScale;
                    newFrame.rotation.f *= newScale;
                    newFrame.rotation.u *= newScale;
                    missile.Entity.SetLocalFrame(ref newFrame, true);

                    GameEntity root = missile.Entity;
                    // === 新mesh entity ===
                    GameEntity newMeshEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
                    root.AddChild(newMeshEntity, true);
                    WeakGameEntity weaponEntity = agent.GetWeaponEntityFromEquipmentSlot(itemIndex);
                    newMeshEntity.AddMultiMesh(missionWeapon.Item.GetCraftedMultiMesh(true));

                    newFrame = MatrixFrame.Identity;
                    newScale = 1f;
                    newFrame.rotation.s *= newScale;
                    newFrame.rotation.f *= newScale;
                    newFrame.rotation.u *= newScale;
                    newMeshEntity.SetLocalFrame(ref newFrame, true);

                    // 记录伤害
                    ProjectileDamageStore.Record(missile.Index, damage);

                    // 移除原装备
                    agent.RemoveEquippedWeapon(itemIndex);

                    // 更新代理属性
                    agent.UpdateAgentProperties();
                }
            }
        }

        /// <summary>
        /// 拦截底层投射物生成函数，捕获引擎分配的真实 Index
        /// 目标方法：Mission.AddMissileAux (...)
        /// </summary>
        [HarmonyPatch(typeof(Mission), "AddMissileAux")]
        [HarmonyPostfix]
        public static void Postfix_CaptureMissileIndex(
            int __result,               // 返回值：引擎分配的 Missile Index
            bool isPrediction,          // 参数：是否为预测弹道
            Agent shooterAgent)         // 参数：发射者
        {
            // 1. 过滤无效情况：预测弹道、空射手、无效索引
            if (isPrediction || shooterAgent == null || __result < 0)
                return;

            // 2. 获取当前手持武器的伤害
            // 此时 Agent 的 WieldedItemIndex 通常已经锁定为正在使用的武器
            EquipmentIndex wieldedItemIndex = shooterAgent.GetPrimaryWieldedItemIndex();

            // 兼容副手武器（如某些特殊投掷动作）
            if (wieldedItemIndex == EquipmentIndex.None)
                wieldedItemIndex = shooterAgent.GetOffhandWieldedItemIndex( );

            if (wieldedItemIndex != EquipmentIndex.None)
            {
                MissionWeapon weapon = shooterAgent.Equipment[wieldedItemIndex];
                // 获取经过技能、属性修正后的最终导弹伤害
                int weaponDamage = weapon.GetModifiedMissileDamageForCurrentUsage();

                // 3. 记录映射关系
                ProjectileDamageStore.Record(__result, weaponDamage);
            }
        }

        /// <summary>
        /// 拦截伤害计算模型，注入武器伤害
        /// 覆盖战役模式 (Sandbox) 和 自定义战斗 (Default)
        /// </summary>
        [HarmonyPatch(typeof(SandboxStrikeMagnitudeModel), "CalculateStrikeMagnitudeForMissile")]
        [HarmonyPatch(typeof(DefaultStrikeMagnitudeModel), "CalculateStrikeMagnitudeForMissile")]
        [HarmonyPostfix]
        public static void Postfix_InjectDamage(
            ref float __result,
            in AttackCollisionData collisionData,
            in MissionWeapon weapon) // 这里的 weapon 参数通常指弹药本身，而非发射器
        {
            int missileIndex = collisionData.AffectorWeaponSlotOrMissileIndex;

            // 尝试从字典中获取之前记录的武器伤害
            if (ProjectileDamageStore.MissileWeaponDamageMap.TryGetValue(missileIndex, out int extraWeaponDamage))
            {
                float ammoDamage = collisionData.MissileTotalDamage;

                // 防御性编程：避免除以零
                if (ammoDamage <= 0f) ammoDamage = 1f;

                // --- 核心修正公式 --- 
                __result = (__result / ammoDamage) * (extraWeaponDamage);
            }
        }
    }
}
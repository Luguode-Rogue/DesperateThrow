using DesperateThrow;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DesperateThrow
{
    public class ProjectileCleanupBehavior : MissionLogic
    {
        // 当投射物命中时，立即清理对应的数据
        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            base.OnMissileHit(attacker, victim, isCanceled, collisionData);
            if (!isCanceled)
            {
                ProjectileDamageStore.Remove(collisionData.AffectorWeaponSlotOrMissileIndex);
            }
        }

        // 当投射物消失（未命中或超出范围）时，引擎通常会调用相关事件，
        // 但为了保险起见，我们在 Mission 结束时统一清空
        protected override void OnEndMission()
        {
            base.OnEndMission();
            ProjectileDamageStore.Clear();
        }

        public override void OnMissionModeChange(MissionMode oldMode, bool atStart)
        {
            base.OnMissionModeChange(oldMode, atStart);
            if (atStart) ProjectileDamageStore.Clear();
        }
 

    }
}
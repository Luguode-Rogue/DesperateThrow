using DesperateThrow;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace DesperateThrow
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

             
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);

            // 初始化 Harmony 并应用所有补丁
            var harmony = new Harmony("com.projectile.system.bottleneck");
            harmony.PatchAll(); 
            // 注意：核心的索引捕获已由 Harmony 在底层完成，此处仅需清理
            mission.AddMissionBehavior(new ProjectileCleanupBehavior());
        }
    }
}
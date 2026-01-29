// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using LudeonTK;
using Verse.AI.Group;

namespace Exosuit
{
    public static class DebugActionsMech
    {
        private const bool DebugLog = true;
        private static void DLog(string message)
        {
            if (DebugLog)
                Verse.Log.Message(message);
        }
        [DebugAction("Mechsuit", "Spawn Hostile Ammo Dragoon", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnHostileAmmoDragoon()
        {
            // 使用 CE 补丁定义的测试兵种
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("DMS_Squad_ArmyDragoon_TEST_Ammo") ?? PawnKindDef.Named("DMS_Squad_ArmyDragoon");
            Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction) 
                           ?? Find.FactionManager.FirstFactionOfDef(FactionDefOf.AncientsHostile);

            PawnGenerationRequest request = new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            
            GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);

            // 强制分配攻击任务，防止其离开地图
            Lord lord = LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false), Find.CurrentMap, new List<Pawn> { pawn });
            
            DLog($"[Mechsuit] Debug: 已生成敌对单位 ({kind.defName}). 派系: {faction.Name}, 任务: 进攻殖民地");
        }
        
        [DebugAction("Mechsuit", "Check Apparel Comps", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CheckApparelComps()
        {
            foreach (Thing thing in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()))
            {
                if (thing is Pawn p)
                {
                    foreach (var apparel in p.apparel.WornApparel)
                    {
                        DLog($"[Mechsuit] Apparel: {apparel.def.defName}, Comps: {string.Join(", ", apparel.AllComps.Select(c => c.GetType().Name))}");
                    }
                }
            }
        }
    }
}

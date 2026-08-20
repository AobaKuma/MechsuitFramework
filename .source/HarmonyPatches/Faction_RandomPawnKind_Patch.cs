// 当白昼倾坠之时
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Exosuit
{
    /// <summary>
    /// 防止外骨骼駕駛員 PawnKind 被 <see cref="Faction.RandomPawnKind"/> 隨機抽中。
    ///
    /// 原版 RandomPawnKind() 會等權重遍歷該派系所有 pawnGroupMakers 裡的 humanlike
    /// option，完全不做任何過濾（連 commonality 都不看）。而
    /// QuestNode_GetPawn.GeneratePawn / QuestGen_Pawns.GeneratePawn 在
    /// canGeneratePawn = true 且未指定 mustBeOfKind 時就是走這條，
    /// 結果駕駛員會被選為任務委託人（asker）等角色 ——
    /// Util_DecideRandomAsker / Script_PawnLend / Scripts_RewardRaid /
    /// Scripts_RewardMechpods 都在此列。
    ///
    /// 原版沒有任何 PawnKindDef 層級的排除旗標可用
    /// （RaceProperties.neverIncludeInQuests 只在動物相關的三處被讀取），
    /// 所以只能在這裡攔截。
    /// </summary>
    [HarmonyPatch(typeof(Faction), nameof(Faction.RandomPawnKind))]
    internal static class Faction_RandomPawnKind_Patch
    {
        /// <summary>
        /// 額外排除項。內容模組可在載入後自行登記不希望被任務隨機選中的 PawnKind。
        /// </summary>
        public static readonly HashSet<PawnKindDef> ExtraExcluded = new();

        // 每個 FactionDef 的 humanlike 候選快取。Def 在執行期不會變動，快取安全。
        private static readonly Dictionary<FactionDef, List<PawnKindDef>> humanlikeCache = new();

        /// <summary>判定某個 PawnKind 是否為外骨骼駕駛員（或被額外排除）。</summary>
        public static bool IsExcluded(PawnKindDef kind)
        {
            if (kind == null) return false;
            return kind.HasModExtension<ModExtForceApparelGen>() || ExtraExcluded.Contains(kind);
        }

        [HarmonyPostfix]
        static void Postfix(Faction __instance, ref PawnKindDef __result)
        {
            if (__instance?.def == null) return;
            if (!IsExcluded(__result)) return;

            // 先從同派系的其他 humanlike kind 裡挑，維持派系風味
            if (GetHumanlikeKinds(__instance.def).Where(k => !IsExcluded(k)).TryRandomElement(out var replacement))
            {
                __result = replacement;
                return;
            }

            // 派系內找不到（例如整個派系都是駕駛員）就退回 basicMemberKind
            PawnKindDef basic = __instance.def.basicMemberKind;
            if (basic != null && !IsExcluded(basic))
            {
                __result = basic;
                return;
            }

            // 真的無可替換就維持原樣 —— 總比丟出一個完全不屬於該派系的 Pawn 好。
        }

        private static List<PawnKindDef> GetHumanlikeKinds(FactionDef def)
        {
            if (humanlikeCache.TryGetValue(def, out List<PawnKindDef> cached)) return cached;

            var list = new List<PawnKindDef>();
            if (def.pawnGroupMakers != null)
            {
                foreach (PawnGroupMaker groupMaker in def.pawnGroupMakers)
                {
                    if (groupMaker?.options == null) continue;
                    foreach (PawnGenOption option in groupMaker.options)
                    {
                        if (option?.kind?.race?.race == null) continue;
                        if (!option.kind.RaceProps.Humanlike) continue;
                        if (!list.Contains(option.kind)) list.Add(option.kind);
                    }
                }
            }

            humanlikeCache[def] = list;
            return list;
        }
    }
}

using HarmonyLib;
using RimWorld;
using Verse;

namespace Exosuit
{
    // 舊存檔裡的儲物架藍圖是 Blueprint_Build（當時 def 沒寫 blueprintClass），由它生成的 Frame 沒有 storageSettings，
    // 完工或自動重建時原版會 CopyFrom(null) 報錯。照 Blueprint_Storage.PostMake 的方式補上預設設定。
    [HarmonyPatch(typeof(Frame))]
    public static class Frame_StorageSettings_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Frame.CompleteConstruction))]
        static void CompleteConstruction_Prefix(Frame __instance) => EnsureStorageSettings(__instance);

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Frame.Destroy))]
        static void Destroy_Prefix(Frame __instance) => EnsureStorageSettings(__instance);

        static void EnsureStorageSettings(Frame frame)
        {
            if (frame.storageSettings != null) return;
            if (frame.def.entityDefToBuild is not ThingDef def || !typeof(IStorageGroupMember).IsAssignableFrom(def.thingClass)) return;
            frame.storageSettings = new StorageSettings();
            if (def.building?.defaultStorageSettings != null)
            {
                frame.storageSettings.CopyFrom(def.building.defaultStorageSettings);
            }
        }
    }
}

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace WalkerGear
{
    ////TBD
    //[HarmonyPatch(typeof(JobGiver_GetRest))]
    //[HarmonyPatch("GetPriority")]
    //public class JobGiver_GetPriority_Patcher
    //{
    //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        var code = new List<CodeInstruction>(instructions);

    //        int insertionIndex = -1;
    //        for (int i = 0; i < code.Count - 1; i++) // -1 since we will be checking i + 1
    //        {
    //            if (code[i].opcode == OpCodes.Ldc_I4 && (int)code[i].operand == 566 && code[i + 1].opcode == OpCodes.Ret)
    //            {
    //                insertionIndex = i;
    //                break;
    //            }
    //        }

    //        return code;
    //    }
    //}
}
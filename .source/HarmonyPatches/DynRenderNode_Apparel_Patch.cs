using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit.HarmonyPatches
{
    [StaticConstructorOnStartup]
    static class DynRenderNode_Apparel_Patch
    {
        static DynRenderNode_Apparel_Patch()
        {
            ExosuitMod.instance.Patch(Tar(), transpiler: typeof(DynRenderNode_Apparel_Patch).Method(nameof(AddParentTagToGeneratedApparelNode)));
            ExosuitMod.instance.Patch(typeof(PawnRenderTree).Method("TrySetupGraphIfNeeded"), postfix: typeof(DynRenderNode_Apparel_Patch).Method(nameof(TrySetupGraphIfNeeded_Patch)));
        }
        static Type tarType;
        [HarmonyTargetMethod]
        static MethodInfo Tar()
        {
            tarType = typeof(DynamicPawnRenderNodeSetup_Apparel).FirstInner(t => t.Name == "<ProcessApparel>d__5");
            var mi = tarType.Method("MoveNext");
            Log.Message(mi.Name);
            return mi;
        }
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> AddParentTagToGeneratedApparelNode(IEnumerable<CodeInstruction> instructions)
        {
            var MethodTar = typeof(PawnRenderTree).Method(nameof(PawnRenderTree.ShouldAddNodeToTree));
            var l = instructions.ToList();
            for (int i = l.Count-1; i >= 0; i--)
            {
                if (l[i].Calls(MethodTar))
                {
                    l[i].operand = typeof(DynRenderNode_Apparel_Patch).Method(nameof(ShouldAddNodeToTreeModified));
                    l.InsertRange(i, [CodeInstruction.LoadArgument(0), CodeInstruction.LoadField(tarType, "ap")]);
                    break;
                }
            }
            return l;
        }

        static bool ShouldAddNodeToTreeModified(PawnRenderTree tree, PawnRenderNodeProperties prop,Apparel ap)
        {
            

            prop.parentTagDef = ap.def.apparel.parentTagDef;
            Log.Message(ap + " ParentTag: " + prop.parentTagDef);
            return tree.ShouldAddNodeToTree(prop);
        }
    

        static void TrySetupGraphIfNeeded_Patch(PawnRenderTree __instance)
        {
            
           
            

        }
    }
}

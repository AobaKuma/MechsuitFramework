using HarmonyLib;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    /// <summary>
    /// 保证龙骑兵渲染节点挂在正确的parentTag
    /// </summary>
    //[StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PawnRenderTree))]
    //[HarmonyPatchCategory("Test")]
    static class PawnRenderTree_Apparel_Patch
    {
/*        static PawnRenderTree_Apparel_Patch()
        {
            ExosuitMod.instance.PatchCategory("Test");
        }*/
        [HarmonyPatch("SetupDynamicNodes")]
        [HarmonyPostfix]
        static void PostSetupApparelTags(Pawn ___pawn, PawnRenderTree __instance)
        {
            //Log.Message("Rebuild Apparel Tags");
            //PrintRenderTree(__instance.rootNode);
            //Action action=null;
            foreach(var (tag,node) in __instance.nodesByTag)
            {
                if (__instance.tmpChildTagNodes.TryGetValue(tag,out var children))
                {
                    children.ForEach(c => {
                        //Log.Message($"Set {c} Parent To {node}");
                        c.parent = node; 
                    });
                }
            }
            
        }

        [HarmonyPostfix]
        [HarmonyPatch("AddChild")]
        static void AddChildEvenHasNullParentTag(PawnRenderNode child,PawnRenderNode parent, PawnRenderTree __instance)
        {
            //Log.Message("AddChild");
            var parTag = child.Props.parentTagDef;
            if (parTag == null && child is PawnRenderNode_Apparel ap_node)
                parTag = ap_node.apparel?.def.apparel.parentTagDef;
            if (parTag == null) return;
            //他应该在的列表
            var tmpChildTagNodes = __instance.tmpChildTagNodes;
            if (!tmpChildTagNodes.TryGetValue(parTag,out var chilren))
                tmpChildTagNodes.Add(parTag, chilren = []);

            //tag放对了不处理
            if (chilren.Contains(child))return;
            if (parent != null && tmpChildTagNodes.TryGetValue(parent.props.tagDef, out List<PawnRenderNode> rootChildren))
                rootChildren.Remove(child);
            chilren.Add(child);
            child.parent = null;
        }

    }
}

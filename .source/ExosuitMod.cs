using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using Verse;
using static HarmonyLib.AccessTools;

namespace Exosuit
{
    public class ExosuitMod : Mod
        
    {
        internal static Harmony instance;
        public ExosuitMod(ModContentPack content) : base(content)
        {
            //Harmony.DEBUG = true;
            BackCompatibility.conversionChain.Add(new BackCompat_Exosuit_1_6());
            instance = new Harmony("ExosuitMod");
            LongEventHandler.QueueLongEvent(delegate
            {
                PatchClassProcessor[] patchClasses = GetTypesFromAssembly(Assembly.GetAssembly(typeof(ExosuitMod))).Select(new Func<Type, PatchClassProcessor>(instance.CreateClassProcessor)).ToArray();
                patchClasses.DoIf((PatchClassProcessor patchClass) => string.IsNullOrEmpty(patchClass.Category), delegate (PatchClassProcessor patchClass)
                {
                    try
                    {
                        patchClass.Patch();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.Message + e.StackTrace);
                    }

                });
            },"Exosuit Patching",true,null);
            
        }
        //[HarmonyPatchCategory("ModPatches")]
        //internal static class SimpleSidearms
        //{
        //    [HarmonyPrefix]
        //    internal static bool SetPrimary(Pawn pawn,ref bool __result)
        //    {
        //        return pawn.equipment.Primary == null || !pawn.equipment.Primary.HasComp<CompApparelForcedWeapon>() || (__result = false);
        //    }
        //}

        /*[HarmonyPatch(typeof(DirectXmlToObjectNew))]
        static class DirectXmlToObjectNew_DefFromNodeNew
        {
            [HarmonyTranspiler]
            [HarmonyPatch(nameof(DirectXmlToObjectNew.DefFromNodeNew))]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = instructions.ToList();
                var pos = list.FindIndex(ci => ci.Calls(typeof(Log).Method(nameof(Log.Error))));
                list.InsertRange(pos + 1, [
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.Call((XmlNode node)=>LogNode(node))
                    ]) ;
                return list;
            }
            static void LogNode(XmlNode node)
            {
                Log.Error(node.InnerXml);
                //Log.Error(node.OuterXml);
            }
            [HarmonyPatch("EmitIlToCreateAndPopulateList")]
            [HarmonyPrefix]
            static bool AddLogToDyn(ILGenerator il, Type listType, Type itemType)
            {
                //DirectXmlToObjectNew.EmitIlToCreateAndPopulateList
                ConstructorInfo constructor = listType.GetConstructor(new Type[] { typeof(int) });
                bool flag = GenTypes.IsDef(itemType);
                LocalBuilder localBuilder = il.DeclareLocal(listType);
                LocalBuilder localBuilder2 = il.DeclareLocal(typeof(XmlNodeList));
                LocalBuilder localBuilder3 = il.DeclareLocal(typeof(int));
                LocalBuilder localBuilder4 = il.DeclareLocal(typeof(int));
                LocalBuilder localBuilder5 = il.DeclareLocal(typeof(XmlNode));
                LocalBuilder localBuilder6 = il.DeclareLocal(typeof(XmlAttribute));
                LocalBuilder localBuilder7 = il.DeclareLocal(typeof(XmlAttribute));
                LocalBuilder localBuilder8 = il.DeclareLocal(typeof(XmlAttribute));
                LocalBuilder localBuilder9 = il.DeclareLocal(typeof(string));
                LocalBuilder localBuilder10 = il.DeclareLocal(typeof(string));
                LocalBuilder localBuilder11 = il.DeclareLocal(typeof(XmlAttribute));
                LocalBuilder localBuilder12 = il.DeclareLocal(typeof(string));
                Label label = il.DefineLabel();
                Label label2 = il.DefineLabel();
                Label label3 = il.DefineLabel();
                Label label4 = il.DefineLabel();
                Label label5 = il.DefineLabel();
                Label label6 = il.DefineLabel();
                LocalBuilder actualItemType = il.DeclareLocal(typeof(Type));
                Label label7 = il.DefineLabel();
                Label label8 = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
                il.Emit(OpCodes.Ldstr, "IsNull");
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, localBuilder6);
                il.Emit(OpCodes.Brfalse, label);
                il.Emit(OpCodes.Ldloc, localBuilder6);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
                il.Emit(OpCodes.Ldstr, "true");
                il.Emit(OpCodes.Ldc_I4, 3);
                il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringEqualsWithComparisonModeMethod);
                il.Emit(OpCodes.Brfalse, label);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, localBuilder);
                il.Emit(OpCodes.Br, label4);
                il.MarkLabel(label);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetChildNodesMethod);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, localBuilder2);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetCountMethod);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, localBuilder3);
                il.Emit(OpCodes.Newobj, constructor);
                il.Emit(OpCodes.Stloc, localBuilder);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, localBuilder4);
                il.MarkLabel(label2);
                il.Emit(OpCodes.Ldloc, localBuilder4);
                il.Emit(OpCodes.Ldloc, localBuilder3);
                il.Emit(OpCodes.Bge, label4);
                il.Emit(OpCodes.Ldloc, localBuilder2);
                il.Emit(OpCodes.Ldloc, localBuilder4);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetItemMethod);
                il.Emit(OpCodes.Stloc, localBuilder5);
                il.Emit(OpCodes.Ldloc, localBuilder5);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldtoken, itemType);
                il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
                il.Emit(OpCodes.Call, DirectXmlToObjectNew.ValidateListNodeMethod);
                il.Emit(OpCodes.Brfalse, label3);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, localBuilder9);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, localBuilder10);
                il.Emit(OpCodes.Ldloc, localBuilder5);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
                il.Emit(OpCodes.Ldstr, "MayRequire");
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, localBuilder7);
                il.Emit(OpCodes.Brfalse, label5);
                il.Emit(OpCodes.Ldloc, localBuilder7);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
                il.Emit(OpCodes.Stloc, localBuilder9);
                il.MarkLabel(label5);
                il.Emit(OpCodes.Ldloc, localBuilder5);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
                il.Emit(OpCodes.Ldstr, "MayRequireAnyOf");
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, localBuilder8);
                il.Emit(OpCodes.Brfalse, label6);
                il.Emit(OpCodes.Ldloc, localBuilder8);
                il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
                il.Emit(OpCodes.Stloc, localBuilder10);
                il.MarkLabel(label6);
                if (flag)
                {
                    il.Emit(OpCodes.Ldloc, localBuilder);
                    il.Emit(OpCodes.Castclass, listType);
                    il.Emit(OpCodes.Ldloc, localBuilder5);
                    il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetInnerTextMethod);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetNameMethod);
                    il.Emit(OpCodes.Box, typeof(string));
                    il.Emit(OpCodes.Ldloc, localBuilder9);
                    il.Emit(OpCodes.Ldloc, localBuilder10);
                    il.Emit(OpCodes.Call, DirectXmlToObjectNew.RegisterListWantsCrossRefMethod.MakeGenericMethod(new Type[] { itemType }));
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, localBuilder9);
                    il.Emit(OpCodes.Ldloc, localBuilder10);
                    il.Emit(OpCodes.Call, DirectXmlToObjectNew.ValidateMayRequiresMethod);
                    il.Emit(OpCodes.Brfalse, label3);
                    il.Emit(OpCodes.Ldloc, localBuilder5);
                    il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
                    il.Emit(OpCodes.Ldstr, "Class");
                    il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, localBuilder11);
                    il.Emit(OpCodes.Brfalse, label7);
                    il.Emit(OpCodes.Ldloc, localBuilder11);
                    il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
                    il.Emit(OpCodes.Stloc, localBuilder12);
                    il.Emit(OpCodes.Ldloc, localBuilder12);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Call, DirectXmlToObjectNew.GenTypesGetTypeInAnyAssemblyMethod);
                    il.Emit(OpCodes.Stloc, actualItemType);
                    il.Emit(OpCodes.Br, label8);
                    il.MarkLabel(label7);
                    il.Emit(OpCodes.Ldtoken, itemType);
                    il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
                    il.Emit(OpCodes.Stloc, actualItemType);
                    il.Emit(OpCodes.Br, label8);
                    il.MarkLabel(label8);
                    il.Emit(OpCodes.Ldloc, actualItemType);
                    //
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldloc, localBuilder12);
                    il.Emit(OpCodes.Call, typeof(DirectXmlToObjectNew_DefFromNodeNew).Method("LogNull"));
                    //
                    il.Emit(OpCodes.Call, DirectXmlToObjectNew.GetListItemAdderForTypeMethod);
                    il.Emit(OpCodes.Ldloc, localBuilder);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ldloc, localBuilder5);
                    il.Emit(OpCodes.Ldloc, actualItemType);
                    il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseValueAndAddListItemDelegateInvokeMethod);
                }
                il.MarkLabel(label3);
                il.Emit(OpCodes.Ldloc, localBuilder4);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, localBuilder4);
                il.Emit(OpCodes.Br, label2);
                il.MarkLabel(label4);
                il.Emit(OpCodes.Ldloc, localBuilder);
                return false;
            }
            static void LogNull(Type type, string value)
            {
                if (type == null)
                {
                    Log.Error(value);
                }
            }
        }*/


        
    }
}

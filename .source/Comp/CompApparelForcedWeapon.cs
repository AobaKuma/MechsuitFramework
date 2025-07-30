using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace Exosuit
{
    public class CompApparelForcedWeapon : ThingComp
    {

        public CompModuleWeapon ParentComp { get; set; }
        public override void Notify_Equipped(Pawn pawn)
        {
            pawn.drawer.renderer.SetAllGraphicsDirty();
            ParentComp.WeaponStorage = null;
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            
            if (parent.holdingOwner == null)
            { 
                parent.DeSpawnOrDeselect();
            }
            ParentComp.WeaponStorage = parent;
            pawn.drawer.renderer.SetAllGraphicsDirty();
            ThingOwner_TryTransferToContainer.thingListening = parent;
        }
        
    }
    public class CompModuleWeapon : ThingComp
    {
        protected CompProperties_ModuleWeapon Props => (CompProperties_ModuleWeapon)props;
        protected Apparel Parent => parent as Apparel; 
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref weapon, "weaponRef");
            Scribe_Deep.Look(ref weaponStorage, "weaponDeep");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Weapon.GetComp<CompApparelForcedWeapon>().ParentComp = this;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            weaponStorage = Weapon;
        }
        /*        public override void CompDrawWornExtras()
                {
                    base.CompDrawWornExtras();
                    if(Parent?.Wearer != null && Props.storedWeaponDrawData!=null) {
                        if (Parent.Wearer.equipment.Primary != Weapon)
                        {
                            var rot = Parent.Wearer.Rotation;
                            var drawData = Props.storedWeaponDrawData;
                            var matrix = Matrix4x4.TRS(
                                Parent.Wearer.DrawPos 
                                + drawData.OffsetForRot(rot)
                                -(drawData.PivotForRot(rot)-DrawData.PivotCenter).ToVector3(), Quaternion.AngleAxis((drawData.FlipForRot(rot)?-1f:1f)*drawData.RotationOffsetForRot(rot), Vector3.up), Vector3.one*drawData.scale);
                            Graphics.DrawMesh(MeshPool.plane10, matrix * Matrix4x4.Translate(Vector3.up*PawnRenderUtility.AltitudeForLayer(drawData.LayerForRot(rot,20))), Weapon.Graphic.MatSingleFor(Weapon),0);
                        }
                    }
                }*/
        /*        public override List<PawnRenderNode> CompRenderNodes()
                {
                    List<PawnRenderNode> nodes = [];
                    if (Props.onGantryRenderNodeProps != null && Parent.Wearer != null)
                    {
                        var node = (PawnRenderNode)Activator.CreateInstance(Props.onGantryRenderNodeProps.nodeClass, [Parent.Wearer, Props.onGantryRenderNodeProps, Parent.Wearer.drawer.renderer.renderTree]);
                        nodes.Add(node);
                    }
                    if (Parent.Wearer != null&& Parent.Wearer.equipment.Primary != Weapon)
                    {
                        var prop = Props.weaponStoredRenderProps;
                        if (prop == null&&Props.storedWeaponDrawData!=null) {
                            prop = new PawnRenderNodeProperties()
                            {
                                debugLabel = Weapon.def.defName,
                                nodeClass = typeof(PawnRenderNode_WeaponHolder),
                                parentTagDef = MiscDefOf.WGApparelBody,
                                workerClass = typeof(PawnRenderNodeWorker),
                                baseLayer = 20,//baselayer for body apparel
                                drawData = Props.storedWeaponDrawData,
                            };
                        }
                        var node = (PawnRenderNode_WeaponHolder)Activator.CreateInstance(prop.nodeClass ?? typeof(PawnRenderNode_WeaponHolder), [Parent.Wearer, prop, Parent.Wearer.drawer.renderer.renderTree,weapon]);
                        nodes.Add(node);
                    }
                    return nodes;
                }*/

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            bool NoModuleWeapon = Parent.Wearer.equipment.Primary == null || !Parent.Wearer.equipment.Primary.TryGetComp<CompApparelForcedWeapon>(out var _);
            var wearer = Parent.Wearer;
            var command = new CommandActionWithOptions()
            {
                defaultLabel = "ToggleModuleWeapon".TranslateSimple(),
                icon = !NoModuleWeapon ? wearer.equipment.Primary.def.uiIcon : Command.BGTex,
                groupKeyIgnoreContent = wearer.thingIDNumber,
                
            };
            if (!NoModuleWeapon)
            {
                command.options.Add(new("BareHands".TranslateSimple(), delegate
                {
                    wearer.equipment.Remove(wearer.equipment.Primary);
                }, MenuOptionPriority.High));
            }
            if (wearer.equipment.Primary != Weapon)
            {
                command.options.Add(new("TakeOutModuleWeapon".Translate(Weapon.LabelShortCap), delegate {
                    /*Log.Message(Weapon);
                    Log.Message(Weapon.holdingOwner?.ToString() ?? "No Owner");*/
                    if (wearer.equipment.Primary != null)
                        wearer.equipment.TryTransferEquipmentToContainer(wearer.equipment.Primary, wearer.inventory.innerContainer);
                    if (Weapon.holdingOwner == null)
                    {
                        Weapon.DeSpawnOrDeselect();
                    }
                    Weapon.holdingOwner?.Remove(Weapon);
                    ThingOwner_TryTransferToContainer.thingListening=null;
                    
                    wearer.equipment.AddEquipment(Weapon);
                }));
            }
                
            command.action = delegate
            {
                Find.WindowStack.Add(new FloatMenu(command.options));
            };
            yield return command;

            

        }
        public ThingWithComps Weapon
        {
            get
            {
                if (weapon == null)
                {
                    if (weaponStorage != null)
                    {
                        return this.weapon = weaponStorage;
                    }
                    ThingDef weapon = Props.weapon;
                    if (Props.weapon != null && Props.weapon.HasComp<CompApparelForcedWeapon>())
                    {
                        this.weapon = ThingMaker.MakeThing(weapon) as ThingWithComps;
                        if (this.weapon.TryGetComp(out CompQuality compQuality) && parent.TryGetQuality(out var qc))
                        {
                            compQuality.SetQuality(qc, null);
                        }
                        this.weapon.GetComp<CompApparelForcedWeapon>().ParentComp = this;
                    }
                }
                return weapon;
            }
        }

        public ThingWithComps WeaponStorage { set
            {
                if(value== null)
                    weaponStorage = null;
                else if(value==weapon) 
                    weaponStorage = value;

            } }

        private ThingWithComps weapon;
        private ThingWithComps weaponStorage;
    }
    public class CompProperties_ModuleWeapon : CompProperties
    {
        public CompProperties_ModuleWeapon()
        {
            compClass = typeof(CompModuleWeapon);
        }
        public ThingDef weapon;
    }

}

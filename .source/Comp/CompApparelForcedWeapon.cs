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
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            var baseEntries = base.SpecialDisplayStats();
            if (baseEntries != null)
            {
                foreach (var entry in baseEntries)
                {
                    if (entry != null) yield return entry;
                }
            }

            if (Props == null) yield break;


            if (Props.weapon != null)
            {
                yield return new StatDrawEntry(
                    WG_StatCategoryDefOf.MF_ModuleStats,
                    "WG_WeaponModule".Translate().CapitalizeFirst(),
                    Props.weapon.LabelCap,
                    "WG_WeaponModule_Desc".Translate(),
                    4000,
                    null,
                    new List<Dialog_InfoCard.Hyperlink> { new Dialog_InfoCard.Hyperlink(Props.weapon) }
                );

            }
        }
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            bool NoModuleWeapon = Parent.Wearer.equipment.Primary == null || !Parent.Wearer.equipment.Primary.TryGetComp<CompApparelForcedWeapon>(out var _);
            var wearer = Parent.Wearer;
            var command = new CommandActionWithOptions()
            {
                defaultLabel = "WG_ToggleModuleWeapon".TranslateSimple(),
                icon = !NoModuleWeapon ? wearer.equipment.Primary.def.uiIcon : TexCommand.AttackMelee,
                groupKeyIgnoreContent = wearer.thingIDNumber,
                
            };
            if (!NoModuleWeapon)
            {
                command.options.Add(new("WG_BareHands".TranslateSimple(), delegate
                {
                    wearer.equipment.Remove(wearer.equipment.Primary);
                }, MenuOptionPriority.High));
            }
            if (wearer.equipment.Primary != Weapon)
            {
                command.options.Add(new("WG_TakeOutModuleWeapon".Translate(Weapon.LabelShortCap), delegate {
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

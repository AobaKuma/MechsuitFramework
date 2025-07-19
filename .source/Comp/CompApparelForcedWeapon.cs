using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    public class CompApparelForcedWeapon : ThingComp
    {
        public bool NeedRemove;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref NeedRemove, "NeedRemove");
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (!NeedRemove)
            {
                parent.DeSpawnOrDeselect();
                pawn.equipment.MakeRoomFor(parent);
                if (parent.ParentHolder is not Pawn_EquipmentTracker)
                {
                    parent.ParentHolder?.GetDirectlyHeldThings().Remove(parent);
                }
                pawn.equipment.AddEquipment(parent);
            }
        }
    }
    public class CompForceUseWeapon : ThingComp
    {
        public CompProperties_ForceUseWeapon Props => (CompProperties_ForceUseWeapon)props;
        public override void Notify_Equipped(Pawn pawn)
        {
            //simpleSidearm compat;
            if (pawn.equipment.Primary != null)
            {
                var i = pawn.equipment.Primary;
                pawn.equipment.Remove(i);
                i.Notify_Unequipped(pawn);
                pawn.equipment.Notify_EquipmentRemoved(i);
                pawn.inventory.innerContainer.TryAddOrTransfer(i);
            }
            pawn.equipment.AddEquipment(Weapon);
            pawn.equipment.Notify_EquipmentAdded(Weapon);

            NeedRemoveWeapon = false;
            base.Notify_Equipped(pawn);
            weaponStorage = null;
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            NeedRemoveWeapon = true;
            pawn.equipment.Remove(Weapon);
            weaponStorage = Weapon;

            //simpleSidearm compat;
            if (ModLister.GetActiveModWithIdentifier("petetimessix.simplesidearms", true) == null)//沒有副武器的狀況下才啟用該功能。
            {
                //這裡是下機後自動換上物品欄的其他武器
                var things = pawn.inventory?.GetDirectlyHeldThings().Where(t => t.def.equipmentType == EquipmentType.Primary);
                if (!things.EnumerableNullOrEmpty() && pawn.equipment.Primary == null)
                {
                    foreach (Thing t in things)
                    {
                        ThingWithComps thing = t as ThingWithComps;
                        if (EquipmentUtility.CanEquip(thing, pawn))
                        {
                            pawn.inventory.innerContainer.Remove(thing);
                            pawn.equipment.AddEquipment(thing);
                            break;
                        }
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref weapon, "weaponRef");
            Scribe_Deep.Look(ref weaponStorage, "weaponDeep");

        }
        private ThingWithComps Weapon
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
                    }
                }
                return weapon;
            }
        }
        private bool NeedRemoveWeapon
        {
            get { return Weapon.TryGetComp<CompApparelForcedWeapon>().NeedRemove; }
            set { Weapon.TryGetComp<CompApparelForcedWeapon>().NeedRemove = value; }
        }

        private ThingWithComps weapon;
        private ThingWithComps weaponStorage;
    }
    public class CompProperties_ForceUseWeapon : CompProperties
    {
        public CompProperties_ForceUseWeapon()
        {
            compClass = typeof(CompForceUseWeapon);
        }
        public ThingDef weapon;
    }

}

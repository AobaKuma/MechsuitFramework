using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WalkerGear
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
                    parent.ParentHolder.GetDirectlyHeldThings().Remove(parent);
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
            base.Notify_Equipped(pawn);
            NeedRemoveWeapon = false;
            //pawn.equipment.MakeRoomFor(Weapon);
            if (pawn.equipment.Primary != null)
            {
                ThingWithComps i = pawn.equipment.Primary;
                pawn.equipment.Remove(i);
                pawn.inventory.TryAddAndUnforbid(i);
            }
            pawn.equipment.AddEquipment(Weapon);
            weaponStorage = null;
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            NeedRemoveWeapon = true;
            pawn.equipment.Remove(Weapon);
            weaponStorage = Weapon;

            var things = pawn.inventory?.GetDirectlyHeldThings().Where(t => t.def.equipmentType == EquipmentType.Primary);
            if (!things.EnumerableNullOrEmpty())
            {
                foreach (Thing t in things)
                {
                    ThingWithComps thing = t as ThingWithComps;
                    if (!EquipmentUtility.CanEquip(thing, pawn)) continue;
                    pawn.inventory.innerContainer.Remove(thing);
                    pawn.equipment.AddEquipment(thing);
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
